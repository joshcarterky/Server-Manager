using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServerManager.Models;

namespace ServerManager.Services;

public interface ICurseForgeService
{
	Task<IReadOnlyList<CurseForgeModResult>> SearchAsaModsAsync(string apiKey, string searchText, CancellationToken cancellationToken = default(CancellationToken));

	IReadOnlyList<CurseForgeModResult> GetTopDownloadedAsaMods();

	Task<IReadOnlyList<CurseForgeModResult>> RefreshTopDownloadedAsaModsAsync(string apiKey, CancellationToken cancellationToken = default(CancellationToken));
}
