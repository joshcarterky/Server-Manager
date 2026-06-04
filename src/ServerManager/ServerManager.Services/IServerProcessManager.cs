using System.Collections.Generic;
using System.Threading.Tasks;
using ServerManager.Models;

namespace ServerManager.Services;

public interface IServerProcessManager
{
	IReadOnlyList<ServerInstance> Servers { get; }

	Task InitializeAsync(AppConfig config);

	void AddServer(ServerInstance server);

	void RemoveServer(ServerInstance server);

	void HydrateManagedIniValues(ServerInstance server);

	Task StartServerAsync(ServerInstance server);

	Task StopServerAsync(ServerInstance server);

	Task RestartServerAsync(ServerInstance server);

	Task SendCommandAsync(ServerInstance server, string command);

	ServerStatus? GetStatus(ServerInstance server);

	IAsyncEnumerable<ServerStatus> MonitorServersAsync();
}
