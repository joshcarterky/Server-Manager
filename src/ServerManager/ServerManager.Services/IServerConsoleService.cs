using System.Collections.ObjectModel;
using ServerManager.Models;

namespace ServerManager.Services;

public interface IServerConsoleService
{
	ObservableCollection<string> Lines { get; }

	void AddLine(ServerInstance server, string message);

	void AddLine(string message);

	void Clear();
}
