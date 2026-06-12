using ServerManager.Models;

namespace ServerManager.Services;

public interface IAsaServerDiscoveryService
{
	AsaServerDiscoveryResult Discover(string selectedDirectory);

	void ApplyToServer(ServerInstance server, AsaServerDiscoveryResult discovery);
}
