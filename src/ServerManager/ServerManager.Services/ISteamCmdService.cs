using System;
using System.Threading.Tasks;
using ServerManager.Models;

namespace ServerManager.Services;

public interface ISteamCmdService
{
	Task<string> GetSteamCmdPathAsync();

	Task EnsureSteamCmdAsync();

	Task InstallOrUpdateServerAsync(ServerInstance server, IProgress<string> progress);

	Task ValidateServerAsync(ServerInstance server, IProgress<string> progress);
}
