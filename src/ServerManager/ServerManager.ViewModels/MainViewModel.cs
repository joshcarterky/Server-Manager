using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ServerManager.Models;
using ServerManager.Services;
using ServerManager.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class MainViewModel : ObservableObject
{
	private readonly IConfigService _configService;

	private readonly IServerProcessManager _serverProcessManager;

	private MenuItemViewModel? _selectedMenuItem;

	private object? _currentPage;

	private string _statusText = "Ready";

	public AppConfig AppConfig { get; private set; }

	public ObservableCollection<MenuItemViewModel> MenuItems { get; }

	public IAsyncRelayCommand NavigateSettingsCommand { get; }

	public MenuItemViewModel SelectedMenuItem
	{
		get
		{
			return _selectedMenuItem;
		}
		set
		{
			SetProperty(ref _selectedMenuItem, value, "SelectedMenuItem");
			CurrentPage = value.Page;
		}
	}

	public object CurrentPage
	{
		get
		{
			return _currentPage;
		}
		set
		{
			SetProperty(ref _currentPage, value, "CurrentPage");
		}
	}

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

	public string UiBackgroundColor => AppConfig.UiBackgroundColor;

	public string UiPanelColor => AppConfig.UiPanelColor;

	public string UiInputColor => AppConfig.UiInputColor;

	public string UiAccentColor => AppConfig.UiAccentColor;

	public string UiTextColor => AppConfig.UiTextColor;

	public MainViewModel(IConfigService configService, IServerProcessManager serverProcessManager, DashboardViewModel dashboardViewModel, ServersViewModel serversViewModel, ModsViewModel modsViewModel, ConfigEditorViewModel configEditorViewModel, ValidationViewModel validationViewModel, SchedulerViewModel schedulerViewModel, WatchdogViewModel watchdogViewModel, ConsoleViewModel consoleViewModel, BackupsViewModel backupsViewModel, ClustersViewModel clustersViewModel, PerformanceViewModel performanceViewModel, TemplatesViewModel templatesViewModel, DiscordViewModel discordViewModel, LogsViewModel logsViewModel, SettingsViewModel settingsViewModel)
	{
		_configService = configService;
		_serverProcessManager = serverProcessManager;
		AppConfig = Task.Run(() => _configService.LoadAsync()).GetAwaiter().GetResult();
		Task.Run(() => _serverProcessManager.InitializeAsync(AppConfig)).GetAwaiter().GetResult();
		MenuItems = new ObservableCollection<MenuItemViewModel>
		{
			new MenuItemViewModel("Dashboard", "Live server overview", "D", dashboardViewModel),
			new MenuItemViewModel("Servers", "Add & remove servers", "S", new ServersView(serversViewModel, dashboardViewModel)),
			new MenuItemViewModel("Templates", "Server presets", "R", new TemplatesView(templatesViewModel)),
			new MenuItemViewModel("Discord", "Webhook automation", "H", new DiscordView(discordViewModel)),
			new MenuItemViewModel("Logs", "Search & export", "L", new LogsView(logsViewModel, configService)),
			new MenuItemViewModel("Settings", "Application settings", "G", new SettingsView(settingsViewModel))
		};
		settingsViewModel.PropertyChanged += delegate(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(SettingsViewModel.UiBackgroundColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiPanelColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiInputColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiAccentColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiTextColor))
			{
				OnPropertyChanged(nameof(UiBackgroundColor));
				OnPropertyChanged(nameof(UiPanelColor));
				OnPropertyChanged(nameof(UiInputColor));
				OnPropertyChanged(nameof(UiAccentColor));
				OnPropertyChanged(nameof(UiTextColor));
			}
		};
		SelectedMenuItem = MenuItems.First();
		NavigateSettingsCommand = new AsyncRelayCommand(ShowSettingsAsync);
	}

	private Task ShowSettingsAsync()
	{
		SelectedMenuItem = MenuItems.First((MenuItemViewModel x) => x.Title == "Settings");
		return Task.CompletedTask;
	}
}
