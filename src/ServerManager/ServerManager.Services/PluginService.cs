using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using ServerManager.Helpers;

namespace ServerManager.Services;

public class PluginService : IPluginService
{
	private readonly List<IPlugin> _plugins = new List<IPlugin>();

	private readonly List<string> _loadedPlugins = new List<string>();

	private string _directory = string.Empty;

	public async Task InitializeAsync(string pluginDirectory)
	{
		_directory = FileHelpers.GetOrCreateDirectory(pluginDirectory);
		await ReloadPluginsAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task<IReadOnlyList<string>> GetLoadedPluginsAsync()
	{
		return Task.FromResult((IReadOnlyList<string>)_loadedPlugins.AsReadOnly());
	}

	public Task ReloadPluginsAsync()
	{
		_plugins.Clear();
		_loadedPlugins.Clear();
		if (!Directory.Exists(_directory))
		{
			return Task.CompletedTask;
		}
		string[] files = Directory.GetFiles(_directory, "*.dll", SearchOption.TopDirectoryOnly);
		foreach (string path in files)
		{
			try
			{
				AssemblyLoadContext assemblyLoadContext = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(path), isCollectible: true);
				using FileStream assembly = File.OpenRead(path);
				foreach (Type item in from type in assemblyLoadContext.LoadFromStream(assembly).GetExportedTypes()
					where typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract
					select type)
				{
					if (Activator.CreateInstance(item) is IPlugin plugin)
					{
						plugin.Initialize();
						_plugins.Add(plugin);
						_loadedPlugins.Add(plugin.Name + " " + plugin.Version);
					}
				}
			}
			catch
			{
			}
		}
		return Task.CompletedTask;
	}
}
