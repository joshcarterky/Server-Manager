using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class ClustersViewModel : ObservableObject
{
	private readonly IConfigService _configService;

	public ObservableCollection<ClusterDefinition> Clusters { get; } = new ObservableCollection<ClusterDefinition>();


	public ClusterDefinition? SelectedCluster { get; set; }

	public IAsyncRelayCommand AddClusterCommand { get; }

	public IAsyncRelayCommand RemoveClusterCommand { get; }

	public ClustersViewModel(IConfigService configService)
	{
		_configService = configService;
		AddClusterCommand = new AsyncRelayCommand(AddClusterAsync);
		RemoveClusterCommand = new AsyncRelayCommand(RemoveClusterAsync, () => SelectedCluster != null);
		Task.Run(() => LoadClustersAsync()).GetAwaiter().GetResult();
	}

	private async Task LoadClustersAsync()
	{
		foreach (ClusterDefinition cluster in (await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false)).Clusters)
		{
			Clusters.Add(cluster);
		}
	}

	private async Task AddClusterAsync()
	{
		ClusterDefinition cluster = new ClusterDefinition
		{
			ClusterId = $"cluster-{Clusters.Count + 1}",
			DisplayName = "New Cluster"
		};
		Clusters.Add(cluster);
		AppConfig appConfig = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		appConfig.Clusters.Add(cluster);
		await _configService.SaveAsync(appConfig).ConfigureAwait(continueOnCapturedContext: false);
	}

	private async Task RemoveClusterAsync()
	{
		if (SelectedCluster != null)
		{
			AppConfig appConfig = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
			appConfig.Clusters.RemoveAll((ClusterDefinition x) => x.ClusterId == SelectedCluster.ClusterId);
			await _configService.SaveAsync(appConfig).ConfigureAwait(continueOnCapturedContext: false);
			Clusters.Remove(SelectedCluster);
		}
	}
}
