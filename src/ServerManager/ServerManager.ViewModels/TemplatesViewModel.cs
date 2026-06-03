using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;

namespace ServerManager.ViewModels;

public class TemplatesViewModel : ObservableObject
{
	private readonly IConfigService _configService;

	private readonly IServerProcessManager _serverProcessManager;

	private readonly IActivityLogService _activityLog;

	private readonly DashboardViewModel _dashboardViewModel;

	private readonly ServersViewModel _serversViewModel;

	private readonly AppConfig _appConfig;

	private readonly string _customTemplatesPath = Path.Combine(AppContext.BaseDirectory, "server-templates.json");

	private ServerTemplatePreset? _selectedTemplate;

	private string _statusText = "Choose a template, then create a server from it.";

	public string Title => "Server Templates";

	public string Description => "Built-in and custom presets for quickly creating consistent dedicated servers.";

	public ObservableCollection<ServerTemplatePreset> Templates { get; } = new ObservableCollection<ServerTemplatePreset>();

	public ObservableCollection<string> PreviewLines { get; } = new ObservableCollection<string>();

	public IAsyncRelayCommand<ServerTemplatePreset> ApplyTemplateCommand { get; }

	public IAsyncRelayCommand SaveSelectedServerAsTemplateCommand { get; }

	public IAsyncRelayCommand RefreshTemplatesCommand { get; }

	public IAsyncRelayCommand DeleteSelectedTemplateCommand { get; }

	public ServerTemplatePreset? SelectedTemplate
	{
		get
		{
			return _selectedTemplate;
		}
		set
		{
			if (SetProperty(ref _selectedTemplate, value, "SelectedTemplate"))
			{
				UpdatePreview();
				ApplyTemplateCommand.NotifyCanExecuteChanged();
				DeleteSelectedTemplateCommand.NotifyCanExecuteChanged();
			}
		}
	}

	public string StatusText
	{
		get
		{
			return _statusText;
		}
		set
		{
			SetProperty(ref _statusText, value, "StatusText");
		}
	}

	public TemplatesViewModel(IConfigService configService, IServerProcessManager serverProcessManager, IActivityLogService activityLog, DashboardViewModel dashboardViewModel, ServersViewModel serversViewModel)
	{
		_configService = configService;
		_serverProcessManager = serverProcessManager;
		_activityLog = activityLog;
		_dashboardViewModel = dashboardViewModel;
		_serversViewModel = serversViewModel;
		_appConfig = Task.Run(() => _configService.LoadAsync()).GetAwaiter().GetResult();
		ApplyTemplateCommand = new AsyncRelayCommand<ServerTemplatePreset>(ApplyTemplateAsync, (ServerTemplatePreset? template) => template != null);
		SaveSelectedServerAsTemplateCommand = new AsyncRelayCommand(SaveSelectedServerAsTemplateAsync, () => _dashboardViewModel.SelectedServer != null);
		RefreshTemplatesCommand = new AsyncRelayCommand(RefreshTemplatesAsync);
		DeleteSelectedTemplateCommand = new AsyncRelayCommand(DeleteSelectedTemplateAsync, () => SelectedTemplate?.IsCustom == true);
		LoadTemplates();
	}

	private void LoadTemplates()
	{
		Templates.Clear();
		foreach (ServerTemplatePreset preset in GetSortedTemplates(GetBuiltInTemplates().Concat(LoadCustomTemplates())))
		{
			NormalizeTemplateGame(preset);
			Templates.Add(preset);
		}
		SelectedTemplate = Templates.FirstOrDefault();
		StatusText = $"Loaded {Templates.Count} server templates.";
	}

	private async Task RefreshTemplatesAsync()
	{
		LoadTemplates();
		await Task.CompletedTask;
	}

	private async Task DeleteSelectedTemplateAsync()
	{
		ServerTemplatePreset? template = SelectedTemplate;
		if (template?.IsCustom != true)
		{
			StatusText = "Only custom templates can be deleted.";
			return;
		}
		MessageBoxResult result = MessageBox.Show("Delete custom template '" + template.Name + "'?", "Delete template", MessageBoxButton.YesNo, MessageBoxImage.Warning);
		if (result != MessageBoxResult.Yes)
		{
			return;
		}
		List<ServerTemplatePreset> customTemplates = LoadCustomTemplates()
			.Where((ServerTemplatePreset x) => !x.Name.Equals(template.Name, StringComparison.OrdinalIgnoreCase))
			.ToList();
		await File.WriteAllTextAsync(_customTemplatesPath, JsonConvert.SerializeObject(customTemplates, Formatting.Indented));
		LoadTemplates();
		_activityLog.Info("Deleted custom template '" + template.Name + "'.");
		StatusText = "Deleted custom template '" + template.Name + "'.";
	}

	public async Task RenameSelectedTemplateAsync(string newName)
	{
		ServerTemplatePreset? template = SelectedTemplate;
		newName = (newName ?? string.Empty).Trim();
		if (template?.IsCustom != true)
		{
			StatusText = "Only custom templates can be renamed.";
			return;
		}
		if (string.IsNullOrWhiteSpace(newName))
		{
			StatusText = "Template name cannot be blank.";
			return;
		}
		List<ServerTemplatePreset> customTemplates = LoadCustomTemplates().ToList();
		ServerTemplatePreset? existing = customTemplates.FirstOrDefault((ServerTemplatePreset x) => x.Name.Equals(template.Name, StringComparison.OrdinalIgnoreCase));
		if (existing == null)
		{
			StatusText = "Custom template was not found.";
			return;
		}
		if (customTemplates.Any((ServerTemplatePreset x) => !x.Name.Equals(template.Name, StringComparison.OrdinalIgnoreCase) && x.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
			|| GetBuiltInTemplates().Any((ServerTemplatePreset x) => x.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
		{
			StatusText = "A template named '" + newName + "' already exists.";
			MessageBox.Show("A template named '" + newName + "' already exists.", "Rename template", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		string oldName = existing.Name;
		existing.Name = newName;
		existing.Description = "Custom template saved from " + newName + ".";
		await File.WriteAllTextAsync(_customTemplatesPath, JsonConvert.SerializeObject(customTemplates, Formatting.Indented));
		LoadTemplates();
		SelectedTemplate = Templates.FirstOrDefault((ServerTemplatePreset x) => x.IsCustom && x.Name == newName) ?? SelectedTemplate;
		_activityLog.Info("Renamed custom template '" + oldName + "' to '" + newName + "'.");
		StatusText = "Renamed custom template '" + oldName + "' to '" + newName + "'.";
	}

	private async Task ApplyTemplateAsync(ServerTemplatePreset? template)
	{
		if (template == null)
		{
			return;
		}
		ServerInstance server = CreateServerFromTemplate(template);
		AssignAvailablePorts(server);
		if (!_appConfig.Servers.Any((ServerInstance existing) => existing.Id == server.Id))
		{
			_appConfig.Servers.Add(server);
		}
		_serverProcessManager.AddServer(server);
		if (!_dashboardViewModel.Servers.Any((ServerInstance existing) => existing.Id == server.Id))
		{
			_dashboardViewModel.Servers.Add(server);
		}
		_dashboardViewModel.SelectedServer = server;
		if (!_serversViewModel.Servers.Any((ServerInstance existing) => existing.Id == server.Id))
		{
			_serversViewModel.Servers.Add(server);
		}
		_serversViewModel.SelectedServer = server;
		await _configService.SaveAsync(_appConfig);
		_activityLog.Info("Created server '" + server.Name + "' from template '" + template.Name + "'.");
		StatusText = "Created server '" + server.Name + "'. Open Dashboard or Servers to configure or install it.";
		MessageBox.Show("Created server '" + server.Name + "' from template '" + template.Name + "'.", "Template applied", MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	private async Task SaveSelectedServerAsTemplateAsync()
	{
		ServerInstance? server = _dashboardViewModel.SelectedServer;
		if (server == null)
		{
			StatusText = "Select a server on the Dashboard before saving a custom template.";
			return;
		}
		ObservableCollection<ServerTemplatePreset> customTemplates = new ObservableCollection<ServerTemplatePreset>(LoadCustomTemplates());
		string templateName = server.Name + " Custom";
		ServerTemplatePreset? existing = customTemplates.FirstOrDefault((ServerTemplatePreset x) => x.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
		ServerTemplatePreset template = new ServerTemplatePreset
		{
			Name = templateName,
			Description = "Custom template saved from " + server.Name + ".",
			Category = "Custom",
			GameId = server.GameId,
			MapName = server.MapName,
			MaxPlayers = server.MaxPlayers,
			XPMultiplier = server.XPMultiplier,
			HarvestMultiplier = server.HarvestMultiplier,
			TamingSpeedMultiplier = server.TamingSpeedMultiplier,
			DifficultyOffset = server.DifficultyOffset,
			ClusterId = server.ClusterId,
			ClusterDirectory = server.ClusterDirectory,
			CrossplayEnabled = server.CrossplayEnabled,
			BattleEyeEnabled = server.BattleEyeEnabled,
			AutoUpdateEnabled = server.AutoUpdateEnabled,
			AutoRestartOnCrash = server.AutoRestartOnCrash,
			NoTransferFromFiltering = server.NoTransferFromFiltering,
			NoTributeDownloads = server.NoTributeDownloads,
			PreventDownloadSurvivors = server.PreventDownloadSurvivors,
			PreventDownloadItems = server.PreventDownloadItems,
			PreventDownloadDinos = server.PreventDownloadDinos,
			PreventUploadSurvivors = server.PreventUploadSurvivors,
			PreventUploadItems = server.PreventUploadItems,
			PreventUploadDinos = server.PreventUploadDinos,
			IsCustom = true
		};
		if (existing != null)
		{
			int index = customTemplates.IndexOf(existing);
			customTemplates[index] = template;
		}
		else
		{
			customTemplates.Add(template);
		}
		await File.WriteAllTextAsync(_customTemplatesPath, JsonConvert.SerializeObject(customTemplates, Formatting.Indented));
		LoadTemplates();
		SelectedTemplate = Templates.FirstOrDefault((ServerTemplatePreset x) => x.IsCustom && x.Name == templateName) ?? SelectedTemplate;
		_activityLog.Info("Saved custom template '" + templateName + "'.");
		StatusText = "Saved custom template '" + templateName + "'.";
	}

	private ServerInstance CreateServerFromTemplate(ServerTemplatePreset template)
	{
		NormalizeTemplateGame(template);
		GameProfile profile = GameProfileCatalog.Get(template.GameId);
		ServerInstance server = new ServerInstance
		{
			Name = template.Name,
			GameId = profile.Id,
			AppId = profile.SteamAppId,
			ExecutableName = profile.DefaultExecutableName,
			MapName = template.MapName,
			InstallDirectory = GetDefaultServerInstallDirectory(template.Name),
			ClusterId = template.ClusterId,
			ClusterDirectory = GetTemplateClusterDirectory(template),
			MaxPlayers = template.MaxPlayers,
			XPMultiplier = template.XPMultiplier,
			HarvestMultiplier = template.HarvestMultiplier,
			TamingSpeedMultiplier = template.TamingSpeedMultiplier,
			DifficultyOffset = template.DifficultyOffset,
			CrossplayEnabled = template.CrossplayEnabled,
			BattleEyeEnabled = template.BattleEyeEnabled,
			AutoUpdateEnabled = template.AutoUpdateEnabled,
			AutoRestartOnCrash = template.AutoRestartOnCrash,
			NoTransferFromFiltering = template.NoTransferFromFiltering,
			NoTributeDownloads = template.NoTributeDownloads,
			PreventDownloadSurvivors = template.PreventDownloadSurvivors,
			PreventDownloadItems = template.PreventDownloadItems,
			PreventDownloadDinos = template.PreventDownloadDinos,
			PreventUploadSurvivors = template.PreventUploadSurvivors,
			PreventUploadItems = template.PreventUploadItems,
			PreventUploadDinos = template.PreventUploadDinos
		};
		server.Config.MaxPlayers = server.MaxPlayers;
		server.Config.Port = server.GamePort;
		server.Config.QueryPort = server.QueryPort;
		server.Config.RconPort = server.RconPort;
		server.Config.CrossplayEnabled = server.CrossplayEnabled;
		server.Config.UseBattleEye = server.BattleEyeEnabled;
		server.Config.NoTransferFromFiltering = server.NoTransferFromFiltering;
		Directory.CreateDirectory(server.InstallDirectory);
		Directory.CreateDirectory(server.ClusterDirectory);
		return server;
	}

	private void AssignAvailablePorts(ServerInstance server)
	{
		int gamePort = 7777;
		int queryPort = 27015;
		int rconPort = 32330;
		while (_appConfig.Servers.Any((ServerInstance existing) => existing.GamePort == gamePort || existing.QueryPort == queryPort || existing.RconPort == rconPort))
		{
			gamePort += 10;
			queryPort += 10;
			rconPort += 10;
		}
		server.GamePort = gamePort;
		server.QueryPort = queryPort;
		server.RconPort = rconPort;
		server.Config.Port = gamePort;
		server.Config.QueryPort = queryPort;
		server.Config.RconPort = rconPort;
	}

	private void UpdatePreview()
	{
		PreviewLines.Clear();
		if (SelectedTemplate == null)
		{
			return;
		}
		PreviewLines.Add("Game: " + SelectedTemplate.GameDisplayName);
		PreviewLines.Add("Map: " + SelectedTemplate.MapName);
		PreviewLines.Add("Players: " + SelectedTemplate.MaxPlayers);
		PreviewLines.Add("XP: " + SelectedTemplate.XPMultiplier + "x");
		PreviewLines.Add("Harvest: " + SelectedTemplate.HarvestMultiplier + "x");
		PreviewLines.Add("Taming: " + SelectedTemplate.TamingSpeedMultiplier + "x");
		PreviewLines.Add("Difficulty: " + SelectedTemplate.DifficultyOffset);
		PreviewLines.Add("Cluster ID: " + DisplayValue(SelectedTemplate.ClusterId));
		PreviewLines.Add("Cluster Directory: " + DisplayValue(GetTemplateClusterDirectory(SelectedTemplate)));
		PreviewLines.Add("Crossplay: " + (SelectedTemplate.CrossplayEnabled ? "Enabled" : "Disabled"));
		PreviewLines.Add("BattleEye: " + (SelectedTemplate.BattleEyeEnabled ? "Enabled" : "Disabled"));
		PreviewLines.Add("Transfers: " + (SelectedTemplate.NoTributeDownloads ? "Restricted" : "Allowed"));
		StatusText = "Selected template: " + SelectedTemplate.Name;
	}

	private static string GetDefaultServerInstallDirectory(string serverName)
	{
		string safeName = string.Join("_", (string.IsNullOrWhiteSpace(serverName) ? "New Server" : serverName).Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
		if (string.IsNullOrWhiteSpace(safeName))
		{
			safeName = "New Server";
		}
		return Path.Combine(AppContext.BaseDirectory, "servers", safeName);
	}

	private static IEnumerable<ServerTemplatePreset> GetSortedTemplates(IEnumerable<ServerTemplatePreset> templates)
	{
		return templates
			.Select(delegate(ServerTemplatePreset template)
			{
				NormalizeTemplateGame(template);
				return template;
			})
			.OrderBy((ServerTemplatePreset template) => template.GameDisplayName, StringComparer.OrdinalIgnoreCase)
			.ThenBy((ServerTemplatePreset template) => template.Category, StringComparer.OrdinalIgnoreCase)
			.ThenBy((ServerTemplatePreset template) => template.Name, StringComparer.OrdinalIgnoreCase);
	}

	private static void NormalizeTemplateGame(ServerTemplatePreset template)
	{
		if (template != null && string.IsNullOrWhiteSpace(template.GameId))
		{
			template.GameId = GameProfileCatalog.Default.Id;
		}
	}

	private static string GetTemplateClusterDirectory(ServerTemplatePreset template)
	{
		if (!string.IsNullOrWhiteSpace(template.ClusterDirectory))
		{
			return template.ClusterDirectory;
		}
		return Path.Combine(AppContext.BaseDirectory, "clusters", string.IsNullOrWhiteSpace(template.ClusterId) ? "default" : template.ClusterId);
	}

	private static string DisplayValue(string value)
	{
		return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
	}

	private IReadOnlyList<ServerTemplatePreset> LoadCustomTemplates()
	{
		if (!File.Exists(_customTemplatesPath))
		{
			return Array.Empty<ServerTemplatePreset>();
		}
		try
		{
			return JsonConvert.DeserializeObject<IReadOnlyList<ServerTemplatePreset>>(File.ReadAllText(_customTemplatesPath)) ?? Array.Empty<ServerTemplatePreset>();
		}
		catch
		{
			return Array.Empty<ServerTemplatePreset>();
		}
	}

	private static IReadOnlyList<ServerTemplatePreset> GetBuiltInTemplates()
	{
		return new[]
		{
			new ServerTemplatePreset { Name = "Official PvE", Category = "Official", Description = "Close to official PvE rules with transfers allowed and low rates.", MapName = "TheIsland_WP", MaxPlayers = 70, XPMultiplier = 1.0, HarvestMultiplier = 1.0, TamingSpeedMultiplier = 1.0, DifficultyOffset = 1.0, CrossplayEnabled = true, BattleEyeEnabled = true },
			new ServerTemplatePreset { Name = "Official PvP", Category = "Official", Description = "Official-style PvP with BattleEye and standard progression.", MapName = "TheIsland_WP", MaxPlayers = 70, XPMultiplier = 1.0, HarvestMultiplier = 1.0, TamingSpeedMultiplier = 1.0, DifficultyOffset = 1.0, CrossplayEnabled = true, BattleEyeEnabled = true },
			new ServerTemplatePreset { Name = "Solo Boosted", Category = "Boosted", Description = "Small local/co-op server with faster progression and easier resource gathering.", MapName = "TheIsland_WP", MaxPlayers = 10, XPMultiplier = 3.0, HarvestMultiplier = 5.0, TamingSpeedMultiplier = 8.0, DifficultyOffset = 1.0, CrossplayEnabled = true },
			new ServerTemplatePreset { Name = "FiberCraft", Category = "Boosted", Description = "High-rate sandbox starter for quick building and testing.", MapName = "TheIsland_WP", MaxPlayers = 20, XPMultiplier = 10.0, HarvestMultiplier = 25.0, TamingSpeedMultiplier = 25.0, DifficultyOffset = 1.0, CrossplayEnabled = true },
			new ServerTemplatePreset { Name = "Hardcore", Category = "Challenge", Description = "Lower rates, tighter transfers, and a slower survival pace.", MapName = "ScorchedEarth_WP", MaxPlayers = 30, XPMultiplier = 0.75, HarvestMultiplier = 0.75, TamingSpeedMultiplier = 0.75, DifficultyOffset = 1.0, CrossplayEnabled = true, BattleEyeEnabled = true, NoTributeDownloads = true, PreventDownloadDinos = true, PreventDownloadItems = true },
			new ServerTemplatePreset { Name = "Casual PvE", Category = "Casual", Description = "Comfortable rates for friends and community PvE servers.", MapName = "TheCenter_WP", MaxPlayers = 30, XPMultiplier = 2.0, HarvestMultiplier = 3.0, TamingSpeedMultiplier = 5.0, DifficultyOffset = 1.0, CrossplayEnabled = true },
			new ServerTemplatePreset { Name = "Breeding Server", Category = "Specialized", Description = "Boosted progression template intended for breeding-focused worlds.", MapName = "TheIsland_WP", MaxPlayers = 20, XPMultiplier = 2.0, HarvestMultiplier = 4.0, TamingSpeedMultiplier = 10.0, DifficultyOffset = 1.0, CrossplayEnabled = true, NoTransferFromFiltering = false }
		};
	}
}

public class ServerTemplatePreset
{
	public string Name { get; set; } = "New Template";

	public string Description { get; set; } = string.Empty;

	public string Category { get; set; } = "Custom";

	public string GameId { get; set; } = GameProfileIds.ArkSurvivalAscended;

	[JsonIgnore]
	public string GameDisplayName => GameProfileCatalog.Get(GameId).DisplayName;

	public string MapName { get; set; } = "TheIsland_WP";

	public int MaxPlayers { get; set; } = 10;

	public double XPMultiplier { get; set; } = 1.0;

	public double HarvestMultiplier { get; set; } = 1.0;

	public double TamingSpeedMultiplier { get; set; } = 1.0;

	public double DifficultyOffset { get; set; } = 1.0;

	public string ClusterId { get; set; } = string.Empty;

	public string ClusterDirectory { get; set; } = string.Empty;

	public bool CrossplayEnabled { get; set; } = true;

	public bool BattleEyeEnabled { get; set; }

	public bool AutoUpdateEnabled { get; set; } = true;

	public bool AutoRestartOnCrash { get; set; } = true;

	public bool NoTransferFromFiltering { get; set; } = true;

	public bool NoTributeDownloads { get; set; }

	public bool PreventDownloadSurvivors { get; set; }

	public bool PreventDownloadItems { get; set; }

	public bool PreventDownloadDinos { get; set; }

	public bool PreventUploadSurvivors { get; set; }

	public bool PreventUploadItems { get; set; }

	public bool PreventUploadDinos { get; set; }

	public bool IsCustom { get; set; }
}
