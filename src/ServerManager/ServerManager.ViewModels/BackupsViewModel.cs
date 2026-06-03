using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class BackupsViewModel : ObservableObject
{
	private readonly IBackupService _backupService;

	private readonly IConfigService _configService;

	public ObservableCollection<BackupEntry> Backups { get; } = new ObservableCollection<BackupEntry>();


	public ServerInstance? SelectedServer { get; set; }

	public BackupEntry? SelectedBackup { get; set; }

	public IAsyncRelayCommand CreateBackupCommand { get; }

	public IAsyncRelayCommand RestoreBackupCommand { get; }

	public BackupsViewModel(IBackupService backupService, IConfigService configService)
	{
		_backupService = backupService;
		_configService = configService;
		CreateBackupCommand = new AsyncRelayCommand(CreateBackupAsync, () => SelectedServer != null);
		RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync, () => SelectedBackup != null && SelectedServer != null);
	}

	public async Task LoadBackupsAsync()
	{
		if (SelectedServer == null)
		{
			return;
		}
		Backups.Clear();
		foreach (BackupEntry item in await _backupService.ListBackupsAsync(SelectedServer.Id).ConfigureAwait(continueOnCapturedContext: false))
		{
			Backups.Add(item);
		}
	}

	private async Task CreateBackupAsync()
	{
		if (SelectedServer != null)
		{
			BackupEntry item = await _backupService.CreateBackupAsync(SelectedServer).ConfigureAwait(continueOnCapturedContext: false);
			Backups.Insert(0, item);
		}
	}

	private async Task RestoreBackupAsync()
	{
		if (SelectedServer != null && SelectedBackup != null)
		{
			await _backupService.RestoreBackupAsync(SelectedServer, SelectedBackup).ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
