using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class PlaceholderModuleViewModel : ObservableObject
{
	private string _statusText = "Select a capability to configure.";

	public string Title { get; }

	public string Description { get; }

	public ObservableCollection<string> Capabilities { get; }

	public IRelayCommand<string> ConfigureCapabilityCommand { get; }

	public string StatusText
	{
		get
		{
			return _statusText;
		}
		set
		{
			SetProperty(ref _statusText, value, "StatusText");
		}
	}

	public PlaceholderModuleViewModel(string title, string description, params string[] capabilities)
	{
		Title = title;
		Description = description;
		Capabilities = new ObservableCollection<string>(capabilities);
		ConfigureCapabilityCommand = new RelayCommand<string>(ConfigureCapability);
	}

	private void ConfigureCapability(string? capability)
	{
		if (!string.IsNullOrWhiteSpace(capability))
		{
			StatusText = capability + " selected. Detailed editor implementation is next for this module.";
		}
	}
}
