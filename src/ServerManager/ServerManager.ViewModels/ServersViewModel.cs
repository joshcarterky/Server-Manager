using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ServerManager.ViewModels;

public class ServersViewModel : ObservableObject
{
	private readonly IServerProcessManager _serverProcessManager;

	private readonly IConfigService _configService;

	private readonly ILoggingService _loggingService;

	private readonly ConsoleViewModel _consoleViewModel;

	private readonly IAsaServerDiscoveryService _asaServerDiscoveryService;

	private readonly AppConfig _appConfig;

	private ServerInstance? _selectedServer;

	public ObservableCollection<ServerInstance> Servers { get; } = new ObservableCollection<ServerInstance>();

	public ObservableCollection<GameProfile> GameProfiles { get; } = new ObservableCollection<GameProfile>(GameProfileCatalog.All);


	public ObservableCollection<string> MapNames { get; } = new ObservableCollection<string>
	{
		"TheIsland_WP", "ScorchedEarth_WP", "TheCenter_WP", "Aberration_WP", "Extinction_WP", "Ragnarok_WP",
		"Valguero_WP", "Astraeos_WP", "LostColony_WP", "BobsMissions_WP"
	};


	public ServerInstance? SelectedServer
	{
		get
		{
			return _selectedServer;
		}
		set
		{
			SetProperty(ref _selectedServer, value, "SelectedServer");
			StartServerCommand?.NotifyCanExecuteChanged();
			StopServerCommand?.NotifyCanExecuteChanged();
			RestartServerCommand?.NotifyCanExecuteChanged();
			InstallServerCommand?.NotifyCanExecuteChanged();
			BrowseInstallDirectoryCommand?.NotifyCanExecuteChanged();
			RemoveServerCommand?.NotifyCanExecuteChanged();
			SaveServerCommand?.NotifyCanExecuteChanged();
		}
	}

	public IAsyncRelayCommand StartServerCommand { get; }

	public IAsyncRelayCommand StopServerCommand { get; }

	public IAsyncRelayCommand RestartServerCommand { get; }

	public IAsyncRelayCommand InstallServerCommand { get; }

	public IAsyncRelayCommand BrowseInstallDirectoryCommand { get; }

	public IAsyncRelayCommand AddServerCommand { get; }

	public IAsyncRelayCommand AddExistingServerCommand { get; }

	public IAsyncRelayCommand RemoveServerCommand { get; }

	public IAsyncRelayCommand SaveServerCommand { get; }

	private static string DefaultServerInstallDirectory => Path.Combine(AppContext.BaseDirectory, "servers");

	private static string DefaultClusterDirectory => DashboardViewModel.DefaultClusterDirectory;

	public ServersViewModel(IServerProcessManager serverProcessManager, IConfigService configService, ILoggingService loggingService, ConsoleViewModel consoleViewModel, IAsaServerDiscoveryService asaServerDiscoveryService)
	{
		_serverProcessManager = serverProcessManager;
		_configService = configService;
		_loggingService = loggingService;
		_consoleViewModel = consoleViewModel;
		_asaServerDiscoveryService = asaServerDiscoveryService;
		_appConfig = Task.Run(() => _configService.LoadAsync()).GetAwaiter().GetResult();
		Task.Run(() => _serverProcessManager.InitializeAsync(_appConfig)).GetAwaiter().GetResult();
		foreach (ServerInstance server in _appConfig.Servers)
		{
			Servers.Add(server);
		}
		StartServerCommand = new AsyncRelayCommand(StartServerAsync, CanManageServer);
		StopServerCommand = new AsyncRelayCommand(StopServerAsync, CanManageServer);
		RestartServerCommand = new AsyncRelayCommand(RestartServerAsync, CanManageServer);
		InstallServerCommand = new AsyncRelayCommand(InstallServerAsync, CanManageServer);
		BrowseInstallDirectoryCommand = new AsyncRelayCommand(BrowseInstallDirectoryAsync, CanManageServer);
		AddServerCommand = new AsyncRelayCommand(AddServerAsync);
		AddExistingServerCommand = new AsyncRelayCommand(AddExistingServerAsync);
		RemoveServerCommand = new AsyncRelayCommand(RemoveServerAsync, CanManageServer);
		SaveServerCommand = new AsyncRelayCommand(SaveSelectedServerAsync, CanManageServer);
		SelectedServer = Servers.FirstOrDefault();
	}

	private bool CanManageServer()
	{
		return SelectedServer != null;
	}

	private async Task StartServerAsync()
	{
		if (SelectedServer != null)
		{
			_serverProcessManager.HydrateManagedIniValues(SelectedServer);
			NormalizeServerSettings(SelectedServer);
			await _configService.SaveAsync(_appConfig).ConfigureAwait(continueOnCapturedContext: false);
			_serverProcessManager.AddServer(SelectedServer);
			await _serverProcessManager.StartServerAsync(SelectedServer).ConfigureAwait(continueOnCapturedContext: false);
			_loggingService.Logger.Information("Auto-connecting RCON console for server {ServerName}", SelectedServer.Name);
			_ = _consoleViewModel.ConnectToServerAsync(SelectedServer, showErrorDialog: false, retryCount: 24, retryDelayMs: 5000);
		}
	}

	private async Task StopServerAsync()
	{
		if (SelectedServer != null)
		{
			await _serverProcessManager.StopServerAsync(SelectedServer).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async Task RestartServerAsync()
	{
		if (SelectedServer != null)
		{
			_serverProcessManager.HydrateManagedIniValues(SelectedServer);
			NormalizeServerSettings(SelectedServer);
			await _configService.SaveAsync(_appConfig).ConfigureAwait(continueOnCapturedContext: false);
			await _serverProcessManager.RestartServerAsync(SelectedServer).ConfigureAwait(continueOnCapturedContext: false);
			_loggingService.Logger.Information("Auto-connecting RCON console for restarted server {ServerName}", SelectedServer.Name);
			_ = _consoleViewModel.ConnectToServerAsync(SelectedServer, showErrorDialog: false, retryCount: 24, retryDelayMs: 5000);
		}
	}

	private async Task InstallServerAsync()
	{
		if (SelectedServer == null)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(SelectedServer.InstallDirectory))
		{
			MessageBox.Show("Choose an install directory before running Install / Update.", "Install directory required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		try
		{
			ISteamCmdService obj = (App.Host?.Services.GetService(typeof(ISteamCmdService)) as ISteamCmdService) ?? throw new InvalidOperationException("SteamCMD service is not available.");
			_loggingService.Logger.Information("Starting install/update for server {ServerName} in {InstallDirectory}", SelectedServer.Name, SelectedServer.InstallDirectory);
			await obj.InstallOrUpdateServerAsync(progress: new Progress<string>(delegate(string message)
			{
				_loggingService.Logger.Information("[SteamCMD] {Message}", message);
			}), server: SelectedServer);
			MessageBox.Show("Install / Update completed.", "Server install", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		catch (Exception ex)
		{
			_loggingService.Logger.Error(ex, "Install/update failed for server {ServerName}", SelectedServer.Name);
			MessageBox.Show(ex.Message, "Install / Update failed", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private async Task BrowseInstallDirectoryAsync()
	{
		if (SelectedServer != null)
		{
			OpenFolderDialog openFolderDialog = new OpenFolderDialog
			{
				Title = "Select the server install directory",
				InitialDirectory = GetInitialDirectory(SelectedServer.InstallDirectory)
			};
			if (openFolderDialog.ShowDialog(Application.Current?.MainWindow).GetValueOrDefault())
			{
				SelectedServer.InstallDirectory = openFolderDialog.FolderName;
				OnPropertyChanged("SelectedServer");
				await _configService.SaveAsync(_appConfig);
			}
		}
	}

	private static string GetInitialDirectory(string installDirectory)
	{
		if (!string.IsNullOrWhiteSpace(installDirectory) && Directory.Exists(installDirectory))
		{
			return installDirectory;
		}
		return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
	}

	public async Task UpdateInstallDirectoryAsync(string installDirectory)
	{
		if (SelectedServer != null && !string.IsNullOrWhiteSpace(installDirectory))
		{
			SelectedServer.InstallDirectory = installDirectory;
			OnPropertyChanged("SelectedServer");
			await _configService.SaveAsync(_appConfig);
		}
	}

	private async Task SaveSelectedServerAsync()
	{
		if (SelectedServer != null)
		{
			NormalizeServerSettings(SelectedServer);
			if (!_appConfig.Servers.Any((ServerInstance x) => x.Id == SelectedServer.Id))
			{
				_appConfig.Servers.Add(SelectedServer);
			}
			_serverProcessManager.AddServer(SelectedServer);
			await _configService.SaveAsync(_appConfig);
			OnPropertyChanged("SelectedServer");
			MessageBox.Show("Server settings saved.", "Server settings", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
	}

	private static void NormalizeServerSettings(ServerInstance server)
	{
		GameProfile profile = GameProfileCatalog.Get(server.GameId);
		server.GameId = profile.Id;
		server.AppId = profile.SteamAppId;
		if (string.IsNullOrWhiteSpace(server.ExecutableName))
		{
			server.ExecutableName = profile.DefaultExecutableName;
		}
		server.Config.Port = server.GamePort;
		server.Config.QueryPort = server.QueryPort;
		server.Config.RconPort = server.RconPort;
		server.Config.RconPassword = server.RconPassword;
		server.Config.ServerPassword = server.ServerPassword;
		server.Config.AdminPassword = server.AdminPassword;
		server.Config.MaxPlayers = server.MaxPlayers;
		server.Config.ClusterId = server.ClusterId;
		server.ClusterDirectory = DashboardViewModel.NormalizeClusterDirectory(server);
		server.Config.ClusterDirectory = server.ClusterDirectory;
		server.Config.NoTransferFromFiltering = server.NoTransferFromFiltering;
		server.Config.CrossplayEnabled = server.CrossplayEnabled;
		server.Config.UseBattleEye = server.BattleEyeEnabled;
	}

	private async Task AddServerAsync()
	{
		GameProfile? profile = SelectGameProfile("New Server");
		if (profile == null)
		{
			return;
		}
		await CreateServerAsync("New Server", profile.Id);
	}

	public async Task<ServerInstance> CreateServerAsync(string serverName, string gameId)
	{
		GameProfile profile = GameProfileCatalog.Get(gameId);
		string cleanServerName = string.IsNullOrWhiteSpace(serverName) ? "New Server" : serverName.Trim();
		ServerInstance serverInstance = new ServerInstance
		{
			Name = cleanServerName,
			GameId = profile.Id,
			AppId = profile.SteamAppId,
			ExecutableName = profile.DefaultExecutableName,
			InstallDirectory = GetDefaultServerInstallDirectory(cleanServerName),
			ClusterDirectory = DashboardViewModel.GetDefaultClusterDirectory(string.Empty),
			MapName = (MapNames.FirstOrDefault() ?? "TheIsland_WP")
		};
		NormalizeServerSettings(serverInstance);
		Servers.Add(serverInstance);
		_appConfig.Servers.Add(serverInstance);
		_serverProcessManager.AddServer(serverInstance);
		SelectedServer = serverInstance;
		await _configService.SaveAsync(_appConfig);
		return serverInstance;
	}

	private async Task AddExistingServerAsync()
	{
		GameProfile? profile = SelectGameProfile("Add Existing Server");
		if (profile == null)
		{
			return;
		}
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "Select an existing server install folder",
			InitialDirectory = GetInitialDirectory(string.Empty)
		};
		if (!openFolderDialog.ShowDialog(Application.Current?.MainWindow).GetValueOrDefault())
		{
			return;
		}
		AsaServerDiscoveryResult sourceDiscovery = _asaServerDiscoveryService.Discover(openFolderDialog.FolderName);
		if (!sourceDiscovery.IsValidInstall)
		{
			MessageBox.Show(string.Join(Environment.NewLine, sourceDiscovery.Errors), "Invalid ASA server folder", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		string sourceInstallDirectory = sourceDiscovery.InstallDirectory;
		if (Servers.Any((ServerInstance x) => !string.IsNullOrWhiteSpace(x.InstallDirectory) && string.Equals(NormalizeDirectoryPath(x.InstallDirectory), NormalizeDirectoryPath(sourceInstallDirectory), StringComparison.OrdinalIgnoreCase)))
		{
			MessageBox.Show("That server folder is already in the manager.", "Server already exists", MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		string serverName = GetServerNameFromDirectory(sourceInstallDirectory);
		string installDirectory = GetAvailableDefaultServerInstallDirectory(serverName);
		if (!string.Equals(NormalizeDirectoryPath(sourceInstallDirectory), NormalizeDirectoryPath(installDirectory), StringComparison.OrdinalIgnoreCase))
		{
			await Task.Run(() => CopyDirectory(sourceInstallDirectory, installDirectory)).ConfigureAwait(continueOnCapturedContext: true);
		}
		ServerInstance serverInstance = new ServerInstance
		{
			Name = serverName,
			GameId = profile.Id,
			AppId = profile.SteamAppId,
			ExecutableName = profile.DefaultExecutableName,
			InstallDirectory = installDirectory,
			MapName = (MapNames.FirstOrDefault() ?? "TheIsland_WP")
		};
		AsaServerDiscoveryResult importedDiscovery = _asaServerDiscoveryService.Discover(serverInstance.InstallDirectory);
		_asaServerDiscoveryService.ApplyToServer(serverInstance, importedDiscovery);
		NormalizeServerSettings(serverInstance);
		Servers.Add(serverInstance);
		_appConfig.Servers.Add(serverInstance);
		_serverProcessManager.AddServer(serverInstance);
		SelectedServer = serverInstance;
		await _configService.SaveAsync(_appConfig);
		_loggingService.Logger.Information("Imported existing server {ServerName} from {SourceDirectory} into {InstallDirectory}", serverInstance.Name, sourceInstallDirectory, installDirectory);
	}

	private GameProfile? SelectGameProfile(string title)
	{
		Window dialog = new Window
		{
			Title = title,
			Width = 420.0,
			Height = 270.0,
			MinHeight = 270.0,
			ResizeMode = ResizeMode.NoResize,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Owner = Application.Current?.MainWindow,
			Background = Brush("#111d2a"),
			Foreground = Brushes.White
		};

		Grid root = new Grid
		{
			Margin = new Thickness(22.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		TextBlock heading = new TextBlock
		{
			Text = "Select Game Server",
			Foreground = Brushes.White,
			FontSize = 20.0,
			FontWeight = FontWeights.Bold
		};
		root.Children.Add(heading);

		TextBlock description = new TextBlock
		{
			Text = "Choose the game server type to install or import.",
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 5.0, 0.0, 16.0)
		};
		Grid.SetRow(description, 1);
		root.Children.Add(description);

		ComboBox gameBox = new ComboBox
		{
			ItemsSource = GameProfiles,
			DisplayMemberPath = "DisplayName",
			SelectedValuePath = "Id",
			SelectedItem = GameProfiles.FirstOrDefault(),
			MinHeight = 38.0,
			Background = Brush("#0b1422"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0)
		};
		ApplyDarkComboBox(gameBox);
		Grid.SetRow(gameBox, 2);
		root.Children.Add(gameBox);

		StackPanel buttons = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
		};
		Button cancel = CreateDialogButton("Cancel", "#203249");
		cancel.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
		cancel.Click += delegate { dialog.DialogResult = false; };
		Button continueButton = CreateDialogButton("Continue", "#4658ff");
		continueButton.Click += delegate { dialog.DialogResult = true; };
		buttons.Children.Add(cancel);
		buttons.Children.Add(continueButton);
		Grid.SetRow(buttons, 4);
		root.Children.Add(buttons);
		dialog.Content = root;

		bool selected = dialog.ShowDialog().GetValueOrDefault();
		return selected ? gameBox.SelectedItem as GameProfile : null;
	}

	private static void ApplyDarkComboBox(ComboBox combo)
	{
		combo.Background = Brush("#0b1422");
		combo.Foreground = Brushes.White;
		combo.BorderBrush = Brush("#28445f");
		combo.BorderThickness = new Thickness(1.0);
		combo.Padding = new Thickness(10.0, 0.0, 34.0, 0.0);
		combo.Resources[SystemColors.WindowBrushKey] = Brush("#0b1422");
		combo.Resources[SystemColors.ControlBrushKey] = Brush("#0b1422");
		combo.Resources[SystemColors.ControlTextBrushKey] = Brushes.White;
		combo.Resources[SystemColors.HighlightBrushKey] = Brush("#4658ff");
		combo.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
		combo.ItemContainerStyle = CreateComboBoxItemStyle();
		combo.Template = CreateComboBoxTemplate();
	}

	private static ControlTemplate CreateComboBoxTemplate()
	{
		ControlTemplate template = new ControlTemplate(typeof(ComboBox));
		FrameworkElementFactory grid = new FrameworkElementFactory(typeof(Grid));

		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4.0));
		grid.AppendChild(border);

		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetBinding(ContentPresenter.ContentProperty, new Binding("SelectionBoxItem") { RelativeSource = RelativeSource.TemplatedParent });
		content.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("SelectionBoxItemTemplate") { RelativeSource = RelativeSource.TemplatedParent });
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		grid.AppendChild(content);

		FrameworkElementFactory arrow = new FrameworkElementFactory(typeof(TextBlock));
		arrow.SetValue(TextBlock.TextProperty, "v");
		arrow.SetValue(TextBlock.ForegroundProperty, Brush("#9fb8d6"));
		arrow.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
		arrow.SetValue(TextBlock.FontSizeProperty, 11.0);
		arrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
		arrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		arrow.SetValue(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 12.0, 0.0));
		grid.AppendChild(arrow);

		FrameworkElementFactory toggle = new FrameworkElementFactory(typeof(ToggleButton));
		toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsDropDownOpen") { RelativeSource = RelativeSource.TemplatedParent, Mode = BindingMode.TwoWay });
		toggle.SetValue(Control.BackgroundProperty, Brushes.Transparent);
		toggle.SetValue(Control.BorderThicknessProperty, new Thickness(0.0));
		toggle.SetValue(Control.TemplateProperty, CreateTransparentToggleTemplate());
		grid.AppendChild(toggle);

		FrameworkElementFactory popup = new FrameworkElementFactory(typeof(Popup));
		popup.SetValue(Popup.NameProperty, "PART_Popup");
		popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
		popup.SetValue(Popup.AllowsTransparencyProperty, true);
		popup.SetBinding(Popup.IsOpenProperty, new Binding("IsDropDownOpen") { RelativeSource = RelativeSource.TemplatedParent });

		FrameworkElementFactory popupBorder = new FrameworkElementFactory(typeof(Border));
		popupBorder.SetValue(Border.BackgroundProperty, Brush("#0b1422"));
		popupBorder.SetValue(Border.BorderBrushProperty, Brush("#28445f"));
		popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
		popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4.0));
		popupBorder.SetBinding(FrameworkElement.MinWidthProperty, new Binding("ActualWidth") { RelativeSource = RelativeSource.TemplatedParent });

		FrameworkElementFactory scroll = new FrameworkElementFactory(typeof(ScrollViewer));
		scroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
		scroll.SetValue(ScrollViewer.MaxHeightProperty, 220.0);
		FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ItemsPresenter));
		scroll.AppendChild(presenter);
		popupBorder.AppendChild(scroll);
		popup.AppendChild(popupBorder);
		grid.AppendChild(popup);

		template.VisualTree = grid;
		return template;
	}

	private static ControlTemplate CreateTransparentToggleTemplate()
	{
		ControlTemplate template = new ControlTemplate(typeof(ToggleButton));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
		template.VisualTree = border;
		return template;
	}

	private static Style CreateComboBoxItemStyle()
	{
		Style style = new Style(typeof(ComboBoxItem));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#0b1422")));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10.0, 7.0, 10.0, 7.0)));

		Trigger selected = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#4658ff")));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Triggers.Add(selected);

		Trigger hover = new Trigger { Property = ComboBoxItem.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#18314d")));
		hover.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Triggers.Add(hover);

		return style;
	}

	private static Button CreateDialogButton(string text, string color)
	{
		Button button = new Button
		{
			Content = text,
			MinWidth = 98.0,
			Height = 36.0,
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			FontWeight = FontWeights.Bold
		};
		button.Template = CreateDialogButtonTemplate();
		return button;
	}

	private static ControlTemplate CreateDialogButtonTemplate()
	{
		ControlTemplate template = new ControlTemplate(typeof(Button));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6.0));
		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		border.AppendChild(content);
		template.VisualTree = border;
		return template;
	}

	private static SolidColorBrush Brush(string color)
	{
		SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		brush.Freeze();
		return brush;
	}

	private static string NormalizeDirectoryPath(string directoryPath)
	{
		return Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private static string GetServerNameFromDirectory(string installDirectory)
	{
		string? directoryName = new DirectoryInfo(installDirectory).Name;
		return string.IsNullOrWhiteSpace(directoryName) ? "Existing Server" : directoryName;
	}

	private string GetDefaultServerInstallDirectory(string serverName)
	{
		string text = string.Join("_", (string.IsNullOrWhiteSpace(serverName) ? "New Server" : serverName).Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "New Server";
		}
		string basePath = Path.Combine(DefaultServerInstallDirectory, text);
		string path = basePath;
		int suffix = 2;
		while (Servers.Any((ServerInstance server) => !string.IsNullOrWhiteSpace(server.InstallDirectory) && string.Equals(NormalizeDirectoryPath(server.InstallDirectory), NormalizeDirectoryPath(path), StringComparison.OrdinalIgnoreCase)))
		{
			path = basePath + " " + suffix;
			suffix++;
		}
		return path;
	}

	private string GetAvailableDefaultServerInstallDirectory(string serverName)
	{
		string path = GetDefaultServerInstallDirectory(serverName);
		string basePath = path;
		int suffix = 2;
		while (Directory.Exists(path) || Servers.Any((ServerInstance server) => !string.IsNullOrWhiteSpace(server.InstallDirectory) && string.Equals(NormalizeDirectoryPath(server.InstallDirectory), NormalizeDirectoryPath(path), StringComparison.OrdinalIgnoreCase)))
		{
			path = basePath + " " + suffix;
			suffix++;
		}
		return path;
	}

	private static string ResolveExistingInstallDirectory(string selectedDirectory)
	{
		string fullPath = Path.GetFullPath(selectedDirectory);
		DirectoryInfo? current = new DirectoryInfo(fullPath);
		while (current != null)
		{
			if (string.Equals(current.Name, "ShooterGame", StringComparison.OrdinalIgnoreCase))
			{
				return current.Parent?.FullName ?? fullPath;
			}
			if (Directory.Exists(Path.Combine(current.FullName, "ShooterGame")))
			{
				return current.FullName;
			}
			current = current.Parent;
		}
		string? serverExecutable = FindFirstFile(fullPath, "ArkAscendedServer.exe");
		if (!string.IsNullOrWhiteSpace(serverExecutable))
		{
			DirectoryInfo? executableDirectory = new DirectoryInfo(Path.GetDirectoryName(serverExecutable) ?? fullPath);
			while (executableDirectory != null)
			{
				if (string.Equals(executableDirectory.Name, "ShooterGame", StringComparison.OrdinalIgnoreCase))
				{
					return executableDirectory.Parent?.FullName ?? fullPath;
				}
				executableDirectory = executableDirectory.Parent;
			}
		}
		return fullPath;
	}

	private static string? FindFirstFile(string rootDirectory, string fileName)
	{
		if (!Directory.Exists(rootDirectory))
		{
			return null;
		}
		try
		{
			return Directory.EnumerateFiles(rootDirectory, fileName, SearchOption.AllDirectories).FirstOrDefault();
		}
		catch
		{
			return null;
		}
	}

	private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
	{
		Directory.CreateDirectory(destinationDirectory);
		foreach (string directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(sourceDirectory, directory);
			Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
		}
		foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(sourceDirectory, file);
			string destinationFile = Path.Combine(destinationDirectory, relativePath);
			Directory.CreateDirectory(Path.GetDirectoryName(destinationFile) ?? destinationDirectory);
			File.Copy(file, destinationFile, overwrite: true);
		}
	}

	private static void SetExecutableNameFromInstallDirectory(ServerInstance server)
	{
		string[] candidatePaths =
		{
			Path.Combine(server.InstallDirectory, "ArkAscendedServer.exe"),
			Path.Combine(server.InstallDirectory, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe"),
			Path.Combine(server.InstallDirectory, "ShooterGame", "Binaries", "Win64", "ShooterGameServer.exe")
		};
		string? executablePath = candidatePaths.FirstOrDefault(File.Exists);
		if (string.IsNullOrWhiteSpace(executablePath))
		{
			executablePath = Directory.EnumerateFiles(server.InstallDirectory, "ArkAscendedServer.exe", SearchOption.AllDirectories).FirstOrDefault()
				?? Directory.EnumerateFiles(server.InstallDirectory, "ShooterGameServer.exe", SearchOption.AllDirectories).FirstOrDefault();
		}
		if (!string.IsNullOrWhiteSpace(executablePath))
		{
			server.ExecutableName = Path.GetFileName(executablePath);
		}
	}

	private async Task RemoveServerAsync()
	{
		if (SelectedServer != null && ConfirmRemoveServer(SelectedServer))
		{
			await RemoveServerAsync(SelectedServer);
		}
	}

	public async Task RemoveServerAsync(ServerInstance serverToRemove)
	{
		if (serverToRemove == null)
		{
			return;
		}
		int index = Servers.IndexOf(serverToRemove);
		await _serverProcessManager.StopServerAsync(serverToRemove);
		Servers.Remove(serverToRemove);
		_appConfig.Servers.RemoveAll((ServerInstance x) => x.Id == serverToRemove.Id);
		_serverProcessManager.RemoveServer(serverToRemove);
		if (Servers.Count > 0)
		{
			int index2 = Math.Max(0, Math.Min(index, Servers.Count - 1));
			SelectedServer = Servers[index2];
		}
		else
		{
			SelectedServer = null;
		}
		await _configService.SaveAsync(_appConfig);
	}

	public static bool ConfirmRemoveServer(ServerInstance server)
	{
		MessageBoxResult result = MessageBox.Show(
			"Remove '" + server.Name + "' from the manager?\n\nThis only removes it from the app list. It will not delete the server install folder or saves.",
			"Remove server",
			MessageBoxButton.YesNo,
			MessageBoxImage.Warning);
		return result == MessageBoxResult.Yes;
	}
}
