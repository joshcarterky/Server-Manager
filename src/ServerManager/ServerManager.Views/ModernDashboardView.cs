using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ServerManager.Models;
using ServerManager.ViewModels;
using Microsoft.Win32;

namespace ServerManager.Views;

public class ModernDashboardView : UserControl
{
	private const string CustomMapOption = "Custom Map";

	private static readonly HttpClient PublicIpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(4.0) };

	private static string? _cachedPublicIpAddress;

	private static DateTime _cachedPublicIpAddressAt;

	private readonly DashboardViewModel _viewModel;

	private readonly StackPanel _serverCards;

	private readonly Border _configurePanel;

	private readonly TextBlock _configureSubtitle;

	private readonly TabControl _configureTabs;

	public ModernDashboardView(DashboardViewModel viewModel)
	{
		_viewModel = viewModel;
		DataContext = viewModel;
		_serverCards = new StackPanel { Orientation = Orientation.Horizontal };
		_configureSubtitle = new TextBlock
		{
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0
		};
		_configureTabs = new TabControl();
		_configurePanel = BuildInlineConfigurePanel();
		Grid root = new Grid
		{
			Background = CreateBackgroundBrush(),
			ClipToBounds = true
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		root.Children.Add(BuildHeader());

		ScrollViewer scrollViewer = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Margin = new Thickness(28.0, 12.0, 28.0, 0.0)
		};
		StackPanel content = new StackPanel();
		content.Children.Add(BuildStatsRow());
		content.Children.Add(BuildServersPanel());
		content.Children.Add(BuildActivityPanel());
		scrollViewer.Content = content;
		Grid.SetRow(scrollViewer, 1);
		root.Children.Add(scrollViewer);

		Border footer = new Border
		{
			BorderBrush = BrushFrom("#1f3854"),
			BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0),
			Padding = new Thickness(0.0, 14.0, 0.0, 14.0)
		};
		footer.Child = new TextBlock
		{
			Text = "Made for dedicated server communities",
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		Grid.SetRow(footer, 2);
		root.Children.Add(footer);

		Content = root;
		Loaded += delegate { RebuildServers(); };
		_viewModel.Servers.CollectionChanged += delegate { RebuildServers(); };
	}

	private Grid BuildHeader()
	{
		Grid header = new Grid
		{
			Margin = new Thickness(28.0, 28.0, 28.0, 16.0)
		};
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel titleRow = new StackPanel { Orientation = Orientation.Horizontal };
		titleRow.Children.Add(new TextBlock
		{
			Text = "▦",
			Foreground = BrushFrom("#2dd4ff"),
			FontSize = 25.0,
			FontWeight = FontWeights.Bold,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 0.0, 16.0, 0.0)
		});
		StackPanel titleStack = new StackPanel();
		titleStack.Children.Add(new TextBlock
		{
			Text = "Dashboard",
			Foreground = Brushes.White,
			FontSize = 26.0,
			FontWeight = FontWeights.Bold
		});
		titleStack.Children.Add(new TextBlock
		{
			Text = "Fleet overview for configured dedicated game servers",
			Foreground = BrushFrom("#a7bad8"),
			FontSize = 12.5
		});
		titleRow.Children.Add(titleStack);
		header.Children.Add(titleRow);

		StackPanel actions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Top
		};
		actions.Children.Add(CreateActionButton("Refresh", "#7138d8", delegate { _viewModel.RefreshCommand.Execute(null); }));
		Grid.SetColumn(actions, 1);
		header.Children.Add(actions);
		return header;
	}

	private UIElement BuildStatsRow()
	{
		UniformGrid stats = new UniformGrid
		{
			Columns = 5,
			Margin = new Thickness(0.0, 0.0, 0.0, 16.0)
		};
		stats.Children.Add(CreateStatCard("Fleet Status", "FleetState", null, "#4f46e5"));
		stats.Children.Add(CreateStatCard("Managed", "ManagedCount", "Total servers", "#2563eb"));
		stats.Children.Add(CreateStatCard("Online", "OnlineCount", "Servers online", "#0891b2"));
		stats.Children.Add(CreateStatCard("Players", "TotalPlayers", "Total players", "#22c55e"));
		stats.Children.Add(CreateStatCard("Mods", "ModCount", "Total mods", "#facc15"));
		return stats;
	}

	private Border BuildServersPanel()
	{
		Border panel = CreatePanel();
		panel.MinHeight = 340.0;
		panel.Margin = new Thickness(0.0, 0.0, 0.0, 16.0);
		Grid grid = new Grid();
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		grid.Children.Add(CreateSectionTitle("▦", "Servers", null));
		ScrollViewer cardScroll = new ScrollViewer
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
		};
		cardScroll.Content = _serverCards;
		Grid.SetRow(cardScroll, 1);
		grid.Children.Add(cardScroll);
		panel.Child = grid;
		return panel;
	}

	private Border BuildActivityPanel()
	{
		Border panel = CreatePanel();
		panel.MinHeight = 240.0;
		Grid root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		Grid header = new Grid();
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		header.Children.Add(CreateSectionTitle("◒", "Activity Console", "Real-time actions, SteamCMD progress, and errors"));
		header.Children.Add(CreateActionButton("Clear", "#7138d8", delegate { _viewModel.ClearActivityCommand.Execute(null); }));
		Grid.SetColumn(header.Children[1], 1);
		root.Children.Add(header);

		ListBox entries = new ListBox
		{
			Background = BrushFrom("#99111d2a"),
			BorderBrush = BrushFrom("#2a4564"),
			BorderThickness = new Thickness(1.0),
			Foreground = Brushes.White,
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0),
			Padding = new Thickness(8.0),
			MaxHeight = 220.0
		};
		entries.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ActivityEntries"));
		entries.ItemTemplate = CreateActivityTemplate();
		Grid.SetRow(entries, 1);
		root.Children.Add(entries);
		panel.Child = root;
		return panel;
	}

	private Border BuildInlineConfigurePanel()
	{
		Border panel = CreatePanel();
		panel.Margin = new Thickness(0.0, 0.0, 0.0, 16.0);
		panel.Visibility = Visibility.Collapsed;
		Grid root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		Grid header = new Grid();
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		StackPanel title = new StackPanel();
		title.Children.Add(new TextBlock
		{
			Text = "Configure Server",
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.Bold
		});
		title.Children.Add(_configureSubtitle);
		header.Children.Add(title);
		Button close = CreateActionButton("Close", "#203249", delegate { _configurePanel.Visibility = Visibility.Collapsed; });
		Grid.SetColumn(close, 1);
		header.Children.Add(close);
		root.Children.Add(header);

		_configureTabs.Margin = new Thickness(0.0, 16.0, 0.0, 0.0);
		_configureTabs.Background = Brushes.Transparent;
		_configureTabs.BorderThickness = new Thickness(0.0);
		_configureTabs.ItemContainerStyle = CreateTabItemStyle();
		_configureTabs.Template = CreateTabControlTemplate();
		_configureTabs.Items.Add(new TabItem { Header = "Settings", Content = BuildSettingsTab() });
		_configureTabs.Items.Add(new TabItem { Header = "INI", Content = BuildIniTab() });
		_configureTabs.Items.Add(new TabItem { Header = "Console", Content = BuildConsoleTab() });
		Grid.SetRow(_configureTabs, 1);
		root.Children.Add(_configureTabs);
		panel.Child = root;
		return panel;
	}

	private FrameworkElement BuildSettingsTab()
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		AddLabeledEditor(grid, "Server Name", "SelectedServer.Name", 0, 0);
		AddMapEditor(grid, 0, 1);
		AddLabeledEditor(grid, "Install Directory", "SelectedServer.InstallDirectory", 1, 0, 2);
		AddLabeledEditor(grid, "Executable Name", "SelectedServer.ExecutableName", 2, 0);
		AddLabeledEditor(grid, "Cluster ID", "SelectedServer.ClusterId", 2, 1);
		AddClusterDirectoryEditor(grid, 3, 0, 2);
		AddLabeledEditor(grid, "Game Port", "SelectedServer.GamePort", 4, 0);
		AddLabeledEditor(grid, "Query Port", "SelectedServer.QueryPort", 4, 1);

		Button save = CreateActionButton("Save Server", "#4658ff", delegate
		{
			if (_viewModel.SaveServerCommand.CanExecute(null))
			{
				_viewModel.SaveServerCommand.Execute(null);
			}
		});
		save.Width = 130.0;
		save.Margin = new Thickness(16.0, 24.0, 0.0, 0.0);
		save.VerticalAlignment = VerticalAlignment.Top;
		Grid.SetColumn(save, 2);
		Grid.SetRow(save, 4);
		grid.Children.Add(save);
		return grid;
	}

	private FrameworkElement BuildIniTab()
	{
		StackPanel root = new StackPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		root.Children.Add(new TextBlock
		{
			Text = "INI Settings Manager",
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.Bold
		});
		root.Children.Add(new TextBlock
		{
			Text = "Edit the server setting catalog, or import/export Game.ini and GameUserSettings.ini.",
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 5.0, 0.0, 14.0)
		});

		Border countPanel = new Border
		{
			Background = BrushFrom("#99091624"),
			BorderBrush = BrushFrom("#28445f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(7.0),
			Padding = new Thickness(12.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
		countPanel.Child = new TextBlock
		{
			Text = _viewModel.IniSettings.Count + " server settings loaded from the configuration catalog",
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 12.0
		};
		root.Children.Add(countPanel);

		WrapPanel actions = new WrapPanel();
		actions.Children.Add(CreateIniButton("Edit INI Settings", "#4658ff", delegate { EditIniSettings(); }));
		actions.Children.Add(CreateIniButton("Import Game.ini", "#00a875", async delegate { await ImportIniFileAsync("Game.ini"); }));
		actions.Children.Add(CreateIniButton("Export Game.ini", "#203249", delegate { ExportIniFile("Game.ini"); }));
		actions.Children.Add(CreateIniButton("Import GameUserSettings.ini", "#00a875", async delegate { await ImportIniFileAsync("GameUserSettings.ini"); }));
		actions.Children.Add(CreateIniButton("Export GameUserSettings.ini", "#203249", delegate { ExportIniFile("GameUserSettings.ini"); }));
		root.Children.Add(actions);

		TextBlock path = new TextBlock
		{
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		path.SetBinding(TextBlock.TextProperty, new Binding("SelectedServer.InstallDirectory")
		{
			StringFormat = "Server config folder: {0}\\ShooterGame\\Saved\\Config\\WindowsServer",
			TargetNullValue = "Select a server to import or export INI files."
		});
		root.Children.Add(path);
		return root;
	}

	private FrameworkElement BuildConsoleTab()
	{
		return new RconConsoleSurface(_viewModel.ServerConsole);
	}

	private void AddLabeledEditor(Grid grid, string label, string bindingPath, int row, int column, int columnSpan = 1)
	{
		StackPanel stack = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 12.0, 12.0)
		};
		stack.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});
		TextBox box = new TextBox
		{
			Background = BrushFrom("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = BrushFrom("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(9.0),
			MinHeight = 34.0
		};
		box.SetBinding(TextBox.TextProperty, new Binding(bindingPath)
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		stack.Children.Add(box);
		Grid.SetRow(stack, row);
		Grid.SetColumn(stack, column);
		Grid.SetColumnSpan(stack, columnSpan);
		grid.Children.Add(stack);
	}

	private void AddClusterDirectoryEditor(Grid grid, int row, int column, int columnSpan = 1)
	{
		StackPanel stack = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 12.0, 12.0)
		};
		stack.Children.Add(new TextBlock
		{
			Text = "Cluster Directory",
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});

		Grid picker = new Grid();
		picker.ColumnDefinitions.Add(new ColumnDefinition());
		picker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		picker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		TextBox box = new TextBox
		{
			Background = BrushFrom("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = BrushFrom("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(9.0),
			MinHeight = 34.0
		};
		box.SetBinding(TextBox.TextProperty, new Binding("SelectedServer.ClusterDirectory")
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		picker.Children.Add(box);

		Button browse = CreateButton("Browse", "#203249");
		browse.MinWidth = 92.0;
		browse.Height = 34.0;
		browse.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		browse.Click += BrowseClusterDirectory;
		Grid.SetColumn(browse, 1);
		picker.Children.Add(browse);

		Button defaultButton = CreateButton("Default", "#203249");
		defaultButton.MinWidth = 92.0;
		defaultButton.Height = 34.0;
		defaultButton.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		defaultButton.Click += RestoreDefaultClusterDirectory;
		Grid.SetColumn(defaultButton, 2);
		picker.Children.Add(defaultButton);

		stack.Children.Add(picker);
		Grid.SetRow(stack, row);
		Grid.SetColumn(stack, column);
		Grid.SetColumnSpan(stack, columnSpan);
		grid.Children.Add(stack);
	}

	private void AddMapEditor(Grid grid, int row, int column)
	{
		StackPanel stack = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 12.0, 12.0)
		};
		stack.Children.Add(new TextBlock
		{
			Text = "Map",
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});
		ComboBox box = new ComboBox
		{
			Background = BrushFrom("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = BrushFrom("#28445f"),
			MinHeight = 34.0
		};
		box.ItemsSource = _viewModel.MapNames.Concat(new[] { CustomMapOption }).ToList();
		stack.Children.Add(box);
		StackPanel customStack = new StackPanel
		{
			Visibility = Visibility.Collapsed
		};
		customStack.Children.Add(new TextBlock
		{
			Text = CustomMapOption,
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 11.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 5.0)
		});
		TextBox customBox = new TextBox
		{
			Background = BrushFrom("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = BrushFrom("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(9.0),
			MinHeight = 34.0
		};
		customStack.Children.Add(customBox);
		stack.Children.Add(customStack);
		WireCustomMapEditor(box, customStack, customBox);
		Grid.SetRow(stack, row);
		Grid.SetColumn(stack, column);
		grid.Children.Add(stack);
	}

	private void WireCustomMapEditor(ComboBox mapBox, FrameworkElement customStack, TextBox customBox)
	{
		bool updating = false;

		void RefreshSelection()
		{
			updating = true;
			string mapName = _viewModel.SelectedServer?.MapName ?? string.Empty;
			bool custom = IsCustomMapName(mapName);
			mapBox.SelectedItem = custom
				? CustomMapOption
				: _viewModel.MapNames.FirstOrDefault((string knownMap) => string.Equals(knownMap, mapName, StringComparison.OrdinalIgnoreCase));
			mapBox.Text = custom ? CustomMapOption : mapName;
			customStack.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
			customBox.Text = custom ? mapName : string.Empty;
			updating = false;
		}

		mapBox.SelectionChanged += delegate
		{
			if (updating || _viewModel.SelectedServer == null)
			{
				return;
			}

			string selected = mapBox.SelectedItem as string ?? string.Empty;
			bool custom = string.Equals(selected, CustomMapOption, StringComparison.Ordinal);
			customStack.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
			if (custom)
			{
				customBox.Focus();
				return;
			}

			_viewModel.SelectedServer.MapName = selected;
		};

		customBox.TextChanged += delegate
		{
			if (!updating && _viewModel.SelectedServer != null && string.Equals(mapBox.SelectedItem as string, CustomMapOption, StringComparison.Ordinal))
			{
				_viewModel.SelectedServer.MapName = customBox.Text.Trim();
			}
		};

		_viewModel.PropertyChanged += delegate(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(DashboardViewModel.SelectedServer))
			{
				RefreshSelection();
			}
		};
		Loaded += delegate
		{
			RefreshSelection();
			Dispatcher.BeginInvoke((Action)RefreshSelection, System.Windows.Threading.DispatcherPriority.Loaded);
		};
	}

	private bool IsCustomMapName(string mapName)
	{
		return !string.IsNullOrWhiteSpace(mapName)
			&& !_viewModel.MapNames.Any((string knownMap) => string.Equals(knownMap, mapName, StringComparison.OrdinalIgnoreCase));
	}

	private void BrowseClusterDirectory(object sender, RoutedEventArgs e)
	{
		if (_viewModel.SelectedServer == null)
		{
			return;
		}

		OpenFolderDialog dialog = new OpenFolderDialog
		{
			Title = "Select shared ASA cluster folder",
			InitialDirectory = GetInitialDirectory(_viewModel.SelectedServer.ClusterDirectory),
			Multiselect = false
		};

		if (dialog.ShowDialog(Window.GetWindow(this)).GetValueOrDefault() && !string.IsNullOrWhiteSpace(dialog.FolderName))
		{
			_viewModel.SelectedServer.ClusterDirectory = dialog.FolderName;
		}
	}

	private void RestoreDefaultClusterDirectory(object sender, RoutedEventArgs e)
	{
		if (_viewModel.SelectedServer != null)
		{
			_viewModel.SelectedServer.ClusterDirectory = DashboardViewModel.DefaultClusterDirectory;
		}
	}

	private static string GetInitialDirectory(string currentDirectory)
	{
		if (Directory.Exists(currentDirectory))
		{
			return currentDirectory;
		}

		string defaultDirectory = DashboardViewModel.DefaultClusterDirectory;
		if (Directory.Exists(defaultDirectory))
		{
			return defaultDirectory;
		}

		return Environment.GetFolderPath(Environment.SpecialFolder.Personal);
	}

	private DataTemplate CreateActivityTemplate()
	{
		FrameworkElementFactory row = new FrameworkElementFactory(typeof(StackPanel));
		row.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
		row.SetValue(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 0.0, 8.0));
		FrameworkElementFactory time = new FrameworkElementFactory(typeof(TextBlock));
		time.SetValue(TextBlock.ForegroundProperty, BrushFrom("#b8c8df"));
		time.SetValue(TextBlock.FontSizeProperty, 12.0);
		time.SetValue(FrameworkElement.WidthProperty, 70.0);
		time.SetBinding(TextBlock.TextProperty, new Binding("Timestamp") { StringFormat = "{0:HH:mm:ss}" });
		row.AppendChild(time);
		FrameworkElementFactory level = new FrameworkElementFactory(typeof(Border));
		level.SetValue(Border.BackgroundProperty, BrushFrom("#28486f"));
		level.SetValue(Border.CornerRadiusProperty, new CornerRadius(5.0));
		level.SetValue(Border.PaddingProperty, new Thickness(8.0, 2.0, 8.0, 2.0));
		level.SetValue(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 22.0, 0.0));
		FrameworkElementFactory levelText = new FrameworkElementFactory(typeof(TextBlock));
		levelText.SetValue(TextBlock.ForegroundProperty, Brushes.White);
		levelText.SetValue(TextBlock.FontSizeProperty, 11.0);
		levelText.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
		levelText.SetBinding(TextBlock.TextProperty, new Binding("Level"));
		level.AppendChild(levelText);
		row.AppendChild(level);
		FrameworkElementFactory message = new FrameworkElementFactory(typeof(TextBlock));
		message.SetValue(TextBlock.ForegroundProperty, BrushFrom("#d8e8ff"));
		message.SetValue(TextBlock.FontSizeProperty, 12.0);
		message.SetBinding(TextBlock.TextProperty, new Binding("Message"));
		row.AppendChild(message);
		return new DataTemplate { VisualTree = row };
	}

	private void RebuildServers()
	{
		_serverCards.Children.Clear();
		foreach (ServerInstance server in _viewModel.Servers)
		{
			_serverCards.Children.Add(CreateServerCard(server));
		}
	}

	private Border CreateServerCard(ServerInstance server)
	{
		Border card = new Border
		{
			Width = 430.0,
			Height = 250.0,
			CornerRadius = new CornerRadius(8.0),
			BorderThickness = new Thickness(1.0),
			BorderBrush = BrushFrom("#2e6d88"),
			Background = BrushFrom("#aa0b1623"),
			Margin = new Thickness(0.0, 0.0, 18.0, 0.0),
			ClipToBounds = true
		};
		Grid root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(92.0) });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76.0) });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.Children.Add(new Border
		{
			Background = new ImageBrush
			{
				ImageSource = new BitmapImage(GetMapImageUri(server.MapName)),
				Stretch = Stretch.UniformToFill,
				Opacity = 0.58
			}
		});
		root.Children.Add(new Border
		{
			Width = 5.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = BrushFrom("#21d67b")
		});
		Grid header = new Grid { Margin = new Thickness(18.0, 14.0, 14.0, 8.0) };
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		StackPanel title = new StackPanel();
		title.Children.Add(new TextBlock
		{
			Text = "Session: " + server.Name,
			Foreground = Brushes.White,
			FontSize = 16.0,
			FontWeight = FontWeights.Bold
		});
		title.Children.Add(new TextBlock
		{
			Text = "Map: " + server.MapName,
			Foreground = Brushes.White,
			FontSize = 12.0,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 7.0, 0.0, 0.0)
		});
		TextBlock endpoint = new TextBlock
		{
			Text = "IP: checking...",
			Foreground = Brushes.White,
			FontSize = 12.0,
			FontWeight = FontWeights.SemiBold
		};
		title.Children.Add(endpoint);
		_ = SetPublicEndpointAsync(endpoint, server.GamePort);
		header.Children.Add(title);
		header.Children.Add(CreateStatusBadge(server));
		Grid.SetColumn(header.Children[1], 1);
		root.Children.Add(header);

		Grid metrics = new Grid { Margin = new Thickness(18.0, 8.0, 14.0, 8.0) };
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.Children.Add(CreateMetricTile("CPU", "CpuUsageText", server, 0));
		metrics.Children.Add(CreateMetricTile("Memory", "MemoryUsageText", server, 1));
		metrics.Children.Add(CreateUsersTile(server, 2));
		Grid.SetRow(metrics, 1);
		root.Children.Add(metrics);

		Grid footer = new Grid { Margin = new Thickness(18.0, 2.0, 14.0, 14.0) };
		footer.ColumnDefinitions.Add(new ColumnDefinition());
		footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		footer.Children.Add(new TextBlock
		{
			Text = server.MapName,
			Foreground = BrushFrom("#d8e8ff"),
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center
		});
		StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
		buttons.Children.Add(CreateMiniButton("Start", "#16a34a", delegate { _viewModel.StartServerTileCommand.Execute(server); }));
		buttons.Children.Add(CreateMiniButton("Stop", "#dc3f5c", delegate { _viewModel.StopServerTileCommand.Execute(server); }));
		Grid.SetColumn(buttons, 1);
		footer.Children.Add(buttons);
		Grid.SetRow(footer, 2);
		root.Children.Add(footer);
		card.Child = root;
		return card;
	}

	private Border CreateStatusBadge(ServerInstance server)
	{
		Border badge = new Border
		{
			Background = BrushFrom("#16a34a"),
			CornerRadius = new CornerRadius(14.0),
			Padding = new Thickness(10.0, 5.0, 10.0, 5.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		TextBlock text = new TextBlock
		{
			Foreground = Brushes.White,
			FontWeight = FontWeights.Bold,
			FontSize = 11.0
		};
		text.SetBinding(TextBlock.TextProperty, new Binding("StatusText") { Source = server });
		badge.Child = text;
		return badge;
	}

	private Border CreateMetricTile(string label, string valuePath, ServerInstance server, int column)
	{
		Border tile = CreateMetricShell(column);
		StackPanel stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		stack.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = Brushes.White,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Center
		});
		TextBlock value = new TextBlock
		{
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		value.SetBinding(TextBlock.TextProperty, new Binding(valuePath) { Source = server });
		stack.Children.Add(value);
		tile.Child = stack;
		return tile;
	}

	private Border CreateUsersTile(ServerInstance server, int column)
	{
		Border tile = CreateMetricShell(column);
		StackPanel stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		stack.Children.Add(new TextBlock
		{
			Text = "Users",
			Foreground = Brushes.White,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Center
		});
		TextBlock value = new TextBlock
		{
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		value.SetBinding(TextBlock.TextProperty, new Binding("PlayerCount") { Source = server, StringFormat = "{0}/" + server.MaxPlayers });
		stack.Children.Add(value);
		tile.Child = stack;
		return tile;
	}

	private Border CreateMetricShell(int column)
	{
		Border tile = new Border
		{
			Background = BrushFrom("#99091624"),
			BorderBrush = BrushFrom("#28445f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(7.0),
			Margin = new Thickness(4.0),
			Padding = new Thickness(8.0)
		};
		Grid.SetColumn(tile, column);
		return tile;
	}

	private Border CreateStatCard(string label, string valuePath, string? subtext, string accent)
	{
		Border card = new Border
		{
			Background = new LinearGradientBrush(ColorFrom("#aa1b2248"), ColorFrom("#9920424b"), 0.0),
			BorderBrush = BrushFrom("#234260"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(20.0),
			Margin = new Thickness(0.0, 0.0, 16.0, 0.0),
			MinHeight = 120.0
		};
		StackPanel stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = BrushFrom("#a7bad8"),
			FontSize = 12.0
		});
		TextBlock value = new TextBlock
		{
			Foreground = BrushFrom(accent),
			FontSize = 25.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 8.0, 0.0, 4.0)
		};
		value.SetBinding(TextBlock.TextProperty, new Binding(valuePath));
		stack.Children.Add(value);
		stack.Children.Add(new TextBlock
		{
			Text = subtext ?? "Overall status",
			Foreground = BrushFrom("#a7bad8"),
			FontSize = 12.0
		});
		card.Child = stack;
		return card;
	}

	private StackPanel CreateSectionTitle(string icon, string title, string? subtitle)
	{
		StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
		row.Children.Add(new TextBlock
		{
			Text = icon,
			Foreground = BrushFrom("#2dd4ff"),
			FontSize = 16.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		});
		StackPanel text = new StackPanel();
		text.Children.Add(new TextBlock
		{
			Text = title,
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.Bold
		});
		if (!string.IsNullOrWhiteSpace(subtitle))
		{
			text.Children.Add(new TextBlock
			{
				Text = subtitle,
				Foreground = BrushFrom("#9fb8d6"),
				FontSize = 12.0
			});
		}
		row.Children.Add(text);
		return row;
	}

	private Border CreatePanel()
	{
		return new Border
		{
			Background = BrushFrom("#99111d2a"),
			BorderBrush = BrushFrom("#24435f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(16.0)
		};
	}

	private Button CreateActionButton(string text, string color, RoutedEventHandler click)
	{
		Button button = CreateButton(text, color);
		button.MinWidth = 104.0;
		button.Height = 36.0;
		button.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
		button.Click += click;
		return button;
	}

	private Button CreateMiniButton(string text, string color, RoutedEventHandler click)
	{
		Button button = CreateButton(text, color);
		button.Width = text == "Configure" ? 78.0 : 62.0;
		button.Height = 34.0;
		button.FontSize = 11.0;
		button.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
		button.Click += click;
		return button;
	}

	private Button CreateIniButton(string text, string color, RoutedEventHandler click)
	{
		Button button = CreateButton(text, color);
		button.MinWidth = 170.0;
		button.Height = 36.0;
		button.Margin = new Thickness(0.0, 0.0, 8.0, 8.0);
		button.Click += click;
		return button;
	}

	private Button CreateButton(string text, string color)
	{
		Button button = new Button
		{
			Content = text,
			Background = BrushFrom(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0)
		};
		button.Template = CreateButtonTemplate();
		return button;
	}

	private ControlTemplate CreateButtonTemplate()
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

	private static ControlTemplate CreateTabControlTemplate()
	{
		ControlTemplate template = new ControlTemplate(typeof(TabControl));
		FrameworkElementFactory root = new FrameworkElementFactory(typeof(DockPanel));
		root.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
		root.SetValue(FrameworkElement.SnapsToDevicePixelsProperty, true);

		FrameworkElementFactory tabPanel = new FrameworkElementFactory(typeof(TabPanel));
		tabPanel.SetValue(DockPanel.DockProperty, Dock.Top);
		tabPanel.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
		tabPanel.SetValue(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 0.0, 8.0));
		tabPanel.SetValue(TabPanel.IsItemsHostProperty, true);

		FrameworkElementFactory contentBorder = new FrameworkElementFactory(typeof(Border));
		contentBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
		contentBorder.SetValue(Border.BorderThicknessProperty, new Thickness(0.0));
		FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
		presenter.SetValue(ContentPresenter.ContentSourceProperty, "SelectedContent");
		presenter.SetValue(ContentPresenter.SnapsToDevicePixelsProperty, true);
		contentBorder.AppendChild(presenter);

		root.AppendChild(tabPanel);
		root.AppendChild(contentBorder);

		template.VisualTree = root;
		return template;
	}

	private static Style CreateTabItemStyle()
	{
		Style style = new Style(typeof(TabItem));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.BackgroundProperty, BrushFrom("#0b1422")));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, BrushFrom("#28445f")));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(13.0, 7.0, 13.0, 7.0)));
		style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 6.0, 0.0)));

		ControlTemplate template = new ControlTemplate(typeof(TabItem));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6.0));

		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.ContentSourceProperty, "Header");
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		border.AppendChild(content);
		template.VisualTree = border;

		Trigger selected = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, BrushFrom("#4658ff")));
		selected.Setters.Add(new Setter(Control.BorderBrushProperty, BrushFrom("#4658ff")));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		template.Triggers.Add(selected);

		Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, BrushFrom("#203249")));
		template.Triggers.Add(hover);

		style.Setters.Add(new Setter(Control.TemplateProperty, template));
		return style;
	}

	private async void AddExistingServer(object sender, RoutedEventArgs e)
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "Select an existing ASA install folder or ShooterGame Saved folder",
			InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
			CheckFileExists = false,
			CheckPathExists = true,
			FileName = "Select this folder",
			Filter = "Folders|*.folder",
			ValidateNames = false
		};
		if (dialog.ShowDialog(Window.GetWindow(this)).GetValueOrDefault())
		{
			string? directory = Path.GetDirectoryName(dialog.FileName);
			if (!string.IsNullOrWhiteSpace(directory))
			{
				await _viewModel.AddExistingServerAsync(directory);
			}
		}
	}

	private void EditIniSettings()
	{
		_viewModel.ReloadIniSettingsFromSelectedServerFiles();
		IniEditorWindow window = new IniEditorWindow(_viewModel.IniSettings, async delegate
		{
			await _viewModel.SaveIniSettingsAsync();
			if (_viewModel.SelectedServer != null)
			{
				_configureSubtitle.Text = _viewModel.SelectedServer.Name;
			}
		})
		{
			Owner = Window.GetWindow(this)
		};
		window.ShowDialog();
	}

	private async Task ImportIniFileAsync(string fileName)
	{
		if (!TryGetSelectedIniPath(fileName, out string destinationPath))
		{
			return;
		}
		OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "Import " + fileName,
			Filter = fileName + "|" + fileName + "|INI files|*.ini|All files|*.*",
			CheckFileExists = true
		};
		if (!dialog.ShowDialog(Window.GetWindow(this)).GetValueOrDefault())
		{
			return;
		}
		Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);
		if (File.Exists(destinationPath))
		{
			MessageBoxResult result = MessageBox.Show(fileName + " already exists for this server. Replace it?", "Import " + fileName, MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result != MessageBoxResult.Yes)
			{
				return;
			}
		}
		if (!string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
		{
			File.Copy(dialog.FileName, destinationPath, overwrite: true);
		}
		int updatedCount = await _viewModel.ReloadImportedIniSettingsAsync(fileName);
		string message = fileName + " imported to this server's config folder.";
		if (updatedCount > 0)
		{
			message += Environment.NewLine + "Updated " + updatedCount + " matching editor setting(s).";
		}
		else
		{
			message += Environment.NewLine + "The full file was saved, but none of its keys matched the built-in editor catalog.";
		}
		MessageBox.Show(message, "INI import", MessageBoxButton.OK, MessageBoxImage.Information);
	}

	private void ExportIniFile(string fileName)
	{
		if (!TryGetSelectedIniPath(fileName, out string sourcePath))
		{
			return;
		}
		if (!File.Exists(sourcePath))
		{
			MessageBox.Show(fileName + " does not exist yet for this server.", "INI export", MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		SaveFileDialog dialog = new SaveFileDialog
		{
			Title = "Export " + fileName,
			FileName = fileName,
			Filter = fileName + "|" + fileName + "|INI files|*.ini|All files|*.*",
			OverwritePrompt = true
		};
		if (!dialog.ShowDialog(Window.GetWindow(this)).GetValueOrDefault())
		{
			return;
		}
		File.Copy(sourcePath, dialog.FileName, overwrite: true);
		MessageBox.Show(fileName + " exported.", "INI export", MessageBoxButton.OK, MessageBoxImage.Information);
	}

	private bool TryGetSelectedIniPath(string fileName, out string path)
	{
		path = string.Empty;
		if (_viewModel.SelectedServer == null)
		{
			MessageBox.Show("Select a server first.", "INI files", MessageBoxButton.OK, MessageBoxImage.Information);
			return false;
		}
		if (string.IsNullOrWhiteSpace(_viewModel.SelectedServer.InstallDirectory))
		{
			MessageBox.Show("Set an install directory for this server first.", "INI files", MessageBoxButton.OK, MessageBoxImage.Information);
			return false;
		}
		path = Path.Combine(_viewModel.SelectedServer.InstallDirectory, "ShooterGame", "Saved", "Config", "WindowsServer", fileName);
		return true;
	}

	private void OpenConfigure(ServerInstance server, bool console)
	{
		_viewModel.SelectServerCommand.Execute(server);
		_configureSubtitle.Text = server.Name;
		_configureTabs.SelectedIndex = console ? 2 : 0;
		_configurePanel.Visibility = Visibility.Visible;
		_configurePanel.BringIntoView();
	}

	private void OnConfigureRequested(ServerInstance server, bool console)
	{
		if (!Dispatcher.CheckAccess())
		{
			Dispatcher.Invoke(delegate { OpenConfigure(server, console); });
			return;
		}
		OpenConfigure(server, console);
	}

	private void OpenConfigureOnServersTab(ServerInstance server, bool console)
	{
		if (Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
		{
			MenuItemViewModel? serversItem = mainViewModel.MenuItems.FirstOrDefault((MenuItemViewModel item) => string.Equals(item.Title, "Servers", StringComparison.OrdinalIgnoreCase));
			if (serversItem != null)
			{
				mainViewModel.SelectedMenuItem = serversItem;
			}
		}
		_viewModel.RequestConfigure(server, console);
	}

	private static async Task SetPublicEndpointAsync(TextBlock endpoint, int port)
	{
		string text = await GetPublicEndpointAsync(port);
		await endpoint.Dispatcher.InvokeAsync(delegate { endpoint.Text = "IP: " + text; });
	}

	private static async Task<string> GetPublicEndpointAsync(int port)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_cachedPublicIpAddress) || DateTime.UtcNow - _cachedPublicIpAddressAt > TimeSpan.FromMinutes(10.0))
			{
				_cachedPublicIpAddress = (await PublicIpClient.GetStringAsync("https://api.ipify.org").ConfigureAwait(false)).Trim();
				_cachedPublicIpAddressAt = DateTime.UtcNow;
			}
			return _cachedPublicIpAddress + ":" + port;
		}
		catch
		{
			return GetLocalEndpoint(port);
		}
	}

	private static string GetLocalEndpoint(int port)
	{
		try
		{
			string? address = Dns.GetHostEntry(Dns.GetHostName()).AddressList
				.FirstOrDefault(static candidate => candidate.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(candidate))
				?.ToString();
			return (string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address) + ":" + port;
		}
		catch
		{
			return "127.0.0.1:" + port;
		}
	}

	private static Brush CreateBackgroundBrush()
	{
		LinearGradientBrush brush = new LinearGradientBrush
		{
			StartPoint = new Point(0.0, 0.0),
			EndPoint = new Point(1.0, 1.0)
		};
		brush.GradientStops.Add(new GradientStop(ColorFrom("#070d18"), 0.0));
		brush.GradientStops.Add(new GradientStop(ColorFrom("#072b47"), 0.38));
		brush.GradientStops.Add(new GradientStop(ColorFrom("#15152b"), 0.72));
		brush.GradientStops.Add(new GradientStop(ColorFrom("#07101c"), 1.0));
		return brush;
	}

	private static Uri GetMapImageUri(string mapName)
	{
		string asset = (mapName ?? "TheIsland").Trim() switch
		{
			"TheIsland_WP" or "TheIsland" => "theisland",
			"ScorchedEarth_WP" or "ScorchedEarth" => "scorchedearth",
			"TheCenter_WP" or "TheCenter" => "thecenter",
			"Aberration_WP" or "Aberration" => "aberration",
			"Extinction_WP" or "Extinction" => "extinction",
			"Ragnarok_WP" or "Ragnarok" => "ragnarok",
			"Valguero_WP" or "Valguero" => "valguero",
			_ => "theisland"
		};
		return new Uri("pack://application:,,,/assets/maps/" + asset + ".png", UriKind.Absolute);
	}

	private static SolidColorBrush BrushFrom(string hex)
	{
		return new SolidColorBrush(ColorFrom(hex));
	}

	private static Color ColorFrom(string hex)
	{
		return (Color)ColorConverter.ConvertFromString(hex);
	}
}
