using System;
using System.IO;
using System.Threading.Tasks;
using ServerManager.Helpers;
using ServerManager.Models;
using Newtonsoft.Json;

namespace ServerManager.Services;

public class ConfigService : IConfigService
{
	private readonly string _configPath;

	private AppConfig? _currentConfig;

	public ConfigService()
	{
		_configPath = Path.Combine(AppContext.BaseDirectory, "asa-config.json");
	}

	public async Task<AppConfig> LoadAsync()
	{
		if (_currentConfig != null)
		{
			return _currentConfig;
		}
		if (!File.Exists(_configPath))
		{
			AppConfig defaultConfig = new AppConfig
			{
				SteamCmdDirectory = Path.Combine(AppContext.BaseDirectory, "steamcmd"),
				BackupDirectory = Path.Combine(AppContext.BaseDirectory, "backups"),
				PluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins")
			};
			FileHelpers.EnsureDirectory(defaultConfig.SteamCmdDirectory);
			FileHelpers.EnsureDirectory(defaultConfig.BackupDirectory);
			FileHelpers.EnsureDirectory(defaultConfig.PluginDirectory);
			await SaveAsync(defaultConfig);
			return defaultConfig;
		}
		_currentConfig = JsonConvert.DeserializeObject<AppConfig>(await File.ReadAllTextAsync(_configPath).ConfigureAwait(continueOnCapturedContext: false)) ?? new AppConfig();
		return _currentConfig;
	}

	public async Task SaveAsync(AppConfig config)
	{
		if (config == null)
		{
			throw new ArgumentNullException("config");
		}
		_currentConfig = config;
		string contents = JsonConvert.SerializeObject(config, Formatting.Indented);
		await File.WriteAllTextAsync(_configPath, contents).ConfigureAwait(continueOnCapturedContext: false);
	}
}
