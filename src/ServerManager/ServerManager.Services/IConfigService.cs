using System.Threading.Tasks;
using ServerManager.Models;

namespace ServerManager.Services;

public interface IConfigService
{
	Task<AppConfig> LoadAsync();

	Task SaveAsync(AppConfig config);
}
