using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ServerManager.Models;
using ServerManager.ViewModels;
using Microsoft.Win32;

namespace ServerManager.Views;

public class DashboardView : UserControl, IComponentConnector, IStyleConnector
{
	private const string CurseForgeBrowseUrl = "https://www.curseforge.com/ark-survival-ascended/mods";

	private static readonly HttpClient PublicIpClient = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(4.0)
	};

	private static string? _cachedPublicIpAddress;

	private static DateTime _cachedPublicIpAddressAt;

	internal ColumnDefinition ServersColumn;

	internal ColumnDefinition ConfigureColumn;

	internal Border ConfigureServerPanel;

	internal TabControl ConfigureTabs;

	internal TabItem SettingsTab;

	private bool _consoleTabModernized;

	private bool _iniTabModernized;

	private bool _contentLoaded;

	public DashboardView()
	{
		InitializeComponent();
		Loaded += delegate
		{
			ModernizeConsoleTab();
			ModernizeIniTab();
		};
	}

	private async void AddExistingServer_Click(object sender, RoutedEventArgs e)
	{
		if (!(base.DataContext is DashboardViewModel dashboardViewModel))
		{
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Select an existing ASA install folder or ShooterGame Saved folder",
			InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
			CheckFileExists = false,
			CheckPathExists = true,
			FileName = "Select this folder",
			Filter = "Folders|*.folder",
			ValidateNames = false
		};
		if (openFileDialog.ShowDialog(Window.GetWindow((DependencyObject)(object)this)).GetValueOrDefault())
		{
			string directoryName = Path.GetDirectoryName(openFileDialog.FileName);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				await dashboardViewModel.AddExistingServerAsync(directoryName);
			}
		}
	}

	private void EditIniSettings_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is DashboardViewModel dashboardViewModel)
		{
			dashboardViewModel.ReloadIniSettingsFromSelectedServerFiles();
			IniEditorWindow iniEditorWindow = new IniEditorWindow(dashboardViewModel.IniSettings, async delegate
			{
				await dashboardViewModel.SaveIniSettingsAsync();
			});
			iniEditorWindow.Owner = Window.GetWindow((DependencyObject)(object)this);
			iniEditorWindow.ShowDialog();
		}
	}

	private void OpenCurseForgeWebsite_Click(object sender, RoutedEventArgs e)
	{
		OpenCurseForgeBrowser(CurseForgeBrowseUrl);
	}

	private void ConfigureServer_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is DashboardViewModel dashboardViewModel && sender is FrameworkElement { Tag: ServerInstance tag })
		{
			if (ConfigureServerPanel.Visibility == Visibility.Visible && dashboardViewModel.SelectedServer?.Id == tag.Id)
			{
				CloseConfigurePanel();
				return;
			}
			dashboardViewModel.SelectServerCommand.Execute(tag);
			OpenConfigurePanel();
			ConfigureTabs.SelectedItem = SettingsTab;
			ConfigureServerPanel.BringIntoView();
		}
	}

	private void ConsoleServer_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is DashboardViewModel dashboardViewModel && sender is FrameworkElement { Tag: ServerInstance tag })
		{
			dashboardViewModel.SelectServerCommand.Execute(tag);
			OpenConfigurePanel();
			TabItem? consoleTab = FindConfigureTab("Console");
			if (consoleTab != null)
			{
				ConfigureTabs.SelectedItem = consoleTab;
			}
			ConfigureServerPanel.BringIntoView();
		}
	}

	private TabItem? FindConfigureTab(string header)
	{
		foreach (object item in ConfigureTabs.Items)
		{
			if (item is TabItem tabItem && string.Equals(tabItem.Header?.ToString(), header, StringComparison.OrdinalIgnoreCase))
			{
				return tabItem;
			}
		}
		return null;
	}

	private void ModernizeConsoleTab()
	{
		if (_consoleTabModernized || base.DataContext is not DashboardViewModel dashboardViewModel)
		{
			return;
		}
		TabItem? consoleTab = FindConfigureTab("Console");
		if (consoleTab == null)
		{
			return;
		}
		consoleTab.Content = BuildRconConsoleSurface(dashboardViewModel.ServerConsole);
		_consoleTabModernized = true;
	}

	private void ModernizeIniTab()
	{
		if (_iniTabModernized || base.DataContext is not DashboardViewModel dashboardViewModel)
		{
			return;
		}
		TabItem? iniTab = FindConfigureTab("INI");
		if (iniTab == null)
		{
			return;
		}
		iniTab.Content = BuildIniTabSurface(dashboardViewModel);
		_iniTabModernized = true;
	}

	private FrameworkElement BuildIniTabSurface(DashboardViewModel dashboardViewModel)
	{
		StackPanel root = new StackPanel
		{
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
		};
		root.Children.Add(new TextBlock
		{
			Text = "INI Settings",
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.SemiBold
		});
		root.Children.Add(new TextBlock
		{
			Text = "Edit settings in the catalog, or import/export the server's Game.ini and GameUserSettings.ini files.",
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 6.0, 0.0, 12.0)
		});
		Border statusPanel = new Border
		{
			Background = BrushFrom("#101923"),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
		statusPanel.Child = new TextBlock
		{
			Text = dashboardViewModel.IniSettings.Count + " server settings loaded from the configuration catalog",
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 12.0
		};
		root.Children.Add(statusPanel);

		WrapPanel actions = new WrapPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 18.0)
		};
		actions.Children.Add(CreateIniActionButton("Edit INI Settings", delegate { EditIniSettings_Click(this, new RoutedEventArgs()); }, "#5d7cff"));
		actions.Children.Add(CreateIniActionButton("Import Game.ini", async delegate { await ImportIniFileAsync("Game.ini"); }, "#2563eb"));
		actions.Children.Add(CreateIniActionButton("Export Game.ini", delegate { ExportIniFile("Game.ini"); }, "#142235"));
		actions.Children.Add(CreateIniActionButton("Import GameUserSettings.ini", async delegate { await ImportIniFileAsync("GameUserSettings.ini"); }, "#2563eb"));
		actions.Children.Add(CreateIniActionButton("Export GameUserSettings.ini", delegate { ExportIniFile("GameUserSettings.ini"); }, "#142235"));
		root.Children.Add(actions);

		TextBlock configPath = new TextBlock
		{
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap
		};
		configPath.SetBinding(TextBlock.TextProperty, new Binding("SelectedServer.InstallDirectory")
		{
			StringFormat = "Server config folder: {0}\\ShooterGame\\Saved\\Config\\WindowsServer",
			TargetNullValue = "Select a server to import or export INI files."
		});
		root.Children.Add(configPath);
		return root;
	}

	private Button CreateIniActionButton(string text, RoutedEventHandler click, string color)
	{
		Button button = new Button
		{
			Content = text,
			Style = CreateConsoleButtonStyle(),
			Background = BrushFrom(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			MinWidth = 160.0,
			Height = 36.0,
			Margin = new Thickness(0.0, 0.0, 8.0, 8.0),
			Padding = new Thickness(14.0, 0.0, 14.0, 0.0),
			FontWeight = FontWeights.SemiBold
		};
		button.Click += click;
		return button;
	}

	private async Task ImportIniFileAsync(string fileName)
	{
		if (!TryGetSelectedIniPath(fileName, out string destinationPath))
		{
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Import " + fileName,
			Filter = fileName + "|" + fileName + "|INI files|*.ini|All files|*.*",
			CheckFileExists = true
		};
		if (!openFileDialog.ShowDialog(Window.GetWindow((DependencyObject)(object)this)).GetValueOrDefault())
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
		if (!string.Equals(Path.GetFullPath(openFileDialog.FileName), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
		{
			File.Copy(openFileDialog.FileName, destinationPath, overwrite: true);
		}
		int updatedCount = 0;
		if (base.DataContext is DashboardViewModel dashboardViewModel)
		{
			updatedCount = await dashboardViewModel.ReloadImportedIniSettingsAsync(fileName);
		}
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
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "Export " + fileName,
			FileName = fileName,
			Filter = fileName + "|" + fileName + "|INI files|*.ini|All files|*.*",
			OverwritePrompt = true
		};
		if (!saveFileDialog.ShowDialog(Window.GetWindow((DependencyObject)(object)this)).GetValueOrDefault())
		{
			return;
		}
		File.Copy(sourcePath, saveFileDialog.FileName, overwrite: true);
		MessageBox.Show(fileName + " exported.", "INI export", MessageBoxButton.OK, MessageBoxImage.Information);
	}

	private bool TryGetSelectedIniPath(string fileName, out string path)
	{
		path = string.Empty;
		if (base.DataContext is not DashboardViewModel { SelectedServer: not null } dashboardViewModel)
		{
			MessageBox.Show("Select a server first.", "INI files", MessageBoxButton.OK, MessageBoxImage.Information);
			return false;
		}
		if (string.IsNullOrWhiteSpace(dashboardViewModel.SelectedServer.InstallDirectory))
		{
			MessageBox.Show("Set an install directory for this server first.", "INI files", MessageBoxButton.OK, MessageBoxImage.Information);
			return false;
		}
		path = Path.Combine(dashboardViewModel.SelectedServer.InstallDirectory, "ShooterGame", "Saved", "Config", "WindowsServer", fileName);
		return true;
	}

	private FrameworkElement BuildRconConsoleSurface(ConsoleViewModel consoleViewModel)
	{
		Grid root = new Grid
		{
			DataContext = consoleViewModel,
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		Grid header = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		StackPanel titleStack = new StackPanel();
		titleStack.Children.Add(new TextBlock
		{
			Text = "RCON Console",
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.SemiBold
		});
		TextBlock selectedServer = new TextBlock
		{
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
		};
		selectedServer.SetBinding(TextBlock.TextProperty, new Binding("SelectedServer.Name") { StringFormat = "Selected server: {0}", TargetNullValue = "Selected server: none" });
		titleStack.Children.Add(selectedServer);
		header.Children.Add(titleStack);
		Border statusBadge = new Border
		{
			Background = BrushFrom("#142235"),
			BorderBrush = BrushFrom("#2a3d55"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(12.0, 7.0, 12.0, 7.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock status = new TextBlock
		{
			Foreground = BrushFrom("#d8e8ff"),
			FontWeight = FontWeights.SemiBold
		};
		status.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
		statusBadge.Child = status;
		Grid.SetColumn(statusBadge, 1);
		header.Children.Add(statusBadge);
		root.Children.Add(header);

		Border consoleFrame = new Border
		{
			Background = BrushFrom("#071019"),
			BorderBrush = BrushFrom("#263a51"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(10.0)
		};
		ListBox lines = new ListBox
		{
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			Foreground = BrushFrom("#d6e6f8"),
			FontFamily = new FontFamily("Consolas"),
			FontSize = 12.0
		};
		lines.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ConsoleLines"));
		consoleFrame.Child = lines;
		Grid.SetRow(consoleFrame, 1);
		root.Children.Add(consoleFrame);

		Grid commandBar = new Grid
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		commandBar.ColumnDefinitions.Add(new ColumnDefinition());
		commandBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		commandBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		commandBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		commandBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		commandBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		TextBox commandBox = new TextBox
		{
			Background = BrushFrom("#101b28"),
			Foreground = Brushes.White,
			BorderBrush = BrushFrom("#2a3d55"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(10.0, 8.0, 10.0, 8.0),
			FontFamily = new FontFamily("Consolas"),
			FontSize = 12.0
		};
		commandBox.SetBinding(TextBox.TextProperty, new Binding("CommandText")
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		commandBox.KeyDown += delegate(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.Enter && consoleViewModel.SendCommandCommand.CanExecute(null))
			{
				consoleViewModel.SendCommandCommand.Execute(null);
				args.Handled = true;
			}
		};
		commandBar.Children.Add(commandBox);
		commandBar.Children.Add(CreateConsoleCommandButton("Send", "SendCommandCommand", 1));
		commandBar.Children.Add(CreateConsoleCommandButton("Reconnect", "RefreshConnectionCommand", 2));
		commandBar.Children.Add(CreateConsoleCommandButton("Connect", "ConnectCommand", 3));
		commandBar.Children.Add(CreateConsoleCommandButton("Disconnect", "DisconnectCommand", 4));
		commandBar.Children.Add(CreateConsoleCommandButton("Clear", "ClearConsoleCommand", 5));
		Grid.SetRow(commandBar, 2);
		root.Children.Add(commandBar);
		return root;
	}

	private Button CreateConsoleCommandButton(string text, string commandPath, int column)
	{
		Button button = new Button
		{
			Content = text,
			Style = CreateConsoleButtonStyle(),
			Background = BrushFrom(text == "Send" ? "#5d7cff" : "#142235"),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			MinWidth = 86.0,
			Height = 36.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			FontWeight = FontWeights.SemiBold
		};
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		Grid.SetColumn(button, column);
		return button;
	}

	private Style CreateConsoleButtonStyle()
	{
		Style style = new Style(typeof(Button));
		ControlTemplate template = new ControlTemplate(typeof(Button));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6.0));
		border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		border.AppendChild(content);
		template.VisualTree = border;
		style.Setters.Add(new Setter(Control.TemplateProperty, template));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
		Trigger hovered = new Trigger
		{
			Property = UIElement.IsMouseOverProperty,
			Value = true
		};
		hovered.Setters.Add(new Setter(Control.BackgroundProperty, BrushFrom("#2563eb")));
		style.Triggers.Add(hovered);
		Trigger disabled = new Trigger
		{
			Property = UIElement.IsEnabledProperty,
			Value = false
		};
		disabled.Setters.Add(new Setter(Control.BackgroundProperty, BrushFrom("#101b28")));
		disabled.Setters.Add(new Setter(Control.ForegroundProperty, BrushFrom("#64748b")));
		style.Triggers.Add(disabled);
		return style;
	}

	private void AddConsoleButtonNear(Button configureButton)
	{
		ModernizeServerCard(configureButton);
		if (configureButton.Parent is not Panel panel || panel.Children.OfType<Button>().Any((Button button) => string.Equals(button.Content?.ToString(), "Console", StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		configureButton.Content = "Configure";
		configureButton.ToolTip = "Configure server";
		CompactServerCardButtons(panel);
		Button consoleButton = new Button
		{
			Content = "Console",
			Style = configureButton.Style,
			Margin = new Thickness(2.0, configureButton.Margin.Top, 2.0, configureButton.Margin.Bottom),
			MinWidth = 0.0,
			Width = 62.0,
			Height = configureButton.Height,
			FontSize = 11.0,
			ToolTip = "Show server console"
		};
		BindingOperations.SetBinding(consoleButton, FrameworkElement.TagProperty, new Binding("Tag")
		{
			Source = configureButton
		});
		consoleButton.Click += ConsoleServer_Click;
		panel.Children.Add(consoleButton);
	}

	private static void CompactServerCardButtons(Panel panel)
	{
		foreach (Button button in panel.Children.OfType<Button>())
		{
			string text = button.Content?.ToString() ?? string.Empty;
			button.Margin = new Thickness(2.0, button.Margin.Top, 2.0, button.Margin.Bottom);
			button.MinWidth = 0.0;
			button.FontSize = 11.0;
			button.Width = text.Equals("Configure", StringComparison.OrdinalIgnoreCase) ? 72.0 : 58.0;
			if (text.Equals("Install / Update", StringComparison.OrdinalIgnoreCase) || text.Equals("Update", StringComparison.OrdinalIgnoreCase))
			{
				button.Content = "Update";
				button.Width = 62.0;
			}
		}
	}

	private void ModernizeServerCard(Button sourceButton)
	{
		if (sourceButton.Tag is not ServerInstance server)
		{
			return;
		}
		Border? card = FindServerCardBorder(sourceButton, server);
		if (card == null || card.Child is FrameworkElement { Tag: "ModernServerCard" })
		{
			return;
		}
		card.Width = 540.0;
		card.MinWidth = 520.0;
		card.Height = 210.0;
		card.Padding = new Thickness(0.0);
		card.CornerRadius = new CornerRadius(7.0);
		card.BorderBrush = BrushFrom("#26384a");
		card.BorderThickness = new Thickness(1.0);
		card.Background = BrushFrom("#101923");
		card.Child = BuildModernServerCard(server, sourceButton.Style);
	}

	private Border? FindServerCardBorder(DependencyObject start, ServerInstance server)
	{
		DependencyObject? current = start;
		Border? fallback = null;
		while (current != null)
		{
			if (current is Border border)
			{
				fallback ??= border;
				if (Equals((border as FrameworkElement)?.DataContext, server))
				{
					return border;
				}
			}
			current = VisualTreeHelper.GetParent(current);
		}
		return fallback;
	}

	private FrameworkElement BuildModernServerCard(ServerInstance server, Style? buttonStyle)
	{
		Grid root = new Grid
		{
			Tag = "ModernServerCard",
			Margin = new Thickness(0.0),
			ClipToBounds = true
		};
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(82.0) });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(74.0) });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		Border imagePanel = new Border
		{
			CornerRadius = new CornerRadius(7.0, 7.0, 0.0, 0.0),
			Background = new ImageBrush
			{
				ImageSource = new BitmapImage(GetMapImageUri(server.MapName)),
				Stretch = Stretch.UniformToFill,
				Opacity = 0.62
			}
		};
		root.Children.Add(imagePanel);

		Border headerShade = new Border
		{
			Background = new LinearGradientBrush(ColorFrom("#99000000"), ColorFrom("#33000000"), 0.0)
		};
		root.Children.Add(headerShade);

		Border statusStrip = new Border
		{
			Width = 8.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = BrushFrom("#28d839")
		};
		Grid.SetRowSpan(statusStrip, 3);
		root.Children.Add(statusStrip);

		Grid header = new Grid
		{
			Margin = new Thickness(24.0, 10.0, 10.0, 8.0)
		};
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		StackPanel titleStack = new StackPanel();
		TextBlock title = new TextBlock
		{
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.SemiBold
		};
		title.SetBinding(TextBlock.TextProperty, new Binding("Name") { Source = server, StringFormat = "Session: {0}" });
		TextBlock mapName = new TextBlock
		{
			Foreground = BrushFrom("#d8e8ff"),
			FontSize = 12.0,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
		};
		mapName.SetBinding(TextBlock.TextProperty, new Binding("MapName") { Source = server, StringFormat = "Map: {0}" });
		TextBlock endpoint = new TextBlock
		{
			Foreground = Brushes.White,
			FontSize = 12.0,
			Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
		};
		endpoint.Text = "Public: checking...";
		_ = SetPublicEndpointAsync(endpoint, server.GamePort);
		titleStack.Children.Add(title);
		titleStack.Children.Add(mapName);
		titleStack.Children.Add(endpoint);
		header.Children.Add(titleStack);
		Border statusBadge = new Border
		{
			Background = BrushFrom("#26bf35"),
			CornerRadius = new CornerRadius(18.0),
			Padding = new Thickness(12.0, 5.0, 12.0, 5.0),
			VerticalAlignment = VerticalAlignment.Top
		};
		TextBlock statusText = new TextBlock
		{
			Foreground = Brushes.White,
			FontWeight = FontWeights.Bold,
			FontSize = 14.0
		};
		statusText.SetBinding(TextBlock.TextProperty, new Binding("StatusText") { Source = server });
		statusBadge.Child = statusText;
		Grid.SetColumn(statusBadge, 1);
		header.Children.Add(statusBadge);
		root.Children.Add(header);

		Grid metrics = new Grid
		{
			Margin = new Thickness(24.0, 10.0, 10.0, 10.0)
		};
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		Grid.SetRow(metrics, 1);
		metrics.Children.Add(CreateMetricTile("CPU", "CpuUsageText", server, 0));
		metrics.Children.Add(CreateMetricTile("Memory", "MemoryUsageText", server, 1));
		metrics.Children.Add(CreateUsersTile(server, 2));
		root.Children.Add(metrics);

		Grid footer = new Grid
		{
			Margin = new Thickness(24.0, 0.0, 10.0, 10.0)
		};
		footer.ColumnDefinitions.Add(new ColumnDefinition());
		footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		Grid.SetRow(footer, 2);
		TextBlock mapText = new TextBlock
		{
			Foreground = BrushFrom("#c9d9ee"),
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		mapText.SetBinding(TextBlock.TextProperty, new Binding("MapName") { Source = server });
		footer.Children.Add(mapText);
		StackPanel actions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		actions.Children.Add(CreateCardButton("Configure", "#5d7cff", buttonStyle, delegate { OpenSettingsFor(server); }));
		actions.Children.Add(CreateCardButton("Start", "#22c55e", buttonStyle, delegate { RunTileCommand("Start", server); }));
		actions.Children.Add(CreateCardButton("Stop", "#c8001f", buttonStyle, delegate { RunTileCommand("Stop", server); }));
		actions.Children.Add(CreateCardButton("Update", "#3b82f6", buttonStyle, delegate { RunTileCommand("Update", server); }));
		Grid.SetColumn(actions, 1);
		footer.Children.Add(actions);
		root.Children.Add(footer);
		return root;
	}

	private Border CreateMetricTile(string label, string valuePath, ServerInstance server, int column)
	{
		Border tile = CreateMetricShell(column);
		StackPanel stack = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock labelBlock = new TextBlock
		{
			Text = label,
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 14.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		TextBlock valueBlock = new TextBlock
		{
			Foreground = Brushes.White,
			FontSize = 13.0,
			FontWeight = FontWeights.SemiBold,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		valueBlock.SetBinding(TextBlock.TextProperty, new Binding(valuePath) { Source = server });
		stack.Children.Add(labelBlock);
		stack.Children.Add(valueBlock);
		tile.Child = stack;
		return tile;
	}

	private Border CreateUsersTile(ServerInstance server, int column)
	{
		Border tile = CreateMetricShell(column);
		StackPanel stack = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		stack.Children.Add(new TextBlock
		{
			Text = "Users",
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 14.0,
			HorizontalAlignment = HorizontalAlignment.Center
		});
		TextBlock valueBlock = new TextBlock
		{
			Foreground = Brushes.White,
			FontSize = 13.0,
			FontWeight = FontWeights.SemiBold,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		valueBlock.SetBinding(TextBlock.TextProperty, new Binding("PlayerCount") { Source = server, StringFormat = "{0}/" + server.MaxPlayers });
		stack.Children.Add(valueBlock);
		tile.Child = stack;
		return tile;
	}

	private Border CreateMetricShell(int column)
	{
		Border tile = new Border
		{
			Background = BrushFrom("#d9000000"),
			CornerRadius = new CornerRadius(7.0),
			Margin = new Thickness(4.0),
			Padding = new Thickness(10.0, 8.0, 10.0, 8.0),
			MinHeight = 56.0,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		Grid.SetColumn(tile, column);
		return tile;
	}

	private static async Task SetPublicEndpointAsync(TextBlock endpoint, int port)
	{
		string text = await GetPublicEndpointAsync(port);
		if (endpoint.Dispatcher.CheckAccess())
		{
			endpoint.Text = text;
		}
		else
		{
			await endpoint.Dispatcher.InvokeAsync(delegate
			{
				endpoint.Text = text;
			});
		}
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
			return "Public IP unavailable (" + GetLocalEndpoint(port) + ")";
		}
	}

	private static string GetLocalEndpoint(int port)
	{
		try
		{
			string? address = Dns.GetHostEntry(Dns.GetHostName())
				.AddressList
				.FirstOrDefault(static candidate => candidate.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(candidate))
				?.ToString();
			return (string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address) + ":" + port;
		}
		catch
		{
			return "127.0.0.1:" + port;
		}
	}

	private Button CreateCardButton(string text, string color, Style? style, RoutedEventHandler click)
	{
		Button button = new Button
		{
			Content = text,
			Style = style,
			Background = BrushFrom(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			MinWidth = 0.0,
			Width = text == "Configure" ? 76.0 : 60.0,
			Height = 30.0,
			Margin = new Thickness(3.0, 0.0, 0.0, 0.0),
			FontSize = 11.0,
			FontWeight = FontWeights.SemiBold
		};
		button.Click += click;
		return button;
	}

	private void OpenSettingsFor(ServerInstance server)
	{
		if (base.DataContext is DashboardViewModel dashboardViewModel)
		{
			dashboardViewModel.SelectServerCommand.Execute(server);
			OpenConfigurePanel();
			ConfigureTabs.SelectedItem = SettingsTab;
			ConfigureServerPanel.BringIntoView();
		}
	}

	private void OpenConsoleFor(ServerInstance server)
	{
		if (base.DataContext is DashboardViewModel dashboardViewModel)
		{
			dashboardViewModel.SelectServerCommand.Execute(server);
			OpenConfigurePanel();
			TabItem? consoleTab = FindConfigureTab("Console");
			if (consoleTab != null)
			{
				ConfigureTabs.SelectedItem = consoleTab;
			}
			ConfigureServerPanel.BringIntoView();
		}
	}

	private void RunTileCommand(string command, ServerInstance server)
	{
		if (base.DataContext is not DashboardViewModel dashboardViewModel)
		{
			return;
		}
		switch (command)
		{
		case "Start":
			dashboardViewModel.StartServerTileCommand.Execute(server);
			break;
		case "Stop":
			dashboardViewModel.StopServerTileCommand.Execute(server);
			break;
		case "Update":
			dashboardViewModel.InstallServerTileCommand.Execute(server);
			break;
		}
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

	private void OpenConfigurePanel()
	{
		ConfigureServerPanel.Visibility = Visibility.Visible;
		ConfigureColumn.Width = new GridLength(1.0, GridUnitType.Star);
	}

	private void CloseConfigurePanel()
	{
		ConfigureServerPanel.Visibility = Visibility.Collapsed;
		ConfigureColumn.Width = new GridLength(0.0);
	}

	private void BrowseInstallDirectory_Click(object sender, RoutedEventArgs e)
	{
		if (!(base.DataContext is DashboardViewModel { SelectedServer: not null } dashboardViewModel))
		{
			return;
		}
		string initialDirectory = (Directory.Exists(dashboardViewModel.SelectedServer.InstallDirectory) ? dashboardViewModel.SelectedServer.InstallDirectory : Environment.GetFolderPath(Environment.SpecialFolder.Personal));
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Select ASA server install folder",
			InitialDirectory = initialDirectory,
			CheckFileExists = false,
			CheckPathExists = true,
			FileName = "Select this folder",
			Filter = "Folders|*.folder",
			ValidateNames = false
		};
		if (openFileDialog.ShowDialog(Window.GetWindow((DependencyObject)(object)this)).GetValueOrDefault())
		{
			string directoryName = Path.GetDirectoryName(openFileDialog.FileName);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				dashboardViewModel.SelectedServer.InstallDirectory = directoryName;
			}
		}
	}

	private void BrowseClusterDirectory_Click(object sender, RoutedEventArgs e)
	{
		if (!(base.DataContext is DashboardViewModel { SelectedServer: not null } dashboardViewModel))
		{
			return;
		}
		string initialDirectory = (Directory.Exists(dashboardViewModel.SelectedServer.ClusterDirectory) ? dashboardViewModel.SelectedServer.ClusterDirectory : Environment.GetFolderPath(Environment.SpecialFolder.Personal));
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Select shared ASA cluster folder",
			InitialDirectory = initialDirectory,
			CheckFileExists = false,
			CheckPathExists = true,
			FileName = "Select this folder",
			Filter = "Folders|*.folder",
			ValidateNames = false
		};
		if (openFileDialog.ShowDialog(Window.GetWindow((DependencyObject)(object)this)).GetValueOrDefault())
		{
			string directoryName = Path.GetDirectoryName(openFileDialog.FileName);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				dashboardViewModel.SelectedServer.ClusterDirectory = directoryName;
			}
		}
	}

	private void OpenCurseForgeBrowser(string url)
	{
		if (base.DataContext is DashboardViewModel dashboardViewModel)
		{
			CurseForgeBrowserWindow window = new CurseForgeBrowserWindow(url, dashboardViewModel)
			{
				Owner = Window.GetWindow((DependencyObject)(object)this)
			};
			window.Show();
			return;
		}
		Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/ServerManager;component/views/dashboardview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Button)target).Click += AddExistingServer_Click;
			break;
		case 2:
			ServersColumn = (ColumnDefinition)target;
			break;
		case 3:
			ConfigureColumn = (ColumnDefinition)target;
			break;
		case 5:
			ConfigureServerPanel = (Border)target;
			break;
		case 6:
			ConfigureTabs = (TabControl)target;
			break;
		case 7:
			SettingsTab = (TabItem)target;
			break;
		case 8:
			((Button)target).Click += BrowseInstallDirectory_Click;
			break;
		case 9:
			((Button)target).Click += BrowseClusterDirectory_Click;
			break;
		case 10:
			((Button)target).Click += OpenCurseForgeWebsite_Click;
			break;
		case 11:
			((Button)target).Click += EditIniSettings_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 4)
		{
			Button configureButton = (Button)target;
			configureButton.Click += ConfigureServer_Click;
			configureButton.Loaded += delegate
			{
				AddConsoleButtonNear(configureButton);
			};
		}
	}
}
