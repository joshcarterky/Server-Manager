using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using ServerManager.Models;
using ServerManager.ViewModels;
using Microsoft.Win32;

namespace ServerManager.Views;

public class ConfigureServerPanel : Border
{
	private const string CustomMapOption = "Custom Map";

	private readonly DashboardViewModel _viewModel;
	private readonly TextBlock _subtitle;
	private readonly TabControl _tabs;

	public ConfigureServerPanel(DashboardViewModel viewModel)
	{
		_viewModel = viewModel;
		DataContext = viewModel;
		_subtitle = new TextBlock
		{
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0
		};
		_tabs = new TabControl();
		Margin = new Thickness(24.0, 0.0, 24.0, 24.0);
		Padding = new Thickness(18.0);
		Background = Brush("#aa0d1828");
		BorderBrush = Brush("#263a58");
		BorderThickness = new Thickness(1.0);
		CornerRadius = new CornerRadius(8.0);
		Visibility = Visibility.Collapsed;
		Child = BuildContent();
	}

	public void Open(ServerInstance server, bool console)
	{
		_viewModel.SelectServerCommand.Execute(server);
		_subtitle.Text = server.Name;
		_tabs.SelectedIndex = console ? 3 : 0;
		Visibility = Visibility.Visible;
		BringIntoView();
	}

	private UIElement BuildContent()
	{
		Grid root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		Grid header = new Grid();
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		StackPanel title = new StackPanel();
		title.Children.Add(new TextBlock
		{
			Text = "Configure Server",
			Foreground = Brushes.White,
			FontSize = 20.0,
			FontWeight = FontWeights.Bold
		});
		title.Children.Add(_subtitle);
		header.Children.Add(title);
		Button close = CreateButton("Close", "#203249");
		close.Width = 92.0;
		close.Height = 34.0;
		close.Click += delegate { Visibility = Visibility.Collapsed; };
		Grid.SetColumn(close, 1);
		header.Children.Add(close);
		root.Children.Add(header);

		_tabs.Margin = new Thickness(0.0, 16.0, 0.0, 0.0);
		_tabs.Background = Brushes.Transparent;
		_tabs.BorderThickness = new Thickness(0.0);
		_tabs.Foreground = Brushes.White;
		_tabs.ItemContainerStyle = CreateTabItemStyle();
		_tabs.Template = CreateTabControlTemplate();
		_tabs.Items.Add(new TabItem { Header = "Settings", Content = BuildSettingsTab() });
		_tabs.Items.Add(new TabItem { Header = "Mods", Content = BuildModsTab() });
		_tabs.Items.Add(new TabItem { Header = "INI", Content = BuildIniTab() });
		_tabs.Items.Add(new TabItem { Header = "Console", Content = BuildConsoleTab() });
		Grid.SetRow(_tabs, 1);
		root.Children.Add(_tabs);
		return root;
	}

	private FrameworkElement BuildSettingsTab()
	{
		Grid grid = new Grid { Margin = new Thickness(0.0, 12.0, 0.0, 0.0) };
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		for (int i = 0; i < 6; i++)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		}

		AddGameEditor(grid, 0, 0);
		AddLabeledEditor(grid, "Server Name", "SelectedServer.Name", 0, 1);
		AddMapEditor(grid, 1, 0);
		AddLabeledEditor(grid, "Install Directory", "SelectedServer.InstallDirectory", 1, 1);
		AddLabeledEditor(grid, "Executable Name", "SelectedServer.ExecutableName", 2, 0);
		AddLabeledEditor(grid, "Cluster ID", "SelectedServer.ClusterId", 2, 1);
		AddClusterDirectoryEditor(grid, 3, 0, 2);
		AddLabeledEditor(grid, "Game Port", "SelectedServer.GamePort", 4, 0);
		AddLabeledEditor(grid, "Query Port", "SelectedServer.QueryPort", 4, 1);

		Button save = CreateButton("Save Server", "#4658ff");
		save.Width = 130.0;
		save.Height = 36.0;
		save.Margin = new Thickness(16.0, 24.0, 0.0, 0.0);
		save.VerticalAlignment = VerticalAlignment.Top;
		save.Click += delegate
		{
			if (_viewModel.SaveServerCommand.CanExecute(null))
			{
				_viewModel.SaveServerCommand.Execute(null);
			}
		};
		Grid.SetColumn(save, 2);
		Grid.SetRow(save, 4);
		grid.Children.Add(save);
		return grid;
	}

	private FrameworkElement BuildModsTab()
	{
		Grid root = new Grid { Margin = new Thickness(0.0, 12.0, 0.0, 0.0) };
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		Border searchCard = ModsCard(new Thickness(0.0, 0.0, 0.0, 14.0));
		StackPanel searchStack = new StackPanel();
		searchStack.Children.Add(CardHeader("CF", "CurseForge Mods", "Search ASA mods, open CurseForge, add Project IDs, and manage this server's load order."));
		Grid search = new Grid { Margin = new Thickness(0.0, 18.0, 0.0, 0.0) };
		search.ColumnDefinitions.Add(new ColumnDefinition());
		search.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132.0) });
		search.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132.0) });
		search.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(148.0) });
		TextBox searchBox = new TextBox
		{
			Background = Brush("#111d2a"),
			Foreground = Brush("#dbeafe"),
			BorderBrush = Brush("#2a4061"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(12.0),
			MinHeight = 42.0,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		searchBox.SetBinding(TextBox.TextProperty, new Binding("CurseForgeSearchText")
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		search.Children.Add(searchBox);
		search.Children.Add(CommandButton("Search", "SearchCurseForgeCommand", "#4658ff", 1));
		search.Children.Add(CommandButton("Add ID", "AddManualModCommand", "#047857", 2));
		Button website = CreateButton("Open Website", "#203249");
		website.MinWidth = 132.0;
		website.Height = 42.0;
		website.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		website.Click += delegate { OpenCurseForgeBrowser(); };
		Grid.SetColumn(website, 3);
		search.Children.Add(website);
		searchStack.Children.Add(search);
		searchCard.Child = searchStack;
		root.Children.Add(searchCard);

		Border resultsCard = ModsCard(new Thickness(0.0, 0.0, 0.0, 10.0));
		Grid resultsGrid = new Grid();
		resultsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		resultsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(170.0) });
		resultsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		resultsGrid.Children.Add(CardHeader("SR", "Search Results", "Mods found from CurseForge search. Select one, then click Add Selected."));

		ListView results = ThemedListView("CurseForgeResults", "SelectedCurseForgeResult");
		results.Margin = new Thickness(0.0, 16.0, 0.0, 0.0);
		GridView resultView = new GridView();
		resultView.Columns.Add(Column("Project ID", "ProjectId", 120.0));
		resultView.Columns.Add(Column("Name", "Name", 360.0));
		resultView.Columns.Add(Column("Author", "Author", 220.0));
		resultView.Columns.Add(Column("Downloads", "DownloadCountText", 160.0));
		resultView.Columns.Add(Column("Updated", "LastUpdatedText", 160.0));
		results.View = resultView;
		Grid.SetRow(results, 1);
		resultsGrid.Children.Add(results);

		Grid modActions = new Grid { Margin = new Thickness(0.0, 14.0, 0.0, 0.0) };
		modActions.ColumnDefinitions.Add(new ColumnDefinition());
		modActions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		TextBlock status = new TextBlock
		{
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap,
			VerticalAlignment = VerticalAlignment.Center
		};
		status.SetBinding(TextBlock.TextProperty, new Binding("CurseForgeStatus"));
		modActions.Children.Add(status);
		StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
		buttons.Children.Add(CommandButton("Add Selected", "AddCurseForgeModCommand", "#047857"));
		buttons.Children.Add(CommandButton("Move Up", "MoveSelectedModUpCommand", "#203249"));
		buttons.Children.Add(CommandButton("Move Down", "MoveSelectedModDownCommand", "#203249"));
		buttons.Children.Add(CommandButton("Remove", "RemoveSelectedModCommand", "#b91c1c"));
		Grid.SetColumn(buttons, 1);
		modActions.Children.Add(buttons);
		Grid.SetRow(modActions, 2);
		resultsGrid.Children.Add(modActions);
		resultsCard.Child = resultsGrid;
		Grid.SetRow(resultsCard, 1);
		root.Children.Add(resultsCard);

		TextBlock hint = new TextBlock
		{
			Text = "Search works without an API key for common ASA mods. Use Add ID for any Project ID.",
			Foreground = Brush("#bfd6f6"),
			FontSize = 12.0,
			Margin = new Thickness(10.0, 0.0, 0.0, 12.0)
		};
		Grid.SetRow(hint, 2);
		root.Children.Add(hint);

		Border loadOrderCard = ModsCard(new Thickness(0.0));
		Grid loadOrderGrid = new Grid();
		loadOrderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		loadOrderGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150.0) });
		Grid loadOrderHeader = CardHeaderWithButton("LO", "Server Load Order", "Mods already added to this server. This is the list saved into the server launch configuration.", "View Installed Mods", delegate { OpenInstalledModsWindow(); });
		loadOrderGrid.Children.Add(loadOrderHeader);

		ListView serverMods = ThemedListView("SelectedServerMods", "SelectedServerMod");
		serverMods.Margin = new Thickness(0.0, 16.0, 0.0, 0.0);
		GridView modView = new GridView();
		modView.Columns.Add(Column("Order", "LoadOrder", 70.0));
		modView.Columns.Add(Column("Project ID", "WorkshopId", 100.0));
		modView.Columns.Add(Column("Name", "Title", 420.0));
		modView.Columns.Add(Column("Author", "Author", 220.0));
		modView.Columns.Add(Column("File", "LatestFileName", 260.0));
		serverMods.View = modView;
		Grid.SetRow(serverMods, 1);
		loadOrderGrid.Children.Add(serverMods);
		loadOrderCard.Child = loadOrderGrid;
		Grid.SetRow(loadOrderCard, 3);
		root.Children.Add(loadOrderCard);

		return root;
	}

	private FrameworkElement BuildIniTab()
	{
		StackPanel root = new StackPanel { Margin = new Thickness(0.0, 12.0, 0.0, 0.0) };
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
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 5.0, 0.0, 14.0)
		});
		Border countPanel = new Border
		{
			Background = Brush("#99091624"),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(7.0),
			Padding = new Thickness(12.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0),
			Child = new TextBlock
			{
				Text = _viewModel.IniSettings.Count + " server settings loaded from the configuration catalog",
				Foreground = Brushes.White,
				FontWeight = FontWeights.SemiBold,
				FontSize = 12.0
			}
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
			Foreground = Brush("#9fb8d6"),
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
		StackPanel stack = new StackPanel { Margin = new Thickness(0.0, 0.0, 12.0, 12.0) };
		stack.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});
		TextBox box = new TextBox
		{
			Background = Brush("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
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
		StackPanel stack = new StackPanel { Margin = new Thickness(0.0, 0.0, 12.0, 12.0) };
		stack.Children.Add(new TextBlock
		{
			Text = "Cluster Directory",
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});

		Grid picker = new Grid();
		picker.ColumnDefinitions.Add(new ColumnDefinition());
		picker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		picker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		TextBox box = new TextBox
		{
			Background = Brush("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
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
		StackPanel stack = new StackPanel { Margin = new Thickness(0.0, 0.0, 12.0, 12.0) };
		stack.Children.Add(new TextBlock
		{
			Text = "Map",
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});
		ComboBox box = new ComboBox
		{
			Background = Brush("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
			MinHeight = 34.0
		};
		ApplyDarkComboBox(box);
		box.ItemsSource = _viewModel.MapNames.Concat(new[] { CustomMapOption }).ToList();
		stack.Children.Add(box);
		StackPanel customStack = new StackPanel
		{
			Visibility = Visibility.Collapsed
		};
		customStack.Children.Add(new TextBlock
		{
			Text = CustomMapOption,
			Foreground = Brush("#9fb8d6"),
			FontSize = 11.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 5.0)
		});
		TextBox customBox = new TextBox
		{
			Background = Brush("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
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

	private void AddGameEditor(Grid grid, int row, int column)
	{
		StackPanel stack = new StackPanel { Margin = new Thickness(0.0, 0.0, 12.0, 12.0) };
		stack.Children.Add(new TextBlock
		{
			Text = "Game",
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});
		ComboBox box = new ComboBox
		{
			Background = Brush("#111d2a"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
			MinHeight = 34.0,
			DisplayMemberPath = "DisplayName",
			SelectedValuePath = "Id"
		};
		ApplyDarkComboBox(box);
		box.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("GameProfiles"));
		box.SetBinding(Selector.SelectedValueProperty, new Binding("SelectedServer.GameId")
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		stack.Children.Add(box);
		Grid.SetRow(stack, row);
		Grid.SetColumn(stack, column);
		grid.Children.Add(stack);
	}

	private static void ApplyDarkComboBox(ComboBox box)
	{
		box.Resources[SystemColors.WindowBrushKey] = Brush("#0b1422");
		box.Resources[SystemColors.ControlBrushKey] = Brush("#0b1422");
		box.Resources[SystemColors.ControlTextBrushKey] = Brushes.White;
		box.Resources[SystemColors.HighlightBrushKey] = Brush("#4658ff");
		box.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
		box.ItemContainerStyle = CreateComboBoxItemStyle();
		box.Template = CreateComboBoxTemplate();
		box.BorderThickness = new Thickness(1.0);
		box.Padding = new Thickness(10.0, 0.0, 34.0, 0.0);
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
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		content.SetValue(TextElement.ForegroundProperty, Brushes.White);
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
		scroll.SetValue(ScrollViewer.MaxHeightProperty, 240.0);
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
		Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#203249")));
		style.Triggers.Add(hover);
		return style;
	}

	private static Style CreateTabItemStyle()
	{
		Style style = new Style(typeof(TabItem));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#0b1422")));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#28445f")));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(13.0, 7.0, 13.0, 7.0)));
		style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0.0, 0.0, 6.0, 0.0)));

		ControlTemplate template = new ControlTemplate(typeof(TabItem));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6.0, 6.0, 0.0, 0.0));

		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.ContentSourceProperty, "Header");
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
		border.AppendChild(content);
		template.VisualTree = border;

		Trigger selected = new Trigger { Property = TabItem.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#4658ff")));
		selected.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#4658ff")));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		template.Triggers.Add(selected);
		Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#203249")));
		template.Triggers.Add(hover);

		style.Setters.Add(new Setter(Control.TemplateProperty, template));
		return style;
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
	private Button CreateIniButton(string text, string color, RoutedEventHandler click)
	{
		Button button = CreateButton(text, color);
		button.MinWidth = 170.0;
		button.Height = 36.0;
		button.Margin = new Thickness(0.0, 0.0, 8.0, 8.0);
		button.Click += click;
		return button;
	}

	private Border ModsCard(Thickness margin)
	{
		return new Border
		{
			Background = Brush("#dd101c31"),
			BorderBrush = Brush("#263f61"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(18.0),
			Margin = margin
		};
	}

	private Grid CardHeader(string badge, string title, string subtitle)
	{
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.Children.Add(Badge(badge));
		StackPanel text = new StackPanel
		{
			Margin = new Thickness(14.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		text.Children.Add(new TextBlock
		{
			Text = title,
			Foreground = Brushes.White,
			FontSize = 16.0,
			FontWeight = FontWeights.Bold
		});
		text.Children.Add(new TextBlock
		{
			Text = subtitle,
			Foreground = Brush("#bfd6f6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		});
		Grid.SetColumn(text, 1);
		grid.Children.Add(text);
		return grid;
	}

	private Grid CardHeaderWithButton(string badge, string title, string subtitle, string buttonText, RoutedEventHandler click)
	{
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.Children.Add(CardHeader(badge, title, subtitle));
		Button button = CreateButton(buttonText, "#203249");
		button.MinWidth = 150.0;
		button.Height = 38.0;
		button.VerticalAlignment = VerticalAlignment.Center;
		button.Click += click;
		Grid.SetColumn(button, 1);
		grid.Children.Add(button);
		return grid;
	}

	private Border Badge(string text)
	{
		return new Border
		{
			Width = 48.0,
			Height = 48.0,
			CornerRadius = new CornerRadius(8.0),
			Background = Brush("#273d78"),
			Child = new TextBlock
			{
				Text = text,
				Foreground = Brush("#dbeafe"),
				FontSize = 13.0,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
	}

	private StackPanel SectionTitle(string title, string subtitle)
	{
		StackPanel stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = title,
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.Bold
		});
		stack.Children.Add(new TextBlock
		{
			Text = subtitle,
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 5.0, 0.0, 0.0)
		});
		return stack;
	}

	private StackPanel ListHeader(string title, string subtitle)
	{
		StackPanel stack = new StackPanel
		{
			Margin = new Thickness(0.0, 8.0, 0.0, 6.0)
		};
		stack.Children.Add(new TextBlock
		{
			Text = title,
			Foreground = Brushes.White,
			FontSize = 14.0,
			FontWeight = FontWeights.Bold
		});
		stack.Children.Add(new TextBlock
		{
			Text = subtitle,
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		});
		return stack;
	}

	private Grid ListHeaderWithButton(string title, string subtitle, string buttonText, RoutedEventHandler click)
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 8.0, 0.0, 6.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.Children.Add(ListHeader(title, subtitle));
		Button button = CreateButton(buttonText, "#203249");
		button.MinWidth = 136.0;
		button.Height = 32.0;
		button.VerticalAlignment = VerticalAlignment.Center;
		button.Click += click;
		Grid.SetColumn(button, 1);
		grid.Children.Add(button);
		return grid;
	}

	private ListView ThemedListView(string itemsPath, string selectedPath)
	{
		ListView list = new ListView
		{
			Background = Brush("#aa081320"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#2a4061"),
			BorderThickness = new Thickness(1.0),
			ItemContainerStyle = CreateListViewItemStyle()
		};
		list.Resources.Add(typeof(GridViewColumnHeader), CreateGridHeaderStyle());
		list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemsPath));
		list.SetBinding(Selector.SelectedItemProperty, new Binding(selectedPath)
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		return list;
	}

	private GridViewColumn Column(string header, string path, double width)
	{
		return new GridViewColumn
		{
			Header = header,
			Width = width,
			CellTemplate = CreateCellTemplate(path)
		};
	}

	private static Style CreateGridHeaderStyle()
	{
		Style style = new Style(typeof(GridViewColumnHeader));
		ControlTemplate template = new ControlTemplate(typeof(GridViewColumnHeader));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new Thickness(0.0, 0.0, 1.0, 1.0));
		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		content.SetValue(FrameworkElement.MarginProperty, new Thickness(8.0, 0.0, 8.0, 0.0));
		border.AppendChild(content);
		template.VisualTree = border;
		style.Setters.Add(new Setter(Control.TemplateProperty, template));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#20324b")));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#2a4061")));
		style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10.0, 8.0, 10.0, 8.0)));
		return style;
	}

	private static Style CreateListViewItemStyle()
	{
		Style style = new Style(typeof(ListViewItem));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#0b1624")));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#15263a")));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8.0, 8.0, 8.0, 8.0)));
		Trigger selected = new Trigger
		{
			Property = ListBoxItem.IsSelectedProperty,
			Value = true
		};
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#2563eb")));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Triggers.Add(selected);
		return style;
	}

	private static DataTemplate CreateCellTemplate(string path)
	{
		FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock));
		text.SetBinding(TextBlock.TextProperty, new Binding(path));
		text.SetValue(TextBlock.ForegroundProperty, Brushes.White);
		text.SetValue(TextBlock.FontSizeProperty, 12.0);
		text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
		text.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
		text.SetValue(FrameworkElement.MarginProperty, new Thickness(8.0, 0.0, 8.0, 0.0));
		return new DataTemplate { VisualTree = text };
	}

	private Button CommandButton(string text, string commandPath, string color, int column = -1)
	{
		Button button = CreateButton(text, color);
		button.MinWidth = 92.0;
		button.Height = 42.0;
		button.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		if (column >= 0)
		{
			Grid.SetColumn(button, column);
		}
		return button;
	}

	private Button CreateButton(string text, string color)
	{
		Button button = new Button
		{
			Content = text,
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0)
		};
		button.Template = CreateButtonTemplate();
		return button;
	}

	private static ControlTemplate CreateButtonTemplate()
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

	private void EditIniSettings()
	{
		_viewModel.ReloadIniSettingsFromSelectedServerFiles();
		IniEditorWindow window = new IniEditorWindow(_viewModel.IniSettings, async delegate
		{
			await _viewModel.SaveIniSettingsAsync();
			if (_viewModel.SelectedServer != null)
			{
				_subtitle.Text = _viewModel.SelectedServer.Name;
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

	private async void OpenInstalledModsWindow()
	{
		if (_viewModel.SelectedServer == null)
		{
			MessageBox.Show("Select a server first.", "Installed mods", MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		await _viewModel.EnrichSelectedServerModsAsync();
		InstalledModsWindow window = new InstalledModsWindow(_viewModel.SelectedServer.Name, _viewModel.SelectedServerMods)
		{
			Owner = Window.GetWindow(this)
		};
		window.Show();
	}

	private void OpenCurseForgeBrowser()
	{
		CurseForgeBrowserWindow window = new CurseForgeBrowserWindow("https://www.curseforge.com/ark-survival-ascended/mods", _viewModel)
		{
			Owner = Window.GetWindow(this)
		};
		window.Show();
	}

	private static SolidColorBrush Brush(string color)
	{
		SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		brush.Freeze();
		return brush;
	}
}
