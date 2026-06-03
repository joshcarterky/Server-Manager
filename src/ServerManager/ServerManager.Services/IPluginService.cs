using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServerManager.Services;

public interface IPluginService
{
	Task InitializeAsync(string pluginDirectory);

	Task<IReadOnlyList<string>> GetLoadedPluginsAsync();

	Task ReloadPluginsAsync();
}
