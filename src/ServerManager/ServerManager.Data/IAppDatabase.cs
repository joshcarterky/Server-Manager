using System.Threading.Tasks;

namespace ServerManager.Data;

public interface IAppDatabase
{
	string DatabasePath { get; }

	Task InitializeAsync();
}
