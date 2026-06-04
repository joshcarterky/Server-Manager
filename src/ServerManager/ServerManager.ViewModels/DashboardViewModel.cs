using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class DashboardViewModel : ObservableObject
{
	private readonly IServerProcessManager _serverProcessManager;

	private readonly IConfigService _configService;

	private readonly ILoggingService _loggingService;

	private readonly IActivityLogService _activityLog;

	private readonly ICurseForgeService _curseForgeService;

	private readonly ConsoleViewModel _consoleViewModel;

	private readonly AppConfig _appConfig;

	private ServerInstance? _selectedServer;

	private ModEntry? _selectedServerMod;

	private CurseForgeModResult? _selectedCurseForgeResult;

	private string _curseForgeSearchText = string.Empty;

	private string _curseForgeStatus = "Search works without an API key for common ASA mods. Use Add ID for any Project ID.";

	private string _topDownloadsStatus = "Showing built-in top downloads. Refresh uses CurseForge when available.";

	public ObservableCollection<ServerInstance> Servers { get; } = new ObservableCollection<ServerInstance>();

	public ObservableCollection<GameProfile> GameProfiles { get; } = new ObservableCollection<GameProfile>(GameProfileCatalog.All);

	public event Action<ServerInstance, bool>? ConfigureRequested;


	public ObservableCollection<ActivityLogEntry> ActivityEntries => _activityLog.Entries;

	public ConsoleViewModel ServerConsole => _consoleViewModel;

	public ObservableCollection<ModEntry> SelectedServerMods { get; } = new ObservableCollection<ModEntry>();


	public ObservableCollection<CurseForgeModResult> CurseForgeResults { get; } = new ObservableCollection<CurseForgeModResult>();


	public ObservableCollection<CurseForgeModResult> TopDownloadedMods { get; } = new ObservableCollection<CurseForgeModResult>();


	public ObservableCollection<string> MapNames { get; } = new ObservableCollection<string>
	{
		"TheIsland_WP", "ScorchedEarth_WP", "TheCenter_WP", "Aberration_WP", "Extinction_WP", "Ragnarok_WP",
		"Valguero_WP", "Astraeos_WP", "LostColony_WP", "BobsMissions_WP"
	};


	public ObservableCollection<IniSettingViewModel> IniSettings { get; } = new ObservableCollection<IniSettingViewModel>();


	public IRelayCommand RefreshCommand { get; }

	public IRelayCommand<ServerInstance> SelectServerCommand { get; }

	public IAsyncRelayCommand AddServerCommand { get; }

	public IAsyncRelayCommand RemoveServerCommand { get; }

	public IAsyncRelayCommand SaveServerCommand { get; }

	public IAsyncRelayCommand StartServerCommand { get; }

	public IAsyncRelayCommand StopServerCommand { get; }

	public IAsyncRelayCommand InstallServerCommand { get; }

	public IAsyncRelayCommand<ServerInstance> StartServerTileCommand { get; }

	public IAsyncRelayCommand<ServerInstance> StopServerTileCommand { get; }

	public IAsyncRelayCommand<ServerInstance> InstallServerTileCommand { get; }

	public IRelayCommand ClearActivityCommand { get; }

	public IAsyncRelayCommand SearchCurseForgeCommand { get; }

	public IAsyncRelayCommand AddCurseForgeModCommand { get; }

	public IAsyncRelayCommand<CurseForgeModResult> AddModResultCommand { get; }

	public IAsyncRelayCommand RefreshTopDownloadsCommand { get; }

	public IAsyncRelayCommand AddManualModCommand { get; }

	public IAsyncRelayCommand RemoveSelectedModCommand { get; }

	public IRelayCommand MoveSelectedModUpCommand { get; }

	public IRelayCommand MoveSelectedModDownCommand { get; }

	public string CurseForgeApiKey
	{
		get
		{
			return _appConfig.CurseForgeApiKey;
		}
		set
		{
			if (!(_appConfig.CurseForgeApiKey == value))
			{
				_appConfig.CurseForgeApiKey = value;
				OnPropertyChanged("CurseForgeApiKey");
			}
		}
	}

	public string CurseForgeSearchText
	{
		get
		{
			return _curseForgeSearchText;
		}
		set
		{
			SetProperty(ref _curseForgeSearchText, value, "CurseForgeSearchText");
			SearchCurseForgeCommand.NotifyCanExecuteChanged();
			AddManualModCommand.NotifyCanExecuteChanged();
		}
	}

	public string CurseForgeStatus
	{
		get
		{
			return _curseForgeStatus;
		}
		set
		{
			SetProperty(ref _curseForgeStatus, value, "CurseForgeStatus");
		}
	}

	public string TopDownloadsStatus
	{
		get
		{
			return _topDownloadsStatus;
		}
		set
		{
			SetProperty(ref _topDownloadsStatus, value, "TopDownloadsStatus");
		}
	}

	public ModEntry? SelectedServerMod
	{
		get
		{
			return _selectedServerMod;
		}
		set
		{
			SetProperty(ref _selectedServerMod, value, "SelectedServerMod");
			RemoveSelectedModCommand.NotifyCanExecuteChanged();
			MoveSelectedModUpCommand.NotifyCanExecuteChanged();
			MoveSelectedModDownCommand.NotifyCanExecuteChanged();
		}
	}

	public CurseForgeModResult? SelectedCurseForgeResult
	{
		get
		{
			return _selectedCurseForgeResult;
		}
		set
		{
			SetProperty(ref _selectedCurseForgeResult, value, "SelectedCurseForgeResult");
			AddCurseForgeModCommand.NotifyCanExecuteChanged();
			AddModResultCommand.NotifyCanExecuteChanged();
		}
	}

	public ServerInstance? SelectedServer
	{
		get
		{
			return _selectedServer;
		}
		set
		{
			if (SetProperty(ref _selectedServer, value, "SelectedServer") && value != null)
			{
				_activityLog.Info("Selected server '" + value.Name + "'.");
				_consoleViewModel.SelectedServer = value;
			}
			SyncSelectedServerMods();
			SyncIniSettingsFromSelectedServer();
			ReloadIniSettingsFromSelectedServerFiles();
			SaveServerCommand.NotifyCanExecuteChanged();
			StartServerCommand.NotifyCanExecuteChanged();
			StopServerCommand.NotifyCanExecuteChanged();
			InstallServerCommand.NotifyCanExecuteChanged();
			RemoveServerCommand.NotifyCanExecuteChanged();
			AddCurseForgeModCommand.NotifyCanExecuteChanged();
			AddModResultCommand.NotifyCanExecuteChanged();
			RemoveSelectedModCommand.NotifyCanExecuteChanged();
			AddManualModCommand.NotifyCanExecuteChanged();
		}
	}

	public int ManagedCount => Servers.Count;

	public int OnlineCount => Servers.Count((ServerInstance server) => _serverProcessManager.GetStatus(server)?.IsOnline ?? false);

	public int OfflineCount => ManagedCount - OnlineCount;

	public int TotalPlayers => Servers.Sum((ServerInstance server) => _serverProcessManager.GetStatus(server)?.PlayerCount ?? 0);

	public int TotalSlots => Servers.Sum((ServerInstance server) => server.MaxPlayers);

	public int ModCount => Servers.Sum((ServerInstance server) => server.Mods.Count);

	public string FleetState
	{
		get
		{
			if (OnlineCount <= 0)
			{
				return "Offline";
			}
			return "Active";
		}
	}

	private static string DefaultServerInstallDirectory => Path.Combine(AppContext.BaseDirectory, "servers");

	public static string DefaultClusterDirectory => Path.Combine(AppContext.BaseDirectory, "clusters", "default");

	public DashboardViewModel(IServerProcessManager serverProcessManager, IConfigService configService, ILoggingService loggingService, IActivityLogService activityLog, ICurseForgeService curseForgeService, ConsoleViewModel consoleViewModel)
	{
		_serverProcessManager = serverProcessManager;
		_configService = configService;
		_loggingService = loggingService;
		_activityLog = activityLog;
		_curseForgeService = curseForgeService;
		_consoleViewModel = consoleViewModel;
		_appConfig = Task.Run(() => _configService.LoadAsync()).GetAwaiter().GetResult();
		foreach (CurseForgeModResult topDownloadedAsaMod in _curseForgeService.GetTopDownloadedAsaMods())
		{
			TopDownloadedMods.Add(topDownloadedAsaMod);
		}
		RefreshCommand = new RelayCommand(Refresh);
		SelectServerCommand = new RelayCommand<ServerInstance>(delegate(ServerInstance? server)
		{
			SelectedServer = server;
		});
		AddServerCommand = new AsyncRelayCommand(AddServerAsync);
		RemoveServerCommand = new AsyncRelayCommand(RemoveServerAsync, () => SelectedServer != null);
		SaveServerCommand = new AsyncRelayCommand(SaveServerAsync, () => SelectedServer != null);
		StartServerCommand = new AsyncRelayCommand(StartServerAsync, () => SelectedServer != null);
		StopServerCommand = new AsyncRelayCommand(StopServerAsync, () => SelectedServer != null);
		InstallServerCommand = new AsyncRelayCommand(InstallServerAsync, () => SelectedServer != null);
		StartServerTileCommand = new AsyncRelayCommand<ServerInstance>((ServerInstance? server) => RunForServerAsync(server, StartServerAsync), (ServerInstance? server) => server != null);
		StopServerTileCommand = new AsyncRelayCommand<ServerInstance>((ServerInstance? server) => RunForServerAsync(server, StopServerAsync), (ServerInstance? server) => server != null);
		InstallServerTileCommand = new AsyncRelayCommand<ServerInstance>((ServerInstance? server) => RunForServerAsync(server, InstallServerAsync), (ServerInstance? server) => server != null);
		ClearActivityCommand = new RelayCommand(_activityLog.Clear);
		SearchCurseForgeCommand = new AsyncRelayCommand(SearchCurseForgeAsync, () => !string.IsNullOrWhiteSpace(CurseForgeSearchText));
		AddCurseForgeModCommand = new AsyncRelayCommand(AddCurseForgeModAsync, () => SelectedServer != null && SelectedCurseForgeResult != null);
		AddModResultCommand = new AsyncRelayCommand<CurseForgeModResult>(AddModResultAsync, (CurseForgeModResult? mod) => SelectedServer != null && mod != null);
		RefreshTopDownloadsCommand = new AsyncRelayCommand(RefreshTopDownloadsAsync);
		AddManualModCommand = new AsyncRelayCommand(AddManualModAsync, () => SelectedServer != null && !string.IsNullOrWhiteSpace(CurseForgeSearchText));
		RemoveSelectedModCommand = new AsyncRelayCommand(RemoveSelectedModAsync, () => SelectedServer != null && SelectedServerMod != null);
		MoveSelectedModUpCommand = new RelayCommand(delegate
		{
			MoveSelectedMod(-1);
		}, () => SelectedServerMod != null && SelectedServerMods.IndexOf(SelectedServerMod) > 0);
		MoveSelectedModDownCommand = new RelayCommand(delegate
		{
			MoveSelectedMod(1);
		}, () => SelectedServerMod != null && SelectedServerMods.IndexOf(SelectedServerMod) >= 0 && SelectedServerMods.IndexOf(SelectedServerMod) < SelectedServerMods.Count - 1);
		LoadIniSettings();
		_activityLog.Info("Dashboard loaded.");
		Refresh();
		Task.Run((Func<Task?>)UpdateMetricsLoop);
	}

	private void Refresh()
	{
		_activityLog.Info("Refreshing server list.");
		ApplyDefaultDirectories();
		_serverProcessManager.InitializeAsync(_appConfig).GetAwaiter().GetResult();
		Guid? selectedId = SelectedServer?.Id;
		Servers.Clear();
		foreach (ServerInstance server in _appConfig.Servers)
		{
			Servers.Add(server);
		}
		SelectedServer = Servers.FirstOrDefault(delegate(ServerInstance x)
		{
			Guid id = x.Id;
			Guid? guid = selectedId;
			return id == guid;
		}) ?? Servers.FirstOrDefault();
		NotifyMetricsChanged();
	}

	public void RequestConfigure(ServerInstance server, bool console)
	{
		ServerInstance dashboardServer = Servers.FirstOrDefault((ServerInstance x) => x.Id == server.Id) ?? server;
		SelectedServer = dashboardServer;
		ConfigureRequested?.Invoke(dashboardServer, console);
	}

	private async Task AddServerAsync()
	{
		ServerInstance server = new ServerInstance
		{
			Name = "New Server",
			GameId = GameProfileCatalog.Default.Id,
			AppId = GameProfileCatalog.Default.SteamAppId,
			ExecutableName = GameProfileCatalog.Default.DefaultExecutableName,
			MapName = (MapNames.FirstOrDefault() ?? "TheIsland_WP"),
			InstallDirectory = GetDefaultServerInstallDirectory("New Server"),
			ClusterDirectory = DefaultClusterDirectory
		};
		_appConfig.Servers.Add(server);
		_serverProcessManager.AddServer(server);
		Servers.Add(server);
		SelectedServer = server;
		await _configService.SaveAsync(_appConfig);
		_activityLog.Info("Created new server '" + server.Name + "'.");
		NotifyMetricsChanged();
	}

	public async Task AddExistingServerAsync(string selectedDirectory)
	{
		if (!string.IsNullOrWhiteSpace(selectedDirectory))
		{
			string sourceInstallDirectory = ResolveExistingInstallDirectory(selectedDirectory);
			string savedDirectory = ResolveExistingSavedDirectory(sourceInstallDirectory, selectedDirectory);
			Dictionary<string, string> importedSettings = ReadImportedServerSettings(savedDirectory);
			string fallbackName = new DirectoryInfo(sourceInstallDirectory).Name;
			ServerInstance server = new ServerInstance
			{
				Name = (string.IsNullOrWhiteSpace(fallbackName) ? "Existing Server" : fallbackName),
				GameId = GameProfileCatalog.Default.Id,
				AppId = GameProfileCatalog.Default.SteamAppId,
				ExecutableName = GameProfileCatalog.Default.DefaultExecutableName,
				InstallDirectory = sourceInstallDirectory,
				MapName = DetectImportedMapName(savedDirectory, importedSettings),
				ClusterDirectory = DefaultClusterDirectory
			};
			ApplyImportedServerSettings(server, importedSettings);
			string installDirectory = GetAvailableDefaultServerInstallDirectory(server.Name);
			if (!string.Equals(NormalizeDirectoryPath(sourceInstallDirectory), NormalizeDirectoryPath(installDirectory), StringComparison.OrdinalIgnoreCase))
			{
				await Task.Run(() => CopyDirectory(sourceInstallDirectory, installDirectory));
			}
			server.InstallDirectory = installDirectory;
			ApplyDefaultDirectories(server);
			NormalizeServerSettings(server);
			_appConfig.Servers.Add(server);
			_serverProcessManager.AddServer(server);
			Servers.Add(server);
			SelectedServer = server;
			await _configService.SaveAsync(_appConfig);
			_activityLog.Info($"Imported existing server '{server.Name}' from {sourceInstallDirectory} into {installDirectory}.");
			NotifyMetricsChanged();
		}
	}

	private async Task RemoveServerAsync()
	{
		if (SelectedServer != null)
		{
			ServerInstance server = SelectedServer;
			await _serverProcessManager.StopServerAsync(server);
			_serverProcessManager.RemoveServer(server);
			_appConfig.Servers.RemoveAll((ServerInstance x) => x.Id == server.Id);
			Servers.Remove(server);
			SelectedServer = Servers.FirstOrDefault();
			await _configService.SaveAsync(_appConfig);
			_activityLog.Warning("Removed server '" + server.Name + "'.");
			NotifyMetricsChanged();
		}
	}

	private async Task SaveServerAsync()
	{
		if (SelectedServer != null)
		{
			ApplyDefaultDirectories(SelectedServer);
			SyncModsToSelectedServer();
			NormalizeServerSettings(SelectedServer);
			SyncIniSettingsFromSelectedServer();
			await _configService.SaveAsync(_appConfig);
			_serverProcessManager.AddServer(SelectedServer);
			RefreshSelectedServerBindings();
			_activityLog.Info("Saved server settings for '" + SelectedServer.Name + "'.");
			NotifyMetricsChanged();
			MessageBox.Show("Server settings saved.", "Dashboard", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
	}

	private async Task SearchCurseForgeAsync()
	{
		_ = 1;
		try
		{
			CurseForgeStatus = "Searching CurseForge...";
			_activityLog.Info("Searching CurseForge for '" + CurseForgeSearchText + "'.");
			await _configService.SaveAsync(_appConfig);
			CurseForgeResults.Clear();
			IReadOnlyList<CurseForgeModResult> readOnlyList = await _curseForgeService.SearchAsaModsAsync(CurseForgeApiKey, CurseForgeSearchText);
			foreach (CurseForgeModResult item in readOnlyList)
			{
				CurseForgeResults.Add(item);
			}
			SelectedCurseForgeResult = CurseForgeResults.FirstOrDefault();
			CurseForgeStatus = ((readOnlyList.Count == 0) ? "No CurseForge mods found." : $"Found {readOnlyList.Count} CurseForge mods.");
			_activityLog.Info(CurseForgeStatus);
		}
		catch (Exception ex)
		{
			CurseForgeStatus = ex.Message;
			_loggingService.Logger.Error(ex, "CurseForge search failed");
			_activityLog.Error("CurseForge search failed: " + ex.Message);
		}
	}

	private async Task RefreshTopDownloadsAsync()
	{
		_ = 1;
		try
		{
			TopDownloadsStatus = "Refreshing top downloads...";
			_activityLog.Info("Refreshing top downloaded ASA mods.");
			await _configService.SaveAsync(_appConfig);
			IReadOnlyList<CurseForgeModResult> source = await _curseForgeService.RefreshTopDownloadedAsaModsAsync(CurseForgeApiKey);
			TopDownloadedMods.Clear();
			foreach (CurseForgeModResult item in source.Take(50))
			{
				TopDownloadedMods.Add(item);
			}
			TopDownloadsStatus = ((TopDownloadedMods.Count >= 50) ? "Showing refreshed top 50 downloaded mods." : $"Showing {TopDownloadedMods.Count} top downloaded mods from the available catalog.");
			_activityLog.Info(TopDownloadsStatus);
		}
		catch (Exception ex)
		{
			TopDownloadsStatus = "Refresh failed: " + ex.Message;
			_loggingService.Logger.Error(ex, "Failed to refresh top downloaded mods");
			_activityLog.Error(TopDownloadsStatus);
		}
	}

	private async Task AddCurseForgeModAsync()
	{
		if (SelectedCurseForgeResult != null)
		{
			await AddModResultAsync(SelectedCurseForgeResult);
		}
	}

	public Task AddBrowserModAsync(CurseForgeModResult mod)
	{
		return AddModResultAsync(mod);
	}

	private async Task AddModResultAsync(CurseForgeModResult? mod)
	{
		CurseForgeModResult mod2 = mod;
		if (SelectedServer != null && mod2 != null)
		{
			mod2 = await EnrichModResultAsync(mod2);
			if (SelectedServerMods.Any((ModEntry x) => x.WorkshopId == mod2.ProjectId))
			{
				CurseForgeStatus = "That mod is already in the selected server load order.";
				_activityLog.Warning(CurseForgeStatus);
				return;
			}
			SelectedServerMods.Add(new ModEntry
			{
				Title = mod2.Name,
				WorkshopId = mod2.ProjectId,
				CurseForgeFileId = mod2.LatestFileId,
				Author = mod2.Author,
				ThumbnailUrl = mod2.ThumbnailUrl,
				ProjectUrl = mod2.ProjectUrl,
				LatestFileName = mod2.LatestFileName,
				DownloadUrl = mod2.DownloadUrl,
				FileSizeText = mod2.FileSizeText,
				LastUpdatedText = mod2.LastUpdatedText,
				DownloadCountText = mod2.DownloadCountText,
				AutoUpdate = true
			});
			await SaveSelectedServerModsAsync($"Added CurseForge mod '{mod2.Name}' ({mod2.ProjectId}) to '{SelectedServer.Name}'.");
		}
	}

	public async Task EnrichSelectedServerModsAsync()
	{
		if (SelectedServer == null)
		{
			return;
		}
		bool changed = false;
		foreach (ModEntry mod in SelectedServerMods)
		{
			if (string.IsNullOrWhiteSpace(mod.WorkshopId) || HasUsefulModMetadata(mod))
			{
				continue;
			}
			CurseForgeModResult? metadata = (await _curseForgeService.SearchAsaModsAsync(string.Empty, mod.WorkshopId)).FirstOrDefault((CurseForgeModResult item) => item.ProjectId == mod.WorkshopId);
			if (metadata == null)
			{
				continue;
			}
			ApplyMetadata(mod, metadata);
			changed = true;
		}
		if (changed)
		{
			SyncModsToSelectedServer();
			await _configService.SaveAsync(_appConfig);
			OnPropertyChanged("SelectedServerMods");
			_activityLog.Info("Updated installed mod metadata for '" + SelectedServer.Name + "'.");
		}
	}

	private async Task<CurseForgeModResult> EnrichModResultAsync(CurseForgeModResult mod)
	{
		if (string.IsNullOrWhiteSpace(mod.ProjectId) || HasUsefulModMetadata(mod))
		{
			return mod;
		}
		CurseForgeModResult? metadata = (await _curseForgeService.SearchAsaModsAsync(string.Empty, mod.ProjectId)).FirstOrDefault((CurseForgeModResult item) => item.ProjectId == mod.ProjectId);
		if (metadata == null)
		{
			return mod;
		}
		return new CurseForgeModResult
		{
			ProjectId = mod.ProjectId,
			Name = string.IsNullOrWhiteSpace(mod.Name) || mod.Name.StartsWith("CurseForge Mod ", StringComparison.OrdinalIgnoreCase) ? metadata.Name : mod.Name,
			Summary = string.IsNullOrWhiteSpace(mod.Summary) ? metadata.Summary : mod.Summary,
			Author = string.IsNullOrWhiteSpace(mod.Author) ? metadata.Author : mod.Author,
			ThumbnailUrl = string.IsNullOrWhiteSpace(mod.ThumbnailUrl) ? metadata.ThumbnailUrl : mod.ThumbnailUrl,
			ProjectUrl = string.IsNullOrWhiteSpace(mod.ProjectUrl) ? metadata.ProjectUrl : mod.ProjectUrl,
			LatestFileName = string.IsNullOrWhiteSpace(mod.LatestFileName) ? metadata.LatestFileName : mod.LatestFileName,
			LatestFileId = string.IsNullOrWhiteSpace(mod.LatestFileId) ? metadata.LatestFileId : mod.LatestFileId,
			DownloadUrl = string.IsNullOrWhiteSpace(mod.DownloadUrl) ? metadata.DownloadUrl : mod.DownloadUrl,
			Category = string.IsNullOrWhiteSpace(mod.Category) ? metadata.Category : mod.Category,
			FileSizeBytes = mod.FileSizeBytes > 0 ? mod.FileSizeBytes : metadata.FileSizeBytes,
			DownloadCount = mod.DownloadCount > 0 ? mod.DownloadCount : metadata.DownloadCount,
			DownloadBarWidth = mod.DownloadBarWidth > 0 ? mod.DownloadBarWidth : metadata.DownloadBarWidth,
			LastUpdated = mod.LastUpdated ?? metadata.LastUpdated
		};
	}

	private static bool HasUsefulModMetadata(ModEntry mod)
	{
		return !string.IsNullOrWhiteSpace(mod.ThumbnailUrl) && !string.IsNullOrWhiteSpace(mod.ProjectUrl) && !string.IsNullOrWhiteSpace(mod.Author);
	}

	private static bool HasUsefulModMetadata(CurseForgeModResult mod)
	{
		return !string.IsNullOrWhiteSpace(mod.ThumbnailUrl) && !string.IsNullOrWhiteSpace(mod.ProjectUrl) && !string.IsNullOrWhiteSpace(mod.Author);
	}

	private static void ApplyMetadata(ModEntry mod, CurseForgeModResult metadata)
	{
		if (string.IsNullOrWhiteSpace(mod.Title) || mod.Title.StartsWith("CurseForge Mod ", StringComparison.OrdinalIgnoreCase) || mod.Title.Contains(" - Ark Survival Ascended Mods", StringComparison.OrdinalIgnoreCase))
		{
			mod.Title = metadata.Name;
		}
		if (string.IsNullOrWhiteSpace(mod.Author))
		{
			mod.Author = metadata.Author;
		}
		if (string.IsNullOrWhiteSpace(mod.ThumbnailUrl))
		{
			mod.ThumbnailUrl = metadata.ThumbnailUrl;
		}
		if (string.IsNullOrWhiteSpace(mod.ProjectUrl))
		{
			mod.ProjectUrl = metadata.ProjectUrl;
		}
		if (string.IsNullOrWhiteSpace(mod.LatestFileName))
		{
			mod.LatestFileName = metadata.LatestFileName;
		}
		if (string.IsNullOrWhiteSpace(mod.CurseForgeFileId))
		{
			mod.CurseForgeFileId = metadata.LatestFileId;
		}
		if (string.IsNullOrWhiteSpace(mod.DownloadUrl))
		{
			mod.DownloadUrl = metadata.DownloadUrl;
		}
		if (string.IsNullOrWhiteSpace(mod.FileSizeText) || mod.FileSizeText == "Unknown size")
		{
			mod.FileSizeText = metadata.FileSizeText;
		}
		if (string.IsNullOrWhiteSpace(mod.LastUpdatedText) || mod.LastUpdatedText == "Unknown")
		{
			mod.LastUpdatedText = metadata.LastUpdatedText;
		}
		if (string.IsNullOrWhiteSpace(mod.DownloadCountText) || mod.DownloadCountText == "Unknown")
		{
			mod.DownloadCountText = metadata.DownloadCountText;
		}
	}

	private async Task AddManualModAsync()
	{
		if (SelectedServer != null)
		{
			string projectId = CurseForgeSearchText.Trim();
			if (!projectId.All(char.IsDigit))
			{
				CurseForgeStatus = "Manual add expects a numeric CurseForge Project ID.";
				_activityLog.Warning(CurseForgeStatus);
				return;
			}
			if (SelectedServerMods.Any((ModEntry x) => x.WorkshopId == projectId))
			{
				CurseForgeStatus = "That Project ID is already in the selected server load order.";
				_activityLog.Warning(CurseForgeStatus);
				return;
			}
			SelectedServerMods.Add(new ModEntry
			{
				Title = "CurseForge Mod " + projectId,
				WorkshopId = projectId,
				AutoUpdate = true
			});
			await SaveSelectedServerModsAsync($"Added CurseForge Project ID {projectId} to '{SelectedServer.Name}'.");
		}
	}

	private async Task RemoveSelectedModAsync()
	{
		if (SelectedServer != null && SelectedServerMod != null)
		{
			ModEntry selectedServerMod = SelectedServerMod;
			SelectedServerMods.Remove(selectedServerMod);
			SelectedServerMod = null;
			await SaveSelectedServerModsAsync($"Removed mod '{selectedServerMod.Title}' ({selectedServerMod.WorkshopId}) from '{SelectedServer.Name}'.");
		}
	}

	private void MoveSelectedMod(int offset)
	{
		if (SelectedServerMod != null)
		{
			int num = SelectedServerMods.IndexOf(SelectedServerMod);
			int num2 = num + offset;
			if (num >= 0 && num2 >= 0 && num2 < SelectedServerMods.Count)
			{
				SelectedServerMods.Move(num, num2);
				UpdateSelectedServerModOrders();
				SaveSelectedServerModsAsync("Updated mod load order for '" + SelectedServer?.Name + "'.");
				MoveSelectedModUpCommand.NotifyCanExecuteChanged();
				MoveSelectedModDownCommand.NotifyCanExecuteChanged();
			}
		}
	}

	private async Task RunForServerAsync(ServerInstance? server, Func<Task> action)
	{
		if (server != null)
		{
			SelectedServer = server;
			await action();
		}
	}

	private async Task StartServerAsync()
	{
		if (SelectedServer == null)
		{
			return;
		}
		try
		{
			NormalizeServerSettings(SelectedServer);
			ApplyIniSettingsToSelectedServer();
			ApplyDefaultDirectories(SelectedServer);
			SaveIniSettingsToSelectedServerFiles();
			await _configService.SaveAsync(_appConfig);
			_serverProcessManager.AddServer(SelectedServer);
			_activityLog.Info("Starting server '" + SelectedServer.Name + "'.");
			await _serverProcessManager.StartServerAsync(SelectedServer);
			_activityLog.Info("Server '" + SelectedServer.Name + "' start command completed.");
			_activityLog.Info("Connecting RCON console for '" + SelectedServer.Name + "'.");
			_ = _consoleViewModel.ConnectToServerAsync(SelectedServer, showErrorDialog: false, retryCount: 24, retryDelayMs: 5000);
		}
		catch (Exception ex)
		{
			_loggingService.Logger.Error(ex, "Failed to start server {ServerName} from dashboard", SelectedServer.Name);
			_activityLog.Error("Failed to start '" + SelectedServer.Name + "': " + ex.Message);
			MessageBox.Show(ex.Message, "Start server failed", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private async Task StopServerAsync()
	{
		if (SelectedServer != null)
		{
			try
			{
				_activityLog.Info("Stopping server '" + SelectedServer.Name + "'.");
				await _serverProcessManager.StopServerAsync(SelectedServer);
				_activityLog.Info("Server '" + SelectedServer.Name + "' stop command completed.");
			}
			catch (Exception ex)
			{
				_loggingService.Logger.Error(ex, "Failed to stop server {ServerName} from dashboard", SelectedServer.Name);
				_activityLog.Error("Failed to stop '" + SelectedServer.Name + "': " + ex.Message);
				MessageBox.Show(ex.Message, "Stop server failed", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			NotifyMetricsChanged();
		}
	}

	private async Task InstallServerAsync()
	{
		if (SelectedServer == null)
		{
			return;
		}
		ApplyDefaultDirectories(SelectedServer);
		SyncPathIniValuesFromSelectedServer();
		if (string.IsNullOrWhiteSpace(SelectedServer.InstallDirectory))
		{
			MessageBox.Show("Choose an install directory before running Install / Update.", "Install directory required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			_activityLog.Warning("Install/update blocked for '" + SelectedServer.Name + "': install directory is missing.");
			return;
		}
		try
		{
			_activityLog.Info("Install/update started for '" + SelectedServer.Name + "'.");
			await ((App.Host?.Services.GetService(typeof(ISteamCmdService)) as ISteamCmdService) ?? throw new InvalidOperationException("SteamCMD service is not available.")).InstallOrUpdateServerAsync(SelectedServer, new Progress<string>(delegate(string message)
			{
				_loggingService.Logger.Information("[SteamCMD] {Message}", message);
				_activityLog.Info("SteamCMD: " + message);
			}));
			_activityLog.Info("Install/update completed for '" + SelectedServer.Name + "'.");
		}
		catch (Exception ex)
		{
			_loggingService.Logger.Error(ex, "Install/update failed for server {ServerName} from dashboard", SelectedServer.Name);
			_activityLog.Error("Install/update failed for '" + SelectedServer.Name + "': " + ex.Message);
			MessageBox.Show(ex.Message, "Install / Update failed", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private static void NormalizeServerSettings(ServerInstance server)
	{
		GameProfile profile = GameProfileCatalog.Get(server.GameId);
		server.GameId = profile.Id;
		server.AppId = profile.SteamAppId;
		if (string.IsNullOrWhiteSpace(server.ExecutableName))
		{
			server.ExecutableName = profile.DefaultExecutableName;
		}
		if (string.IsNullOrWhiteSpace(server.RconPassword))
		{
			server.RconPassword = !string.IsNullOrWhiteSpace(server.AdminPassword) ? server.AdminPassword : "admin";
		}
		if (string.IsNullOrWhiteSpace(server.AdminPassword))
		{
			server.AdminPassword = server.RconPassword;
		}
		server.Config.Port = server.GamePort;
		server.Config.QueryPort = server.QueryPort;
		server.Config.RconPort = server.RconPort;
		server.Config.RconPassword = server.RconPassword;
		server.Config.ServerPassword = server.ServerPassword;
		server.Config.AdminPassword = server.AdminPassword;
		server.Config.MaxPlayers = server.MaxPlayers;
		server.Config.ClusterId = server.ClusterId;
		server.Config.ClusterDirectory = server.ClusterDirectory;
		server.Config.NoTransferFromFiltering = server.NoTransferFromFiltering;
		server.Config.NoTributeDownloads = server.NoTributeDownloads;
		server.Config.PreventDownloadSurvivors = server.PreventDownloadSurvivors;
		server.Config.PreventDownloadItems = server.PreventDownloadItems;
		server.Config.PreventDownloadDinos = server.PreventDownloadDinos;
		server.Config.PreventUploadSurvivors = server.PreventUploadSurvivors;
		server.Config.PreventUploadItems = server.PreventUploadItems;
		server.Config.PreventUploadDinos = server.PreventUploadDinos;
		server.Config.MinimumDinoReuploadInterval = server.MinimumDinoReuploadInterval;
		server.Config.TributeCharacterExpirationSeconds = server.TributeCharacterExpirationSeconds;
		server.Config.TributeDinoExpirationSeconds = server.TributeDinoExpirationSeconds;
		server.Config.TributeItemExpirationSeconds = server.TributeItemExpirationSeconds;
		server.Config.CrossplayEnabled = server.CrossplayEnabled;
		server.Config.UseBattleEye = server.BattleEyeEnabled;
	}

	private async Task SaveSelectedServerModsAsync(string activityMessage)
	{
		if (SelectedServer != null)
		{
			SyncModsToSelectedServer();
			ApplyModLaunchArguments(SelectedServer);
			SetIniValue("ActiveMods", string.Join(",", SelectedServer.Mods.Select((ModEntry x) => x.WorkshopId)));
			NormalizeServerSettings(SelectedServer);
			await _configService.SaveAsync(_appConfig);
			_serverProcessManager.AddServer(SelectedServer);
			CurseForgeStatus = activityMessage;
			_activityLog.Info(activityMessage);
			NotifyMetricsChanged();
			OnPropertyChanged("ModCount");
		}
	}

	private void SyncSelectedServerMods()
	{
		SelectedServerMods.Clear();
		if (SelectedServer != null)
		{
			foreach (ModEntry item in SelectedServer.Mods.OrderBy((ModEntry x) => x.LoadOrder))
			{
				SelectedServerMods.Add(item);
			}
		}
		SelectedServerMod = SelectedServerMods.FirstOrDefault();
		OnPropertyChanged("SelectedServerMods");
	}

	private void SyncModsToSelectedServer()
	{
		if (SelectedServer != null)
		{
			UpdateSelectedServerModOrders();
			SelectedServer.Mods = SelectedServerMods.ToList();
			ApplyModLaunchArguments(SelectedServer);
		}
	}

	private void UpdateSelectedServerModOrders()
	{
		for (int i = 0; i < SelectedServerMods.Count; i++)
		{
			SelectedServerMods[i].LoadOrder = i + 1;
		}
	}

	private static void ApplyModLaunchArguments(ServerInstance server)
	{
		string text = string.Join(",", from x in server.Mods
			orderby x.LoadOrder
			select x.WorkshopId into x
			where !string.IsNullOrWhiteSpace(x)
			select x);
		server.LaunchParameters = RemoveExistingModsArgument(server.LaunchParameters);
		if (!string.IsNullOrWhiteSpace(text))
		{
			server.LaunchParameters = (string.IsNullOrWhiteSpace(server.LaunchParameters) ? ("-mods=" + text) : (server.LaunchParameters + " -mods=" + text));
		}
	}

	private static string RemoveExistingModsArgument(string launchParameters)
	{
		if (string.IsNullOrWhiteSpace(launchParameters))
		{
			return string.Empty;
		}
		IEnumerable<string> values = from part in launchParameters.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			where !part.StartsWith("-mods=", StringComparison.OrdinalIgnoreCase)
			select part;
		return string.Join(' ', values);
	}

	private void LoadIniSettings()
	{
		IniSettings.Clear();
		IniSettings.Add(new IniSettingViewModel("General", "Server Name", "SessionName", "Public session name shown to players in the server browser.", "Launch", "Command line map URL", "string", "New Server", "Text", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("General", "Map", "MapName", "Map package launched by the dedicated server.", "Launch", "Command line", "enum", "TheIsland_WP", "Supported ASA map name", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Cluster", "Cluster ID", "clusterid", "Shared cluster name. Every map in the same Cross-ARK cluster must use the exact same value.", "Launch", "Command line", "string", "", "Unique cluster name", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Cluster", "Cluster Directory", "ClusterDirOverride", "Shared storage folder for Cross-ARK transfer data. Every server in the cluster must use the same path.", "Launch", "Command line", "string", "", "Windows folder path", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Cluster", "No Transfer From Filtering", "NoTransferFromFiltering", "Keeps transfer data restricted to servers that use the configured cluster ID.", "Launch", "Command line flag", "boolean", "True", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Networking", "Game Port", "Port", "UDP game port used by clients to connect.", "GameUserSettings.ini", "[SessionSettings]", "integer", "7777", "1-65535", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Networking", "Query Port", "QueryPort", "Steam/server browser query port.", "GameUserSettings.ini", "[SessionSettings]", "integer", "27015", "1-65535", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Networking", "RCON Port", "RCONPort", "Remote console port used by RCON tools.", "GameUserSettings.ini", "[ServerSettings]", "integer", "32330", "1-65535", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Security", "Admin Password", "ServerAdminPassword", "Password required for server admin commands.", "GameUserSettings.ini", "[ServerSettings]", "string", "", "Text", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Security", "Server Password", "ServerPassword", "Optional join password for private servers.", "GameUserSettings.ini", "[ServerSettings]", "string", "", "Text", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Security", "RCON Password", "RCONPassword", "Password used by RCON clients.", "GameUserSettings.ini", "[ServerSettings]", "string", "", "Text", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Security", "No BattleEye", "NoBattlEye", "Runs the server without BattleEye by adding the -NoBattlEye launch flag.", "Launch", "Command line flag", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Rates", "Difficulty Offset", "DifficultyOffset", "Controls difficulty scaling for wild creature levels and loot quality.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0-1.0 typical", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Rates", "Override Official Difficulty", "OverrideOfficialDifficulty", "Overrides official difficulty scaling; commonly used for max wild dino level tuning.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "5.0", "1.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Rates", "XP Multiplier", "XPMultiplier", "Multiplier for experience gain.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Rates", "Harvest Amount", "HarvestAmountMultiplier", "Multiplier for resources gained from harvesting.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Rates", "Taming Speed", "TamingSpeedMultiplier", "Multiplier for taming speed.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Max Players", "MaxPlayers", "Maximum player slots for the session.", "GameUserSettings.ini", "[/Script/Engine.GameSession]", "integer", "10", "1+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("PvE/PvP", "Server PvE", "ServerPVE", "When true, the server runs PvE rules.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Quality of Life", "Allow Third Person", "AllowThirdPersonPlayer", "Allows players to use third-person camera.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Quality of Life", "Show Map Player Location", "ShowMapPlayerLocation", "Shows player position on the in-game map.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Mods", "Active Mods", "ActiveMods", "Comma-separated mod IDs defining enabled mods and load order.", "GameUserSettings.ini", "[ServerSettings]", "list", "", "Comma-separated IDs", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Player Damage", "PlayerDamageMultiplier", "Multiplier for damage dealt by players.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Player Resistance", "PlayerResistanceMultiplier", "Multiplier for damage resistance applied to players.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Player Harvest Damage", "PlayerHarvestingDamageMultiplier", "Multiplier for harvesting damage dealt by players.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Player Character Water Drain", "PlayerCharacterWaterDrainMultiplier", "Multiplier for player water drain rate.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Player Character Food Drain", "PlayerCharacterFoodDrainMultiplier", "Multiplier for player food drain rate.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Player Character Stamina Drain", "PlayerCharacterStaminaDrainMultiplier", "Multiplier for player stamina drain rate.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Player Health Recovery", "PlayerCharacterHealthRecoveryMultiplier", "Multiplier for player health recovery speed.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Global Spoiling Time", "GlobalSpoilingTimeMultiplier", "Multiplier for item spoil timers.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Corpse Decomposition", "GlobalCorpseDecompositionTimeMultiplier", "Multiplier for corpse decomposition time.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Players", "Item Decomposition", "GlobalItemDecompositionTimeMultiplier", "Multiplier for dropped item decomposition time.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		AddPlayerStatSettings();
		IniSettings.Add(new IniSettingViewModel("Dinos", "Dino Count", "DinoCountMultiplier", "Multiplier for wild creature population density.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Tamed Dino Damage", "TamedDinoDamageMultiplier", "Multiplier for damage dealt by tamed creatures.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Tamed Dino Resistance", "TamedDinoResistanceMultiplier", "Multiplier for damage resistance of tamed creatures.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Wild Dino Damage", "DinoDamageMultiplier", "Multiplier for damage dealt by wild creatures.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Wild Dino Resistance", "DinoResistanceMultiplier", "Multiplier for damage resistance of wild creatures.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Dino Food Drain", "DinoCharacterFoodDrainMultiplier", "Multiplier for creature food drain rate.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Dino Stamina Drain", "DinoCharacterStaminaDrainMultiplier", "Multiplier for creature stamina drain rate.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Dino Health Recovery", "DinoCharacterHealthRecoveryMultiplier", "Multiplier for creature health recovery speed.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Tamed Dino Food Drain", "TamedDinoCharacterFoodDrainMultiplier", "Multiplier for tamed creature food drain rate.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Wild Dino Food Drain", "WildDinoCharacterFoodDrainMultiplier", "Multiplier for wild creature food drain rate.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Max Tamed Dinos", "MaxTamedDinos", "Maximum total tamed creatures allowed on the server.", "GameUserSettings.ini", "[ServerSettings]", "integer", "5000", "0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Max Personal Tamed Dinos", "MaxPersonalTamedDinos", "Maximum personal tamed creatures per player.", "GameUserSettings.ini", "[ServerSettings]", "integer", "0", "0 disables", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "Disable Dino Decay PvE", "DisableDinoDecayPvE", "Disables PvE dino decay behavior.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Dinos", "PvE Dino Decay Period", "PvEDinoDecayPeriodMultiplier", "Multiplier for PvE dino decay timing.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		AddDinoStatSettings("Dino Wild Stat", "PerLevelStatsMultiplier_DinoWild", "wild dino");
		AddDinoStatSettings("Dino Tamed Stat", "PerLevelStatsMultiplier_DinoTamed", "tamed dino");
		AddDinoStatSettings("Dino Tamed Add Stat", "PerLevelStatsMultiplier_DinoTamed_Add", "tamed dino additive");
		AddDinoStatSettings("Dino Tamed Affinity Stat", "PerLevelStatsMultiplier_DinoTamed_Affinity", "tamed dino affinity");
		IniSettings.Add(new IniSettingViewModel("Harvesting", "Harvest Health", "HarvestHealthMultiplier", "Multiplier for resource node health.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Harvesting", "Resource Respawn Period", "ResourcesRespawnPeriodMultiplier", "Multiplier for resource respawn delay.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Harvesting", "Clamp Resource Harvest Damage", "ClampResourceHarvestDamage", "Clamps harvest damage calculations.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Harvesting", "Use Optimized Harvesting Health", "UseOptimizedHarvestingHealth", "Uses optimized resource health calculations for harvesting.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Taming", "Passive Tame Interval", "PassiveTameIntervalMultiplier", "Multiplier for passive tame interval timing.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Taming", "Dino Character Food Drain", "DinoCharacterFoodDrainMultiplier", "Affects hunger-based taming progression for creatures.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Mating Interval", "MatingIntervalMultiplier", "Multiplier for time between breedings.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Egg Hatch Speed", "EggHatchSpeedMultiplier", "Multiplier for egg incubation speed.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Baby Mature Speed", "BabyMatureSpeedMultiplier", "Multiplier for baby creature maturation speed.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Baby Food Consumption", "BabyFoodConsumptionSpeedMultiplier", "Multiplier for baby food consumption speed.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Baby Cuddle Interval", "BabyCuddleIntervalMultiplier", "Multiplier for imprint cuddle interval timing.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Baby Cuddle Grace Period", "BabyCuddleGracePeriodMultiplier", "Multiplier for imprint grace period.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Baby Imprint Stat Scale", "BabyImprintingStatScaleMultiplier", "Multiplier for stat bonus from imprinting.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Breeding", "Lay Egg Interval", "LayEggIntervalMultiplier", "Multiplier for egg laying interval.", "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "Structure Damage", "StructureDamageMultiplier", "Multiplier for damage dealt by structures.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "Structure Resistance", "StructureResistanceMultiplier", "Multiplier for structure damage resistance.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "Max Structures In Range", "TheMaxStructuresInRange", "Maximum nearby structures allowed in range.", "GameUserSettings.ini", "[ServerSettings]", "integer", "10500", "0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "Platform Structure Limit", "MaxPlatformSaddleStructureLimit", "Maximum structures allowed on platform saddles.", "GameUserSettings.ini", "[ServerSettings]", "integer", "0", "0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "Per-Platform Structures", "PerPlatformMaxStructuresMultiplier", "Multiplier for platform structure limit.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "Disable Structure Decay PvE", "DisableStructureDecayPvE", "Disables structure decay on PvE servers.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "PvE Structure Decay Period", "PvEStructureDecayPeriodMultiplier", "Multiplier for PvE structure decay period.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Structures", "Auto Destroy Old Structures", "AutoDestroyOldStructuresMultiplier", "Multiplier for automatic old structure destruction timer.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("PvE/PvP", "Prevent Offline PvP", "PreventOfflinePvP", "Enables offline raid protection behavior.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("PvE/PvP", "Prevent Offline PvP Interval", "PreventOfflinePvPInterval", "Delay before offline protection activates.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "0.0", "Seconds", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("PvE/PvP", "Allow Flyer Carry PvE", "AllowFlyerCarryPvE", "Allows flyers to carry creatures in PvE.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("PvE/PvP", "Enable PvP Gamma", "EnablePvPGamma", "Allows gamma adjustment in PvP.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "No Tribute Downloads", "noTributeDownloads", "Disables tribute downloads for transfers.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Prevent Download Dinos", "PreventDownloadDinos", "Prevents downloading creatures to the server.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Prevent Download Items", "PreventDownloadItems", "Prevents downloading items to the server.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Prevent Download Survivors", "PreventDownloadSurvivors", "Prevents downloading survivors to the server.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Prevent Upload Dinos", "PreventUploadDinos", "Prevents uploading creatures from the server.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Prevent Upload Items", "PreventUploadItems", "Prevents uploading items from the server.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Prevent Upload Survivors", "PreventUploadSurvivors", "Prevents uploading survivors from the server.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Dino Reupload Cooldown", "MinimumDinoReuploadInterval", "Minimum time before uploaded creatures can be uploaded again.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "0.0", "Seconds", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Survivor Expiration Seconds", "TributeCharacterExpirationSeconds", "Expiration timer for uploaded survivors. Zero or less disables survivor expiration.", "GameUserSettings.ini", "[ServerSettings]", "integer", "0", "Seconds", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Dino Expiration Seconds", "TributeDinoExpirationSeconds", "Expiration timer for uploaded creatures.", "GameUserSettings.ini", "[ServerSettings]", "integer", "0", "Seconds", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Transfers", "Item Expiration Seconds", "TributeItemExpirationSeconds", "Expiration timer for uploaded items and cryopods.", "GameUserSettings.ini", "[ServerSettings]", "integer", "0", "Seconds", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Time", "Day Cycle Speed", "DayCycleSpeedScale", "Multiplier for full day/night cycle speed.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Time", "Day Time Speed", "DayTimeSpeedScale", "Multiplier for daytime speed.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Time", "Night Time Speed", "NightTimeSpeedScale", "Multiplier for nighttime speed.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "1.0", "0.0+", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Tribes", "Max Players In Tribe", "MaxNumberOfPlayersInTribe", "Maximum players allowed in a tribe.", "Game.ini", "[/script/shootergame.shootergamemode]", "integer", "0", "0 disables", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Tribes", "Tribe Name Change Cooldown", "TribeNameChangeCooldown", "Cooldown for tribe name changes.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "15.0", "Minutes", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Performance", "Auto Save Period", "AutoSavePeriodMinutes", "World auto-save interval.", "GameUserSettings.ini", "[ServerSettings]", "decimal", "15.0", "Minutes", requiresRestart: false));
		IniSettings.Add(new IniSettingViewModel("Performance", "Server Admin Logs", "AdminLogging", "Logs admin commands.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "False", "True/False", requiresRestart: true));
		IniSettings.Add(new IniSettingViewModel("Performance", "RCON Enabled", "RCONEnabled", "Enables RCON support.", "GameUserSettings.ini", "[ServerSettings]", "boolean", "True", "True/False", requiresRestart: true));
		SyncIniSettingsFromSelectedServer();
	}

	private void AddPlayerStatSettings()
	{
		AddStatSetting("Player Stats", "Player Health Per Level", "PerLevelStatsMultiplier_Player[0]", "Health gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Stamina Per Level", "PerLevelStatsMultiplier_Player[1]", "Stamina gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Torpidity Per Level", "PerLevelStatsMultiplier_Player[2]", "Torpidity scaling per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Oxygen Per Level", "PerLevelStatsMultiplier_Player[3]", "Oxygen gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Food Per Level", "PerLevelStatsMultiplier_Player[4]", "Food gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Water Per Level", "PerLevelStatsMultiplier_Player[5]", "Water gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Temperature Per Level", "PerLevelStatsMultiplier_Player[6]", "Temperature stat scaling per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Weight Per Level", "PerLevelStatsMultiplier_Player[7]", "Weight gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Melee Per Level", "PerLevelStatsMultiplier_Player[8]", "Melee damage gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Speed Per Level", "PerLevelStatsMultiplier_Player[9]", "Movement speed gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Fortitude Per Level", "PerLevelStatsMultiplier_Player[10]", "Fortitude gained per player level.", "1.0");
		AddStatSetting("Player Stats", "Player Crafting Per Level", "PerLevelStatsMultiplier_Player[11]", "Crafting skill gained per player level.", "1.0");
	}

	private void AddDinoStatSettings(string category, string variablePrefix, string descriptionPrefix)
	{
		AddStatSetting(category, descriptionPrefix + " Health", variablePrefix + "[0]", "Health scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Stamina", variablePrefix + "[1]", "Stamina scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Torpidity", variablePrefix + "[2]", "Torpidity scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Oxygen", variablePrefix + "[3]", "Oxygen scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Food", variablePrefix + "[4]", "Food scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Water", variablePrefix + "[5]", "Water scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Temperature", variablePrefix + "[6]", "Temperature stat scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Weight", variablePrefix + "[7]", "Weight scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Melee", variablePrefix + "[8]", "Melee damage scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Speed", variablePrefix + "[9]", "Movement speed scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Fortitude", variablePrefix + "[10]", "Fortitude scaling for " + descriptionPrefix + " creatures.", "1.0");
		AddStatSetting(category, descriptionPrefix + " Crafting", variablePrefix + "[11]", "Crafting stat scaling for " + descriptionPrefix + " creatures.", "1.0");
	}

	private void AddStatSetting(string category, string displayName, string variableName, string description, string defaultValue)
	{
		IniSettings.Add(new IniSettingViewModel(category, displayName, variableName, description, "Game.ini", "[/script/shootergame.shootergamemode]", "decimal", defaultValue, "0.0+", requiresRestart: true));
	}

	private void SyncIniSettingsFromSelectedServer()
	{
		if (SelectedServer != null && IniSettings.Count != 0)
		{
			SetIniValue("SessionName", SelectedServer.Name);
			SetIniValue("MapName", SelectedServer.MapName);
			SetIniValue("clusterid", SelectedServer.ClusterId);
			SetIniValue("ClusterDirOverride", SelectedServer.ClusterDirectory);
			SetIniValue("NoTransferFromFiltering", SelectedServer.NoTransferFromFiltering.ToString());
			SetIniValue("Port", SelectedServer.GamePort.ToString());
			SetIniValue("QueryPort", SelectedServer.QueryPort.ToString());
			SetIniValue("RCONPort", SelectedServer.RconPort.ToString());
			SetIniValue("ServerAdminPassword", SelectedServer.AdminPassword);
			SetIniValue("ServerPassword", SelectedServer.ServerPassword);
			SetIniValue("RCONPassword", SelectedServer.RconPassword);
			SetIniValue("NoBattlEye", (!SelectedServer.BattleEyeEnabled).ToString());
			SetIniValue("DifficultyOffset", SelectedServer.DifficultyOffset.ToString());
			SetIniValue("XPMultiplier", SelectedServer.XPMultiplier.ToString());
			SetIniValue("HarvestAmountMultiplier", SelectedServer.HarvestMultiplier.ToString());
			SetIniValue("TamingSpeedMultiplier", SelectedServer.TamingSpeedMultiplier.ToString());
			SetIniValue("MaxPlayers", SelectedServer.MaxPlayers.ToString());
			SetIniValue("ActiveMods", string.Join(",", SelectedServer.Mods.Select((ModEntry x) => x.WorkshopId)));
			SetIniValue("noTributeDownloads", SelectedServer.NoTributeDownloads.ToString());
			SetIniValue("PreventDownloadSurvivors", SelectedServer.PreventDownloadSurvivors.ToString());
			SetIniValue("PreventDownloadItems", SelectedServer.PreventDownloadItems.ToString());
			SetIniValue("PreventDownloadDinos", SelectedServer.PreventDownloadDinos.ToString());
			SetIniValue("PreventUploadSurvivors", SelectedServer.PreventUploadSurvivors.ToString());
			SetIniValue("PreventUploadItems", SelectedServer.PreventUploadItems.ToString());
			SetIniValue("PreventUploadDinos", SelectedServer.PreventUploadDinos.ToString());
			SetIniValue("MinimumDinoReuploadInterval", SelectedServer.MinimumDinoReuploadInterval.ToString());
			SetIniValue("TributeCharacterExpirationSeconds", SelectedServer.TributeCharacterExpirationSeconds.ToString());
			SetIniValue("TributeDinoExpirationSeconds", SelectedServer.TributeDinoExpirationSeconds.ToString());
			SetIniValue("TributeItemExpirationSeconds", SelectedServer.TributeItemExpirationSeconds.ToString());
		}
	}

	public int ReloadIniSettingsFromSelectedServerFiles(params string[] fileNames)
	{
		if (SelectedServer == null || string.IsNullOrWhiteSpace(SelectedServer.InstallDirectory))
		{
			return 0;
		}
		HashSet<string> requestedFiles = new HashSet<string>(fileNames.Where((string x) => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
		string configDirectory = Path.Combine(SelectedServer.InstallDirectory, "ShooterGame", "Saved", "Config", "WindowsServer");
		int updatedCount = 0;
		foreach (IGrouping<string, IniSettingViewModel> group in IniSettings
			.Where((IniSettingViewModel x) => x.FileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
			.Where((IniSettingViewModel x) => requestedFiles.Count == 0 || requestedFiles.Contains(x.FileName))
			.GroupBy((IniSettingViewModel x) => x.FileName, StringComparer.OrdinalIgnoreCase))
		{
			string iniPath = Path.Combine(configDirectory, group.Key);
			if (!File.Exists(iniPath))
			{
				continue;
			}
			Dictionary<string, Dictionary<string, string>> values = ReadIniFile(iniPath);
			foreach (IniSettingViewModel setting in group)
			{
				if (values.TryGetValue(setting.Section, out Dictionary<string, string>? sectionValues) && sectionValues.TryGetValue(setting.VariableName, out string? value))
				{
					setting.Value = value;
					updatedCount++;
				}
			}
		}
		ApplyIniSettingsToSelectedServer();
		NormalizeServerSettings(SelectedServer);
		RefreshSelectedServerBindings();
		return updatedCount;
	}

	public async Task<int> ReloadImportedIniSettingsAsync(params string[] fileNames)
	{
		int updatedCount = ReloadIniSettingsFromSelectedServerFiles(fileNames);
		if (SelectedServer == null)
		{
			return updatedCount;
		}
		await _configService.SaveAsync(_appConfig);
		_serverProcessManager.AddServer(SelectedServer);
		_activityLog.Info($"Imported INI settings for '{SelectedServer.Name}'. Updated {updatedCount} matching editor value(s).");
		NotifyMetricsChanged();
		return updatedCount;
	}

	public async Task<int> SaveIniSettingsAsync()
	{
		if (SelectedServer == null)
		{
			return 0;
		}
		ApplyIniSettingsToSelectedServer();
		ApplyDefaultDirectories(SelectedServer);
		NormalizeServerSettings(SelectedServer);
		int savedCount = SaveIniSettingsToSelectedServerFiles();
		await _configService.SaveAsync(_appConfig);
		_serverProcessManager.AddServer(SelectedServer);
		RefreshSelectedServerBindings();
		_activityLog.Info($"Saved INI settings for '{SelectedServer.Name}'. Updated {savedCount} file-backed value(s).");
		NotifyMetricsChanged();
		return savedCount;
	}

	private int SaveIniSettingsToSelectedServerFiles()
	{
		if (SelectedServer == null || string.IsNullOrWhiteSpace(SelectedServer.InstallDirectory))
		{
			return 0;
		}
		string configDirectory = Path.Combine(SelectedServer.InstallDirectory, "ShooterGame", "Saved", "Config", "WindowsServer");
		Directory.CreateDirectory(configDirectory);
		int savedCount = 0;
		foreach (IGrouping<string, IniSettingViewModel> group in IniSettings
			.Where((IniSettingViewModel x) => x.FileName.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
			.GroupBy((IniSettingViewModel x) => x.FileName, StringComparer.OrdinalIgnoreCase))
		{
			string iniPath = Path.Combine(configDirectory, group.Key);
			List<string> lines = File.Exists(iniPath) ? File.ReadAllLines(iniPath).ToList() : new List<string>();
			foreach (IniSettingViewModel setting in group)
			{
				UpsertIniValue(lines, setting.Section, setting.VariableName, setting.Value);
				savedCount++;
			}
			File.WriteAllLines(iniPath, lines);
		}
		return savedCount;
	}

	private static Dictionary<string, Dictionary<string, string>> ReadIniFile(string path)
	{
		Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		string currentSection = string.Empty;
		sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string rawLine in File.ReadLines(path))
		{
			string line = rawLine.Trim();
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}
			if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
			{
				currentSection = line;
				if (!sections.ContainsKey(currentSection))
				{
					sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
				continue;
			}
			int equalsIndex = line.IndexOf('=');
			if (equalsIndex <= 0)
			{
				continue;
			}
			string key = line.Substring(0, equalsIndex).Trim();
			string value = line.Substring(equalsIndex + 1).Trim();
			sections[currentSection][key] = value;
		}
		return sections;
	}

	private static void UpsertIniValue(List<string> lines, string section, string key, string value)
	{
		string sectionHeader = NormalizeIniSectionHeader(section);
		int sectionIndex = lines.FindIndex((string line) => string.Equals(line.Trim(), sectionHeader, StringComparison.OrdinalIgnoreCase));
		if (sectionIndex < 0)
		{
			if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
			{
				lines.Add(string.Empty);
			}
			lines.Add(sectionHeader);
			lines.Add(key + "=" + value);
			return;
		}
		int nextSectionIndex = lines.FindIndex(sectionIndex + 1, (string line) => line.TrimStart().StartsWith("[", StringComparison.Ordinal) && line.TrimEnd().EndsWith("]", StringComparison.Ordinal));
		int searchEnd = nextSectionIndex < 0 ? lines.Count : nextSectionIndex;
		for (int i = sectionIndex + 1; i < searchEnd; i++)
		{
			string line = lines[i].TrimStart();
			if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
			{
				lines[i] = key + "=" + value;
				return;
			}
		}
		lines.Insert(searchEnd, key + "=" + value);
	}

	private static string NormalizeIniSectionHeader(string section)
	{
		string trimmed = (section ?? string.Empty).Trim();
		if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
		{
			return trimmed;
		}
		return "[" + trimmed.Trim('[', ']') + "]";
	}

	private void ApplyDefaultDirectories()
	{
		foreach (ServerInstance server in _appConfig.Servers)
		{
			ApplyDefaultDirectories(server);
		}
	}

	private static void ApplyDefaultDirectories(ServerInstance server)
	{
		GameProfile profile = GameProfileCatalog.Get(server.GameId);
		server.GameId = profile.Id;
		server.AppId = profile.SteamAppId;
		if (string.IsNullOrWhiteSpace(server.ExecutableName))
		{
			server.ExecutableName = profile.DefaultExecutableName;
		}
		server.MapName = NormalizeAsaMapName(server.MapName);
		if (string.IsNullOrWhiteSpace(server.InstallDirectory))
		{
			server.InstallDirectory = GetDefaultServerInstallDirectory(server.Name);
		}
		if (string.IsNullOrWhiteSpace(server.ClusterDirectory))
		{
			server.ClusterDirectory = DefaultClusterDirectory;
		}
		Directory.CreateDirectory(server.InstallDirectory);
		Directory.CreateDirectory(server.ClusterDirectory);
	}

	private static string NormalizeAsaMapName(string mapName)
	{
		return (mapName ?? string.Empty).Trim() switch
		{
			"TheIsland" or "The Island" => "TheIsland_WP",
			"ScorchedEarth" or "Scorched Earth" => "ScorchedEarth_WP",
			"TheCenter" or "The Center" => "TheCenter_WP",
			"Aberration" => "Aberration_WP",
			"Extinction" => "Extinction_WP",
			"Ragnarok" => "Ragnarok_WP",
			"Valguero" => "Valguero_WP",
			"Astraeos" => "Astraeos_WP",
			"LostColony" or "Lost Colony" => "LostColony_WP",
			"ClubARK" or "Club ARK" or "BobsMissions" => "BobsMissions_WP",
			string value when value.EndsWith("_WP", StringComparison.OrdinalIgnoreCase) => value,
			string value when !string.IsNullOrWhiteSpace(value) => value,
			_ => "TheIsland_WP"
		};
	}

	private static string GetDefaultServerInstallDirectory(string serverName)
	{
		string text = string.Join("_", (string.IsNullOrWhiteSpace(serverName) ? "New Server" : serverName).Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "New Server";
		}
		return Path.Combine(DefaultServerInstallDirectory, text);
	}

	private string GetAvailableDefaultServerInstallDirectory(string serverName)
	{
		string basePath = GetDefaultServerInstallDirectory(serverName);
		string path = basePath;
		int suffix = 2;
		while (Directory.Exists(path) || _appConfig.Servers.Any((ServerInstance server) => !string.IsNullOrWhiteSpace(server.InstallDirectory) && string.Equals(NormalizeDirectoryPath(server.InstallDirectory), NormalizeDirectoryPath(path), StringComparison.OrdinalIgnoreCase)))
		{
			path = basePath + " " + suffix;
			suffix++;
		}
		return path;
	}

	private static string NormalizeDirectoryPath(string directoryPath)
	{
		return Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
	{
		Directory.CreateDirectory(destinationDirectory);
		foreach (string directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(sourceDirectory, directory);
			Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
		}
		foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(sourceDirectory, file);
			string destinationFile = Path.Combine(destinationDirectory, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destinationDirectory);
			File.Copy(file, destinationFile, overwrite: true);
		}
	}

	private static string ResolveExistingInstallDirectory(string selectedDirectory)
	{
		string fullPath = Path.GetFullPath(selectedDirectory);
		DirectoryInfo? current = new DirectoryInfo(fullPath);
		while (current != null)
		{
			if (string.Equals(current.Name, "ShooterGame", StringComparison.OrdinalIgnoreCase))
			{
				return current.Parent?.FullName ?? fullPath;
			}
			if (Directory.Exists(Path.Combine(current.FullName, "ShooterGame")))
			{
				return current.FullName;
			}
			current = current.Parent;
		}
		string? serverExecutable = FindFirstFile(fullPath, "ArkAscendedServer.exe");
		if (!string.IsNullOrWhiteSpace(serverExecutable))
		{
			DirectoryInfo? executableDirectory = new DirectoryInfo(Path.GetDirectoryName(serverExecutable) ?? fullPath);
			while (executableDirectory != null)
			{
				if (string.Equals(executableDirectory.Name, "ShooterGame", StringComparison.OrdinalIgnoreCase))
				{
					return executableDirectory.Parent?.FullName ?? fullPath;
				}
				executableDirectory = executableDirectory.Parent;
			}
		}
		return fullPath;
	}

	private static string ResolveExistingSavedDirectory(string installDirectory, string selectedDirectory)
	{
		string selectedPath = Path.GetFullPath(selectedDirectory);
		if (string.Equals(new DirectoryInfo(selectedPath).Name, "Saved", StringComparison.OrdinalIgnoreCase))
		{
			return selectedPath;
		}
		string nestedSavedDirectory = Path.Combine(selectedPath, "ShooterGame", "Saved");
		if (Directory.Exists(nestedSavedDirectory))
		{
			return nestedSavedDirectory;
		}
		string installSavedDirectory = Path.Combine(installDirectory, "ShooterGame", "Saved");
		if (Directory.Exists(installSavedDirectory))
		{
			return installSavedDirectory;
		}
		string childSavedDirectory = Path.Combine(selectedPath, "Saved");
		if (Directory.Exists(childSavedDirectory))
		{
			return childSavedDirectory;
		}
		return installSavedDirectory;
	}

	private static Dictionary<string, string> ReadImportedServerSettings(string savedDirectory)
	{
		Dictionary<string, string> settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		string gameUserSettingsPath = Path.Combine(savedDirectory, "Config", "WindowsServer", "GameUserSettings.ini");
		if (!File.Exists(gameUserSettingsPath))
		{
			return settings;
		}
		string section = string.Empty;
		foreach (string rawLine in File.ReadAllLines(gameUserSettingsPath))
		{
			string line = rawLine.Trim();
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}
			if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
			{
				section = line.Substring(1, line.Length - 2);
				continue;
			}
			int equalsIndex = line.IndexOf('=');
			if (equalsIndex <= 0 || !IsImportIniSection(section))
			{
				continue;
			}
			string key = line.Substring(0, equalsIndex).Trim();
			string value = line.Substring(equalsIndex + 1).Trim();
			if (!string.IsNullOrWhiteSpace(key))
			{
				settings[key] = value;
			}
		}
		return settings;
	}

	private static bool IsImportIniSection(string section)
	{
		return section.Equals("ServerSettings", StringComparison.OrdinalIgnoreCase)
			|| section.Equals("/Script/ShooterGame.ShooterGameUserSettings", StringComparison.OrdinalIgnoreCase);
	}

	private static string DetectImportedMapName(string savedDirectory, Dictionary<string, string> settings)
	{
		if (settings.TryGetValue("MapName", out string? configuredMapName) && !string.IsNullOrWhiteSpace(configuredMapName))
		{
			return NormalizeAsaMapName(configuredMapName);
		}
		foreach (string saveFolderName in new[] { "SavedArks", "SavedArksLocal" })
		{
			string saveFolder = Path.Combine(savedDirectory, saveFolderName);
			if (!Directory.Exists(saveFolder))
			{
				continue;
			}
			string? saveFile = Directory.EnumerateFiles(saveFolder, "*.ark", SearchOption.TopDirectoryOnly)
				.Select(Path.GetFileNameWithoutExtension)
				.Where((string? fileName) => !string.IsNullOrWhiteSpace(fileName) && fileName.EndsWith("_WP", StringComparison.OrdinalIgnoreCase))
				.OrderBy((string? fileName) => fileName)
				.FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(saveFile))
			{
				return NormalizeAsaMapName(saveFile);
			}
		}
		return "TheIsland_WP";
	}

	private static void ApplyImportedServerSettings(ServerInstance server, Dictionary<string, string> settings)
	{
		if (settings.TryGetValue("SessionName", out string? sessionName) && !string.IsNullOrWhiteSpace(sessionName))
		{
			server.Name = sessionName;
		}
		if (TryGetSettingInt(settings, "Port", out int port) || TryGetSettingInt(settings, "ServerPort", out port))
		{
			server.GamePort = port;
		}
		if (TryGetSettingInt(settings, "QueryPort", out int queryPort))
		{
			server.QueryPort = queryPort;
		}
		if (TryGetSettingInt(settings, "RCONPort", out int rconPort))
		{
			server.RconPort = rconPort;
		}
		if (TryGetSettingInt(settings, "MaxPlayers", out int maxPlayers))
		{
			server.MaxPlayers = maxPlayers;
		}
		if (TryGetSettingDouble(settings, "DifficultyOffset", out double difficultyOffset))
		{
			server.DifficultyOffset = difficultyOffset;
		}
		if (TryGetSettingDouble(settings, "XPMultiplier", out double xpMultiplier))
		{
			server.XPMultiplier = xpMultiplier;
		}
		if (TryGetSettingDouble(settings, "HarvestAmountMultiplier", out double harvestMultiplier))
		{
			server.HarvestMultiplier = harvestMultiplier;
		}
		if (TryGetSettingDouble(settings, "TamingSpeedMultiplier", out double tamingMultiplier))
		{
			server.TamingSpeedMultiplier = tamingMultiplier;
		}
		if (settings.TryGetValue("ServerAdminPassword", out string? adminPassword))
		{
			server.AdminPassword = adminPassword;
			server.RconPassword = adminPassword;
		}
		if (settings.TryGetValue("RCONPassword", out string? rconPassword) && !string.IsNullOrWhiteSpace(rconPassword))
		{
			server.RconPassword = rconPassword;
		}
		if (settings.TryGetValue("ServerPassword", out string? serverPassword))
		{
			server.ServerPassword = serverPassword;
		}
		if (settings.TryGetValue("ClusterDirOverride", out string? clusterDirectory) && !string.IsNullOrWhiteSpace(clusterDirectory))
		{
			server.ClusterDirectory = clusterDirectory;
		}
		if (settings.TryGetValue("clusterid", out string? clusterId) && !string.IsNullOrWhiteSpace(clusterId))
		{
			server.ClusterId = clusterId;
		}
		server.NoTransferFromFiltering = GetSettingBool(settings, "NoTransferFromFiltering", server.NoTransferFromFiltering);
		server.NoTributeDownloads = GetSettingBool(settings, "noTributeDownloads", server.NoTributeDownloads);
		server.PreventDownloadSurvivors = GetSettingBool(settings, "PreventDownloadSurvivors", server.PreventDownloadSurvivors);
		server.PreventDownloadItems = GetSettingBool(settings, "PreventDownloadItems", server.PreventDownloadItems);
		server.PreventDownloadDinos = GetSettingBool(settings, "PreventDownloadDinos", server.PreventDownloadDinos);
		server.PreventUploadSurvivors = GetSettingBool(settings, "PreventUploadSurvivors", server.PreventUploadSurvivors);
		server.PreventUploadItems = GetSettingBool(settings, "PreventUploadItems", server.PreventUploadItems);
		server.PreventUploadDinos = GetSettingBool(settings, "PreventUploadDinos", server.PreventUploadDinos);
		if (TryGetSettingDouble(settings, "MinimumDinoReuploadInterval", out double dinoReuploadInterval))
		{
			server.MinimumDinoReuploadInterval = dinoReuploadInterval;
		}
		if (TryGetSettingInt(settings, "TributeCharacterExpirationSeconds", out int characterExpiration))
		{
			server.TributeCharacterExpirationSeconds = characterExpiration;
		}
		if (TryGetSettingInt(settings, "TributeDinoExpirationSeconds", out int dinoExpiration))
		{
			server.TributeDinoExpirationSeconds = dinoExpiration;
		}
		if (TryGetSettingInt(settings, "TributeItemExpirationSeconds", out int itemExpiration))
		{
			server.TributeItemExpirationSeconds = itemExpiration;
		}
	}

	private static bool TryGetSettingInt(Dictionary<string, string> settings, string key, out int value)
	{
		value = 0;
		return settings.TryGetValue(key, out string? text) && int.TryParse(text, out value);
	}

	private static bool TryGetSettingDouble(Dictionary<string, string> settings, string key, out double value)
	{
		value = 0.0;
		return settings.TryGetValue(key, out string? text) && double.TryParse(text, out value);
	}

	private static bool GetSettingBool(Dictionary<string, string> settings, string key, bool fallback)
	{
		if (!settings.TryGetValue(key, out string? text))
		{
			return fallback;
		}
		return ParseBool(text, fallback);
	}

	private static string? FindFirstFile(string directory, string searchPattern)
	{
		try
		{
			if (!Directory.Exists(directory))
			{
				return null;
			}
			return Directory.EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories).FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	private void SyncPathIniValuesFromSelectedServer()
	{
		if (SelectedServer != null)
		{
			SetIniValue("ClusterDirOverride", SelectedServer.ClusterDirectory);
			SetIniValue("clusterid", SelectedServer.ClusterId);
		}
	}

	private void ApplyIniSettingsToSelectedServer()
	{
		if (SelectedServer != null)
		{
			SelectedServer.Name = GetIniValue("SessionName", SelectedServer.Name);
			SelectedServer.MapName = GetIniValue("MapName", SelectedServer.MapName);
			SelectedServer.ClusterId = GetNonBlankIniValue("clusterid", SelectedServer.ClusterId);
			SelectedServer.ClusterDirectory = GetNonBlankIniValue("ClusterDirOverride", SelectedServer.ClusterDirectory);
			SelectedServer.NoTransferFromFiltering = ParseBool(GetIniValue("NoTransferFromFiltering", SelectedServer.NoTransferFromFiltering.ToString()), SelectedServer.NoTransferFromFiltering);
			SelectedServer.AdminPassword = GetIniValue("ServerAdminPassword", SelectedServer.AdminPassword);
			SelectedServer.ServerPassword = GetIniValue("ServerPassword", SelectedServer.ServerPassword);
			SelectedServer.RconPassword = GetIniValue("RCONPassword", SelectedServer.RconPassword);
			SelectedServer.BattleEyeEnabled = !ParseBool(GetIniValue("NoBattlEye", (!SelectedServer.BattleEyeEnabled).ToString()), !SelectedServer.BattleEyeEnabled);
			SelectedServer.Config.UseBattleEye = SelectedServer.BattleEyeEnabled;
			if (int.TryParse(GetIniValue("Port", SelectedServer.GamePort.ToString()), out var result))
			{
				SelectedServer.GamePort = result;
			}
			if (int.TryParse(GetIniValue("QueryPort", SelectedServer.QueryPort.ToString()), out var result2))
			{
				SelectedServer.QueryPort = result2;
			}
			if (int.TryParse(GetIniValue("RCONPort", SelectedServer.RconPort.ToString()), out var result3))
			{
				SelectedServer.RconPort = result3;
			}
			if (int.TryParse(GetIniValue("MaxPlayers", SelectedServer.MaxPlayers.ToString()), out var result4))
			{
				SelectedServer.MaxPlayers = result4;
			}
			if (double.TryParse(GetIniValue("DifficultyOffset", SelectedServer.DifficultyOffset.ToString()), out var result5))
			{
				SelectedServer.DifficultyOffset = result5;
			}
			if (double.TryParse(GetIniValue("XPMultiplier", SelectedServer.XPMultiplier.ToString()), out var result6))
			{
				SelectedServer.XPMultiplier = result6;
			}
			if (double.TryParse(GetIniValue("HarvestAmountMultiplier", SelectedServer.HarvestMultiplier.ToString()), out var result7))
			{
				SelectedServer.HarvestMultiplier = result7;
			}
			if (double.TryParse(GetIniValue("TamingSpeedMultiplier", SelectedServer.TamingSpeedMultiplier.ToString()), out var result8))
			{
				SelectedServer.TamingSpeedMultiplier = result8;
			}
			SelectedServer.NoTributeDownloads = ParseBool(GetIniValue("noTributeDownloads", SelectedServer.NoTributeDownloads.ToString()), SelectedServer.NoTributeDownloads);
			SelectedServer.PreventDownloadSurvivors = ParseBool(GetIniValue("PreventDownloadSurvivors", SelectedServer.PreventDownloadSurvivors.ToString()), SelectedServer.PreventDownloadSurvivors);
			SelectedServer.PreventDownloadItems = ParseBool(GetIniValue("PreventDownloadItems", SelectedServer.PreventDownloadItems.ToString()), SelectedServer.PreventDownloadItems);
			SelectedServer.PreventDownloadDinos = ParseBool(GetIniValue("PreventDownloadDinos", SelectedServer.PreventDownloadDinos.ToString()), SelectedServer.PreventDownloadDinos);
			SelectedServer.PreventUploadSurvivors = ParseBool(GetIniValue("PreventUploadSurvivors", SelectedServer.PreventUploadSurvivors.ToString()), SelectedServer.PreventUploadSurvivors);
			SelectedServer.PreventUploadItems = ParseBool(GetIniValue("PreventUploadItems", SelectedServer.PreventUploadItems.ToString()), SelectedServer.PreventUploadItems);
			SelectedServer.PreventUploadDinos = ParseBool(GetIniValue("PreventUploadDinos", SelectedServer.PreventUploadDinos.ToString()), SelectedServer.PreventUploadDinos);
			if (double.TryParse(GetIniValue("MinimumDinoReuploadInterval", SelectedServer.MinimumDinoReuploadInterval.ToString()), out var result9))
			{
				SelectedServer.MinimumDinoReuploadInterval = result9;
			}
			if (int.TryParse(GetIniValue("TributeCharacterExpirationSeconds", SelectedServer.TributeCharacterExpirationSeconds.ToString()), out var result10))
			{
				SelectedServer.TributeCharacterExpirationSeconds = result10;
			}
			if (int.TryParse(GetIniValue("TributeDinoExpirationSeconds", SelectedServer.TributeDinoExpirationSeconds.ToString()), out var result11))
			{
				SelectedServer.TributeDinoExpirationSeconds = result11;
			}
			if (int.TryParse(GetIniValue("TributeItemExpirationSeconds", SelectedServer.TributeItemExpirationSeconds.ToString()), out var result12))
			{
				SelectedServer.TributeItemExpirationSeconds = result12;
			}
		}
	}

	private void RefreshSelectedServerBindings()
	{
		OnPropertyChanged("SelectedServer");
		if (SelectedServer != null)
		{
			_consoleViewModel.SelectedServer = SelectedServer;
		}
	}

	private void SetIniValue(string variableName, string value)
	{
		string variableName2 = variableName;
		IniSettingViewModel iniSettingViewModel = IniSettings.FirstOrDefault((IniSettingViewModel x) => x.VariableName == variableName2);
		if (iniSettingViewModel != null)
		{
			iniSettingViewModel.Value = value;
		}
	}

	private string GetIniValue(string variableName, string fallback)
	{
		string variableName2 = variableName;
		return IniSettings.FirstOrDefault((IniSettingViewModel x) => x.VariableName == variableName2)?.Value ?? fallback;
	}

	private string GetNonBlankIniValue(string variableName, string fallback)
	{
		string iniValue = GetIniValue(variableName, fallback);
		if (string.IsNullOrWhiteSpace(iniValue))
		{
			return fallback;
		}
		return iniValue;
	}

	private static bool ParseBool(string value, bool fallback)
	{
		if (!bool.TryParse(value, out var result))
		{
			return fallback;
		}
		return result;
	}

	private async Task UpdateMetricsLoop()
	{
		while (true)
		{
			await ((DispatcherObject)Application.Current).Dispatcher.InvokeAsync((Action)NotifyMetricsChanged);
			await Task.Delay(2000).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void NotifyMetricsChanged()
	{
		foreach (ServerInstance server in Servers)
		{
			ServerStatus status = _serverProcessManager.GetStatus(server) ?? new ServerStatus
			{
				IsOnline = false
			};
			server.UpdateRuntimeStatus(status);
		}
		OnPropertyChanged("ManagedCount");
		OnPropertyChanged("OnlineCount");
		OnPropertyChanged("OfflineCount");
		OnPropertyChanged("TotalPlayers");
		OnPropertyChanged("TotalSlots");
		OnPropertyChanged("ModCount");
		OnPropertyChanged("FleetState");
	}
}
