using ServerManager.Models;

namespace ServerManager.Services;

public interface ILaunchProfileBuilder
{
	LaunchProfile Build(ServerInstance server, string executablePath);

	string RemoveManagedLaunchArguments(string launchParameters);
}
