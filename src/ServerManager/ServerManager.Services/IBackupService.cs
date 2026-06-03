using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServerManager.Models;

namespace ServerManager.Services;

public interface IBackupService
{
	Task<IReadOnlyList<BackupEntry>> ListBackupsAsync(Guid serverId);

	Task<BackupEntry> CreateBackupAsync(ServerInstance server);

	Task RestoreBackupAsync(ServerInstance server, BackupEntry backup);

	Task CleanupBackupsAsync(ServerInstance server, int retainCount);
}
