using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using ServerManager.Helpers;
using ServerManager.Models;

namespace ServerManager.Services;

public class BackupService : IBackupService
{
	private readonly IConfigService _configService;

	public BackupService(IConfigService configService)
	{
		_configService = configService;
	}

	public async Task<IReadOnlyList<BackupEntry>> ListBackupsAsync(Guid serverId)
	{
		string orCreateDirectory = FileHelpers.GetOrCreateDirectory((await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false)).BackupDirectory);
		if (!Directory.Exists(orCreateDirectory))
		{
			return Array.Empty<BackupEntry>();
		}
		return (from file in Directory.GetFiles(orCreateDirectory, "*.zip", SearchOption.TopDirectoryOnly)
			where Path.GetFileName(file).StartsWith(serverId.ToString())
			select new BackupEntry
			{
				ServerId = serverId,
				FilePath = file,
				Name = Path.GetFileNameWithoutExtension(file),
				CreatedAt = File.GetCreationTimeUtc(file),
				SizeBytes = new FileInfo(file).Length
			} into entry
			orderby entry.CreatedAt descending
			select entry).ToArray();
	}

	public async Task<BackupEntry> CreateBackupAsync(ServerInstance server)
	{
		string orCreateDirectory = FileHelpers.GetOrCreateDirectory((await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false)).BackupDirectory);
		string value = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
		string path = $"{server.Id}_{server.Name}_{value}.zip";
		string text = Path.Combine(orCreateDirectory, path);
		ZipFile.CreateFromDirectory(server.InstallDirectory, text, CompressionLevel.Optimal, includeBaseDirectory: true);
		return new BackupEntry
		{
			ServerId = server.Id,
			Name = Path.GetFileNameWithoutExtension(text),
			FilePath = text,
			CreatedAt = DateTime.UtcNow,
			SizeBytes = new FileInfo(text).Length
		};
	}

	public async Task RestoreBackupAsync(ServerInstance server, BackupEntry backup)
	{
		if (!File.Exists(backup.FilePath))
		{
			throw new FileNotFoundException("Backup archive not found.", backup.FilePath);
		}
		if (Directory.Exists(server.InstallDirectory))
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(server.InstallDirectory);
			FileInfo[] files = directoryInfo.GetFiles();
			for (int i = 0; i < files.Length; i++)
			{
				files[i].Delete();
			}
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			for (int i = 0; i < directories.Length; i++)
			{
				directories[i].Delete(recursive: true);
			}
		}
		ZipFile.ExtractToDirectory(backup.FilePath, server.InstallDirectory);
		await Task.CompletedTask;
	}

	public async Task CleanupBackupsAsync(ServerInstance server, int retainCount)
	{
		foreach (BackupEntry item in (await ListBackupsAsync(server.Id).ConfigureAwait(continueOnCapturedContext: false)).Skip(retainCount))
		{
			if (File.Exists(item.FilePath))
			{
				File.Delete(item.FilePath);
			}
		}
	}
}
