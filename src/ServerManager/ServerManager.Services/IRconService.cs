using System.Threading.Tasks;

namespace ServerManager.Services;

public interface IRconService
{
	bool IsConnected { get; }

	Task ConnectAsync(string host, int port, string password);

	Task DisconnectAsync();

	Task<string> SendCommandAsync(string command);
}
