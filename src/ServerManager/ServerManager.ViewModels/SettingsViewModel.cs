using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ServerManager.ViewModels;

public class SettingsViewModel : ObservableObject
{
	private readonly IConfigService _configService;

	private readonly IPluginService _pluginService;

	private readonly IActivityLogService _activityLogService;

	private readonly IHttpClientFactory _httpClientFactory;

	private string _statusText = "Application settings loaded.";

	private string _latestUpdateDownloadUrl = string.Empty;

	private string _latestUpdateVersion = string.Empty;

	private bool _updateAvailable;

	public AppConfig Config { get; private set; }

	public IAsyncRelayCommand SaveCommand { get; }

	public IAsyncRelayCommand ReloadPluginsCommand { get; }

	public IRelayCommand BrowseSteamCmdDirectoryCommand { get; }

	public IRelayCommand BrowseBackupDirectoryCommand { get; }

	public IRelayCommand BrowsePluginDirectoryCommand { get; }

	public IRelayCommand OpenAppDirectoryCommand { get; }

	public IRelayCommand OpenSteamCmdDirectoryCommand { get; }

	public IRelayCommand OpenBackupDirectoryCommand { get; }

	public IRelayCommand OpenPluginDirectoryCommand { get; }

	public IRelayCommand ResetDefaultsCommand { get; }

	public IAsyncRelayCommand CheckForUpdatesCommand { get; }

	public IRelayCommand OpenLatestDownloadCommand { get; }

	public IAsyncRelayCommand InstallUpdateCommand { get; }

	public string Description => "Application paths, colors, plugin settings, and shared manager options.";

	public string AppDirectory => AppContext.BaseDirectory;

	public string ConfigFilePath => Path.Combine(AppContext.BaseDirectory, "asa-config.json");

	public string CurrentVersion => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

	public string SteamCmdDirectory
	{
		get => Config.SteamCmdDirectory;
		set
		{
			if (Config.SteamCmdDirectory != value)
			{
				Config.SteamCmdDirectory = value;
				OnPropertyChanged(nameof(SteamCmdDirectory));
			}
		}
	}

	public string BackupDirectory
	{
		get => Config.BackupDirectory;
		set
		{
			if (Config.BackupDirectory != value)
			{
				Config.BackupDirectory = value;
				OnPropertyChanged(nameof(BackupDirectory));
			}
		}
	}

	public string PluginDirectory
	{
		get => Config.PluginDirectory;
		set
		{
			if (Config.PluginDirectory != value)
			{
				Config.PluginDirectory = value;
				OnPropertyChanged(nameof(PluginDirectory));
			}
		}
	}

	public string CurseForgeApiKey
	{
		get => Config.CurseForgeApiKey;
		set
		{
			if (Config.CurseForgeApiKey != value)
			{
				Config.CurseForgeApiKey = value;
				OnPropertyChanged(nameof(CurseForgeApiKey));
			}
		}
	}

	public bool MinimizeToTray
	{
		get => Config.MinimizeToTray;
		set
		{
			if (Config.MinimizeToTray != value)
			{
				Config.MinimizeToTray = value;
				OnPropertyChanged(nameof(MinimizeToTray));
			}
		}
	}

	public string UiBackgroundColor
	{
		get => Config.UiBackgroundColor;
		set
		{
			if (Config.UiBackgroundColor != value)
			{
				Config.UiBackgroundColor = value;
				OnPropertyChanged(nameof(UiBackgroundColor));
			}
		}
	}

	public string UiPanelColor
	{
		get => Config.UiPanelColor;
		set
		{
			if (Config.UiPanelColor != value)
			{
				Config.UiPanelColor = value;
				OnPropertyChanged(nameof(UiPanelColor));
			}
		}
	}

	public string UiInputColor
	{
		get => Config.UiInputColor;
		set
		{
			if (Config.UiInputColor != value)
			{
				Config.UiInputColor = value;
				OnPropertyChanged(nameof(UiInputColor));
			}
		}
	}

	public string UiAccentColor
	{
		get => Config.UiAccentColor;
		set
		{
			if (Config.UiAccentColor != value)
			{
				Config.UiAccentColor = value;
				OnPropertyChanged(nameof(UiAccentColor));
			}
		}
	}

	public string UiTextColor
	{
		get => Config.UiTextColor;
		set
		{
			if (Config.UiTextColor != value)
			{
				Config.UiTextColor = value;
				OnPropertyChanged(nameof(UiTextColor));
			}
		}
	}

	public string UpdateManifestUrl
	{
		get => Config.UpdateManifestUrl;
		set
		{
			if (Config.UpdateManifestUrl != value)
			{
				Config.UpdateManifestUrl = value;
				OnPropertyChanged(nameof(UpdateManifestUrl));
			}
		}
	}

	public string LatestUpdateVersion
	{
		get => string.IsNullOrWhiteSpace(_latestUpdateVersion) ? "Not checked" : _latestUpdateVersion;
		set => SetProperty(ref _latestUpdateVersion, value, nameof(LatestUpdateVersion));
	}

	public string StatusText
	{
		get => _statusText;
		set => SetProperty(ref _statusText, value, nameof(StatusText));
	}

	public SettingsViewModel(IConfigService configService, IPluginService pluginService, IActivityLogService activityLogService, IHttpClientFactory httpClientFactory)
	{
		_configService = configService;
		_pluginService = pluginService;
		_activityLogService = activityLogService;
		_httpClientFactory = httpClientFactory;
		Config = Task.Run(() => _configService.LoadAsync()).GetAwaiter().GetResult();
		SaveCommand = new AsyncRelayCommand(SaveAsync);
		ReloadPluginsCommand = new AsyncRelayCommand(ReloadPluginsAsync);
		BrowseSteamCmdDirectoryCommand = new RelayCommand(() => BrowseDirectory("SteamCMD Directory", SteamCmdDirectory, path => SteamCmdDirectory = path));
		BrowseBackupDirectoryCommand = new RelayCommand(() => BrowseDirectory("Backup Directory", BackupDirectory, path => BackupDirectory = path));
		BrowsePluginDirectoryCommand = new RelayCommand(() => BrowseDirectory("Plugin Directory", PluginDirectory, path => PluginDirectory = path));
		OpenAppDirectoryCommand = new RelayCommand(() => OpenDirectory(AppDirectory));
		OpenSteamCmdDirectoryCommand = new RelayCommand(() => OpenDirectory(SteamCmdDirectory));
		OpenBackupDirectoryCommand = new RelayCommand(() => OpenDirectory(BackupDirectory));
		OpenPluginDirectoryCommand = new RelayCommand(() => OpenDirectory(PluginDirectory));
		ResetDefaultsCommand = new RelayCommand(ResetDefaults);
		CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
		OpenLatestDownloadCommand = new RelayCommand(OpenLatestDownload, () => !string.IsNullOrWhiteSpace(_latestUpdateDownloadUrl));
		InstallUpdateCommand = new AsyncRelayCommand(InstallUpdateAsync, () => _updateAvailable && !string.IsNullOrWhiteSpace(_latestUpdateDownloadUrl));
	}

	private async Task SaveAsync()
	{
		ApplyPathDefaults();
		EnsureDirectory(SteamCmdDirectory);
		EnsureDirectory(BackupDirectory);
		EnsureDirectory(PluginDirectory);
		await _configService.SaveAsync(Config);
		_activityLogService.Info("Application settings saved.");
		StatusText = "Settings saved.";
	}

	private async Task ReloadPluginsAsync()
	{
		ApplyPathDefaults();
		await _pluginService.InitializeAsync(Config.PluginDirectory);
		_activityLogService.Info("Plugin directory reloaded.");
		StatusText = "Plugin directory reloaded.";
	}

	private void ResetDefaults()
	{
		SteamCmdDirectory = Path.Combine(AppContext.BaseDirectory, "steamcmd");
		BackupDirectory = Path.Combine(AppContext.BaseDirectory, "backups");
		PluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
		UiBackgroundColor = "#07101c";
		UiPanelColor = "#111d2a";
		UiInputColor = "#0b1422";
		UiAccentColor = "#4658ff";
		UiTextColor = "#ffffff";
		StatusText = "Default settings restored. Save to apply.";
	}

	private async Task CheckForUpdatesAsync()
	{
		if (string.IsNullOrWhiteSpace(UpdateManifestUrl))
		{
			StatusText = "Add an update manifest URL, then check again.";
			return;
		}

		try
		{
			HttpClient client = _httpClientFactory.CreateClient();
			string json = await client.GetStringAsync(UpdateManifestUrl);
			UpdateManifest? manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateManifest>(json);
			if (manifest == null || string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(manifest.DownloadUrl))
			{
				StatusText = "Update manifest is missing version or downloadUrl.";
				return;
			}

			_latestUpdateDownloadUrl = manifest.DownloadUrl;
			LatestUpdateVersion = manifest.Version;
			OpenLatestDownloadCommand.NotifyCanExecuteChanged();

			Version current = NormalizeVersion(CurrentVersion);
			Version latest = NormalizeVersion(manifest.Version);
			_updateAvailable = latest > current;
			InstallUpdateCommand.NotifyCanExecuteChanged();
			if (latest > current)
			{
				StatusText = "Update " + manifest.Version + " is available. Use Download & Install to update now.";
				_activityLogService.Info("Application update available: " + manifest.Version);
				return;
			}

			StatusText = "You are on the latest version.";
			_activityLogService.Info("Application update check completed. Current version is latest.");
		}
		catch (Exception ex)
		{
			_updateAvailable = false;
			InstallUpdateCommand.NotifyCanExecuteChanged();
			StatusText = "Update check failed: " + ex.Message;
			_activityLogService.Error("Update check failed: " + ex.Message);
		}
	}

	private async Task InstallUpdateAsync()
	{
		if (string.IsNullOrWhiteSpace(_latestUpdateDownloadUrl))
		{
			StatusText = "Check for updates first.";
			return;
		}
		if (!_updateAvailable)
		{
			StatusText = "No newer update is ready to install.";
			return;
		}

		try
		{
			StatusText = "Downloading update " + LatestUpdateVersion + "...";
			string updateRoot = Path.Combine(Path.GetTempPath(), "DedicatedServerManagerUpdate");
			string packagePath = Path.Combine(updateRoot, "update.zip");
			string extractPath = Path.Combine(updateRoot, "package");
			ResetDirectory(updateRoot);
			Directory.CreateDirectory(extractPath);

			HttpClient client = _httpClientFactory.CreateClient();
			using (HttpResponseMessage response = await client.GetAsync(_latestUpdateDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
			{
				response.EnsureSuccessStatusCode();
				await using FileStream fileStream = File.Create(packagePath);
				await response.Content.CopyToAsync(fileStream);
			}

			StatusText = "Preparing update...";
			ZipFile.ExtractToDirectory(packagePath, extractPath, overwriteFiles: true);
			string executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppContext.BaseDirectory, "ServerManager.exe");
			string targetDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string updaterScript = WriteUpdaterScript(updateRoot, extractPath, targetDirectory, executablePath);

			_activityLogService.Info("Installing application update " + LatestUpdateVersion + ".");
			Process.Start(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(updaterScript) + " -ProcessId " + Environment.ProcessId,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			});
			Application.Current?.Shutdown();
		}
		catch (Exception ex)
		{
			StatusText = "Update install failed: " + ex.Message;
			_activityLogService.Error("Update install failed: " + ex.Message);
		}
	}

	private void OpenLatestDownload()
	{
		if (string.IsNullOrWhiteSpace(_latestUpdateDownloadUrl))
		{
			StatusText = "Check for updates first.";
			return;
		}

		Process.Start(new ProcessStartInfo
		{
			FileName = _latestUpdateDownloadUrl,
			UseShellExecute = true
		});
	}

	private static Version NormalizeVersion(string versionText)
	{
		string clean = versionText.Split('-', '+')[0].Trim();
		if (Version.TryParse(clean, out Version? version))
		{
			return version;
		}

		return new Version(0, 0, 0);
	}

	private void ApplyPathDefaults()
	{
		if (string.IsNullOrWhiteSpace(SteamCmdDirectory))
		{
			SteamCmdDirectory = Path.Combine(AppContext.BaseDirectory, "steamcmd");
		}
		if (string.IsNullOrWhiteSpace(BackupDirectory))
		{
			BackupDirectory = Path.Combine(AppContext.BaseDirectory, "backups");
		}
		if (string.IsNullOrWhiteSpace(PluginDirectory))
		{
			PluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");
		}
	}

	private void BrowseDirectory(string title, string currentDirectory, Action<string> updatePath)
	{
		OpenFolderDialog dialog = new OpenFolderDialog
		{
			Title = title,
			InitialDirectory = GetInitialDirectory(currentDirectory)
		};

		bool? result = dialog.ShowDialog(Application.Current?.MainWindow);
		if (result == true)
		{
			updatePath(dialog.FolderName);
			StatusText = $"{title} selected.";
		}
	}

	private static string GetInitialDirectory(string directory)
	{
		if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
		{
			return directory;
		}
		return AppContext.BaseDirectory;
	}

	private static void EnsureDirectory(string directory)
	{
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}
	}

	private void OpenDirectory(string directory)
	{
		if (string.IsNullOrWhiteSpace(directory))
		{
			StatusText = "No directory selected.";
			return;
		}

		Directory.CreateDirectory(directory);
		Process.Start(new ProcessStartInfo
		{
			FileName = "explorer.exe",
			Arguments = $"\"{directory}\"",
			UseShellExecute = true
		});
	}

	private static void ResetDirectory(string directory)
	{
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, recursive: true);
		}
		Directory.CreateDirectory(directory);
	}

	private static string WriteUpdaterScript(string updateRoot, string sourceDirectory, string targetDirectory, string executablePath)
	{
		string scriptPath = Path.Combine(updateRoot, "install-update.ps1");
		string script = """
param(
    [int]$ProcessId
)
$ErrorActionPreference = 'Stop'
$source = '__SOURCE__'
$target = '__TARGET__'
$exe = '__EXE__'
Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Copy-Item -Path (Join-Path $source '*') -Destination $target -Recurse -Force
Start-Process -FilePath $exe
""";
		script = script
			.Replace("__SOURCE__", EscapePowerShellLiteral(sourceDirectory))
			.Replace("__TARGET__", EscapePowerShellLiteral(targetDirectory))
			.Replace("__EXE__", EscapePowerShellLiteral(executablePath));
		File.WriteAllText(scriptPath, script);
		return scriptPath;
	}

	private static string EscapePowerShellLiteral(string value)
	{
		return value.Replace("'", "''");
	}

	private static string Quote(string value)
	{
		return "\"" + value.Replace("\"", "\\\"") + "\"";
	}

	private sealed class UpdateManifest
	{
		public string Version { get; set; } = string.Empty;

		public string DownloadUrl { get; set; } = string.Empty;
	}
}
