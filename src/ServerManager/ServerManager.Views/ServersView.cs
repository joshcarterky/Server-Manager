using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using ServerManager.Models;
using ServerManager.ViewModels;

namespace ServerManager.Views;

public class ServersView : UserControl
{
	private static readonly HttpClient PublicIpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(4.0) };
	private static string? _cachedPublicIpAddress;
	private static DateTime _cachedPublicIpAddressAt;

	private ServersViewModel? _viewModel;
	private readonly DashboardViewModel? _dashboardViewModel;
	private WrapPanel? _serverCards;
	private TextBox? _searchBox;
	private ComboBox? _statusFilter;
	private ConfigureServerPanel? _configurePanel;

	public ServersView()
	{
		Loaded += OnLoaded;
	}

	public ServersView(ServersViewModel viewModel)
		: this()
	{
		AttachViewModel(viewModel);
	}

	public ServersView(ServersViewModel viewModel, DashboardViewModel dashboardViewModel)
		: this()
	{
		_dashboardViewModel = dashboardViewModel;
		_dashboardViewModel.ConfigureRequested += OnConfigureRequested;
		Unloaded += delegate
		{
			_dashboardViewModel.ConfigureRequested -= OnConfigureRequested;
		};
		AttachViewModel(viewModel);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_viewModel == null && DataContext is ServersViewModel viewModel)
		{
			AttachViewModel(viewModel);
		}
	}

	private void AttachViewModel(ServersViewModel viewModel)
	{
		if (ReferenceEquals(_viewModel, viewModel))
		{
			return;
		}
		if (_viewModel != null)
		{
			_viewModel.Servers.CollectionChanged -= ServersChanged;
			foreach (ServerInstance server in _viewModel.Servers)
			{
				server.PropertyChanged -= ServerPropertyChanged;
			}
		}
		_viewModel = viewModel;
		DataContext = viewModel;
		viewModel.Servers.CollectionChanged += ServersChanged;
		foreach (ServerInstance server in viewModel.Servers)
		{
			server.PropertyChanged += ServerPropertyChanged;
		}
		Content = BuildView();
		RebuildServers();
	}

	private void ServersChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems != null)
		{
			foreach (ServerInstance server in e.OldItems.OfType<ServerInstance>())
			{
				server.PropertyChanged -= ServerPropertyChanged;
			}
		}
		if (e.NewItems != null)
		{
			foreach (ServerInstance server in e.NewItems.OfType<ServerInstance>())
			{
				server.PropertyChanged += ServerPropertyChanged;
			}
		}
		RebuildServers();
	}

	private void ServerPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		RebuildServers();
	}

	private UIElement BuildView()
	{
		Grid root = new Grid
		{
			Background = new LinearGradientBrush(ColorFrom("#06111f"), ColorFrom("#0b1430"), 45.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		root.Children.Add(BuildHeader());
		Border body = BuildBody();
		Grid.SetRow(body, 1);
		root.Children.Add(body);
		return root;
	}

	private UIElement BuildHeader()
	{
		Grid header = new Grid
		{
			Margin = new Thickness(28.0, 28.0, 28.0, 20.0)
		};
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel titleRow = new StackPanel { Orientation = Orientation.Horizontal };
		titleRow.Children.Add(new TextBlock
		{
			Text = "S",
			Foreground = Brush("#3b5cff"),
			FontSize = 34.0,
			FontWeight = FontWeights.Black,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 0.0, 18.0, 0.0)
		});
		StackPanel titleStack = new StackPanel();
		titleStack.Children.Add(new TextBlock
		{
			Text = "Servers",
			Foreground = Brushes.White,
			FontSize = 32.0,
			FontWeight = FontWeights.Bold
		});
		titleStack.Children.Add(new TextBlock
		{
			Text = "Add, manage, and monitor your dedicated game servers.",
			Foreground = Brush("#9fb8d6"),
			FontSize = 14.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		});
		titleRow.Children.Add(titleStack);
		header.Children.Add(titleRow);

		return header;
	}

	private Border BuildBody()
	{
		Border panel = new Border
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 28.0),
			Background = Brush("#aa0d1828"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0)
		};
		Grid layout = new Grid();
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		panel.Child = layout;

		Grid tools = new Grid
		{
			Background = Brush("#551a2446"),
			MinHeight = 88.0
		};
		tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24.0) });
		tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380.0) });
		tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20.0) });
		tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280.0) });
		tools.ColumnDefinitions.Add(new ColumnDefinition());
		tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24.0) });
		layout.Children.Add(tools);

		_searchBox = SearchBox();
		Grid.SetColumn(_searchBox, 1);
		tools.Children.Add(_searchBox);

		_statusFilter = StatusFilter();
		Grid.SetColumn(_statusFilter, 3);
		tools.Children.Add(_statusFilter);

		tools.Children.Add(ViewToggle());
		Grid.SetColumn(tools.Children[^1], 5);

		ScrollViewer scroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(24.0, 28.0, 24.0, 24.0)
		};
		_serverCards = new WrapPanel { Orientation = Orientation.Horizontal };
		scroll.Content = _serverCards;
		Grid.SetRow(scroll, 1);
		layout.Children.Add(scroll);

		if (_dashboardViewModel != null)
		{
			_configurePanel = new ConfigureServerPanel(_dashboardViewModel);
			Grid.SetRow(_configurePanel, 2);
			layout.Children.Add(_configurePanel);
		}
		return panel;
	}

	private TextBox SearchBox()
	{
		TextBox box = new TextBox
		{
			Height = 44.0,
			Padding = new Thickness(16.0, 0.0, 16.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center,
			Foreground = Brushes.White,
			Background = Brush("#0b1422"),
			BorderBrush = Brush("#253a55"),
			BorderThickness = new Thickness(1.0),
			FontSize = 14.0,
			ToolTip = "Search servers"
		};
		box.TextChanged += delegate { RebuildServers(); };
		return box;
	}

	private ComboBox StatusFilter()
	{
		ComboBox combo = new ComboBox
		{
			Height = 44.0,
			Foreground = Brushes.White,
			Background = Brush("#0b1422"),
			BorderBrush = Brush("#253a55"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(14.0, 0.0, 14.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center
		};
		ApplyDarkComboBox(combo);
		combo.Items.Add("All Statuses");
		combo.Items.Add("Online");
		combo.Items.Add("Offline");
		combo.SelectedIndex = 0;
		combo.SelectionChanged += delegate { RebuildServers(); };
		return combo;
	}

	private static void ApplyDarkComboBox(ComboBox combo)
	{
		combo.Resources[SystemColors.WindowBrushKey] = Brush("#0b1422");
		combo.Resources[SystemColors.ControlBrushKey] = Brush("#0b1422");
		combo.Resources[SystemColors.ControlTextBrushKey] = Brushes.White;
		combo.Resources[SystemColors.HighlightBrushKey] = Brush("#3f45ff");
		combo.Resources[SystemColors.HighlightTextBrushKey] = Brushes.White;
		combo.ItemContainerStyle = CreateComboBoxItemStyle();
		combo.Template = CreateComboBoxTemplate();
		combo.BorderThickness = new Thickness(1.0);
		combo.Padding = new Thickness(14.0, 0.0, 34.0, 0.0);
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
		popupBorder.SetValue(Border.BorderBrushProperty, Brush("#253a55"));
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
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#3f45ff")));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Triggers.Add(selected);
		Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#203249")));
		style.Triggers.Add(hover);
		return style;
	}
	private Border ViewToggle()
	{
		Border shell = new Border
		{
			Background = Brush("#0b1422"),
			BorderBrush = Brush("#253a55"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(7.0),
			Padding = new Thickness(2.0)
		};
		StackPanel row = new StackPanel { Orientation = Orientation.Horizontal };
		row.Children.Add(ToggleTile("Grid", true));
		row.Children.Add(ToggleTile("List", false));
		shell.Child = row;
		return shell;
	}

	private Border ToggleTile(string text, bool selected)
	{
		return new Border
		{
			Width = 52.0,
			Height = 40.0,
			Background = selected ? Brush("#3f45ff") : Brushes.Transparent,
			CornerRadius = new CornerRadius(5.0),
			Child = new TextBlock
			{
				Text = text,
				Foreground = selected ? Brushes.White : Brush("#9fb8d6"),
				FontWeight = FontWeights.Bold,
				FontSize = 12.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
	}

	private Button ActionButton(string text, string commandPath, string background)
	{
		Button button = new Button
		{
			Content = text,
			MinWidth = 154.0,
			Height = 48.0,
			Margin = new Thickness(12.0, 0.0, 0.0, 0.0),
			Padding = new Thickness(18.0, 0.0, 18.0, 0.0),
			Foreground = Brushes.White,
			Background = Brush(background),
			BorderBrush = Brush(background),
			BorderThickness = new Thickness(1.0),
			FontSize = 15.0,
			FontWeight = FontWeights.Bold,
			Cursor = System.Windows.Input.Cursors.Hand
		};
		button.SetBinding(Button.CommandProperty, new System.Windows.Data.Binding(commandPath));
		return button;
	}

	private void RebuildServers()
	{
		if (_serverCards == null || _viewModel == null)
		{
			return;
		}
		_serverCards.Children.Clear();
		List<ServerInstance> servers = FilterServers().ToList();
		foreach (ServerInstance server in servers)
		{
			_serverCards.Children.Add(CreateServerCard(server));
		}
		if (servers.Count < 2)
		{
			_serverCards.Children.Add(CreateEmptyServerPanel());
		}
		if (servers.Count == 0)
		{
			_serverCards.Children.Add(CreateNoMatchesPanel());
		}
	}

	private IEnumerable<ServerInstance> FilterServers()
	{
		if (_viewModel == null)
		{
			return Enumerable.Empty<ServerInstance>();
		}
		IEnumerable<ServerInstance> servers = _viewModel.Servers;
		string search = _searchBox?.Text?.Trim() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(search))
		{
			servers = servers.Where(server =>
				Contains(server.Name, search) ||
				Contains(server.GameDisplayName, search) ||
				Contains(server.MapName, search) ||
				Contains(server.InstallDirectory, search));
		}
		string status = _statusFilter?.SelectedItem?.ToString() ?? "All Statuses";
		if (status == "Online")
		{
			servers = servers.Where(server => server.IsOnline);
		}
		else if (status == "Offline")
		{
			servers = servers.Where(server => !server.IsOnline);
		}
		return servers;
	}

	private static bool Contains(string? value, string search)
	{
		return !string.IsNullOrWhiteSpace(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private Border CreateServerCard(ServerInstance server)
	{
		Border card = new Border
		{
			Width = 600.0,
			MinHeight = 500.0,
			Margin = new Thickness(0.0, 0.0, 24.0, 24.0),
			Background = new LinearGradientBrush(ColorFrom("#cc0b1730"), ColorFrom("#aa083134"), 0.0),
			BorderBrush = Brush("#29445f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0)
		};
		Grid root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		card.Child = root;

		root.Children.Add(new Border
		{
			Width = 5.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = Brush("#21d67b"),
			CornerRadius = new CornerRadius(8.0, 0.0, 0.0, 8.0)
		});

		Grid header = new Grid { Margin = new Thickness(38.0, 36.0, 30.0, 16.0) };
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		header.Children.Add(new TextBlock
		{
			Text = server.Name,
			Foreground = Brushes.White,
			FontSize = 25.0,
			FontWeight = FontWeights.Bold
		});
		Border badge = CreateStatusBadge(server);
		Grid.SetColumn(badge, 1);
		header.Children.Add(badge);
		root.Children.Add(header);

		StackPanel details = new StackPanel { Margin = new Thickness(38.0, 0.0, 30.0, 24.0) };
		details.Children.Add(DetailText("Game: " + server.GameDisplayName));
		details.Children.Add(DetailText("Map: " + server.MapName));
		details.Children.Add(DetailText(server.InstallDirectory));
		TextBlock endpoint = DetailText("IP: checking...");
		details.Children.Add(endpoint);
		_ = SetPublicEndpointAsync(endpoint, server.GamePort);
		Grid.SetRow(details, 1);
		root.Children.Add(details);

		Border rule = new Border
		{
			Height = 1.0,
			Background = Brush("#223855"),
			Margin = new Thickness(38.0, 0.0, 30.0, 24.0)
		};
		Grid.SetRow(rule, 2);
		root.Children.Add(rule);

		StackPanel lower = new StackPanel { Margin = new Thickness(38.0, 0.0, 30.0, 32.0) };
		Grid metrics = new Grid();
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.ColumnDefinitions.Add(new ColumnDefinition());
		metrics.Children.Add(MetricTile("CPU", server.CpuUsageText, "#22c55e", 0));
		metrics.Children.Add(MetricTile("Memory", server.MemoryUsageText, "#a855f7", 1));
		metrics.Children.Add(MetricTile("Users", server.PlayerCount + " / " + server.MaxPlayers, "#3b82f6", 2));
		lower.Children.Add(metrics);

		WrapPanel buttons = new WrapPanel { Margin = new Thickness(0.0, 26.0, 0.0, 0.0) };
		buttons.Children.Add(ServerButton("Configure", "#3f57ff", delegate { OpenDashboardConfigure(server, false); }));
		buttons.Children.Add(ServerButton("Start", "#16a34a", delegate { ExecuteForServer(server, _viewModel?.StartServerCommand); }));
		buttons.Children.Add(ServerButton("Stop", "#dc3f5c", delegate { ExecuteForServer(server, _viewModel?.StopServerCommand); }));
		buttons.Children.Add(ServerButton("Update", "#7138d8", delegate { ExecuteForServer(server, _viewModel?.InstallServerCommand); }));
		buttons.Children.Add(ServerButton("Location", "#2563eb", delegate { OpenServerLocation(server); }));
		buttons.Children.Add(ServerButton("Delete", "#b91c1c", async delegate { await DeleteServerAsync(server); }));
		lower.Children.Add(buttons);

		lower.Children.Add(new TextBlock
		{
			Text = "Last started: not available",
			Foreground = Brush("#a7bad8"),
			FontSize = 14.0,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(6.0, 28.0, 0.0, 0.0)
		});
		Grid.SetRow(lower, 3);
		root.Children.Add(lower);
		return card;
	}

	private TextBlock DetailText(string text)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = Brush("#c4d4eb"),
			FontSize = 14.0,
			FontWeight = FontWeights.SemiBold,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(0.0, 9.0, 0.0, 0.0)
		};
	}

	private Border CreateStatusBadge(ServerInstance server)
	{
		Border badge = new Border
		{
			Background = server.IsOnline ? Brush("#16a34a") : Brush("#24415d"),
			CornerRadius = new CornerRadius(18.0),
			Padding = new Thickness(20.0, 9.0, 20.0, 9.0),
			VerticalAlignment = VerticalAlignment.Top,
			Child = new TextBlock
			{
				Text = server.StatusText,
				Foreground = Brushes.White,
				FontSize = 14.0,
				FontWeight = FontWeights.Bold
			}
		};
		return badge;
	}

	private Border MetricTile(string label, string value, string accent, int column)
	{
		Border tile = new Border
		{
			MinHeight = 124.0,
			Background = Brush("#66071220"),
			BorderBrush = Brush("#243a55"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(18.0),
			Margin = new Thickness(column == 0 ? 0.0 : 12.0, 0.0, 0.0, 0.0)
		};
		Grid.SetColumn(tile, column);
		StackPanel stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = Brush("#dbeafe"),
			FontSize = 16.0,
			FontWeight = FontWeights.Bold
		});
		stack.Children.Add(new TextBlock
		{
			Text = value,
			Foreground = Brushes.White,
			FontSize = 21.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		});
		stack.Children.Add(new Border
		{
			Height = 4.0,
			Background = Brush(accent),
			CornerRadius = new CornerRadius(2.0),
			Margin = new Thickness(0.0, 26.0, 0.0, 0.0)
		});
		tile.Child = stack;
		return tile;
	}

	private Button ServerButton(string text, string color, RoutedEventHandler click)
	{
		Button button = new Button
		{
			Content = text,
			MinWidth = 116.0,
			Height = 50.0,
			Margin = new Thickness(0.0, 0.0, 12.0, 12.0),
			Foreground = Brushes.White,
			Background = Brush(color),
			BorderBrush = Brush(color),
			BorderThickness = new Thickness(1.0),
			FontSize = 14.0,
			FontWeight = FontWeights.Bold,
			Cursor = System.Windows.Input.Cursors.Hand
		};
		button.Click += click;
		return button;
	}

	private Border CreateEmptyServerPanel()
	{
		Border panel = new Border
		{
			Width = 520.0,
			MinHeight = 500.0,
			Margin = new Thickness(0.0, 0.0, 24.0, 24.0),
			Background = Brush("#7714243b"),
			BorderBrush = Brush("#33506d"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(34.0)
		};
		StackPanel stack = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		stack.Children.Add(new TextBlock
		{
			Text = "No more servers",
			Foreground = Brushes.White,
			FontSize = 26.0,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Center
		});
		stack.Children.Add(new TextBlock
		{
			Text = "Add a new server or import an existing one\nto get started.",
			Foreground = Brush("#a7bad8"),
			FontSize = 16.0,
			LineHeight = 25.0,
			TextAlignment = TextAlignment.Center,
			Margin = new Thickness(0.0, 18.0, 0.0, 28.0)
		});
		Button button = ServerButton("+  New Server", "#3f57ff", delegate
		{
			_ = ShowNewServerDialogAsync();
		});
		button.MinWidth = 190.0;
		button.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
		stack.Children.Add(button);
		Button existingButton = ServerButton("+  Add Existing Server", "#12a66d", delegate
		{
			_viewModel?.AddExistingServerCommand.Execute(null);
		});
		existingButton.MinWidth = 190.0;
		existingButton.Margin = new Thickness(0.0);
		stack.Children.Add(existingButton);
		panel.Child = stack;
		return panel;
	}

	private Border CreateNoMatchesPanel()
	{
		Border panel = CreateEmptyServerPanel();
		if (panel.Child is StackPanel stack && stack.Children[0] is TextBlock title)
		{
			title.Text = "No matching servers";
		}
		return panel;
	}

	private async Task ShowNewServerDialogAsync()
	{
		if (_viewModel == null)
		{
			return;
		}
		NewServerDialog dialog = new NewServerDialog(_viewModel.GameProfiles)
		{
			Owner = Window.GetWindow(this)
		};
		if (dialog.ShowDialog().GetValueOrDefault())
		{
			ServerInstance server = await _viewModel.CreateServerAsync(dialog.ServerName, dialog.SelectedGameId);
			if (_dashboardViewModel != null)
			{
				_dashboardViewModel.RefreshCommand.Execute(null);
			}
			OpenDashboardConfigure(server, false);
		}
	}

	private void SelectServer(ServerInstance server)
	{
		if (_viewModel != null)
		{
			_viewModel.SelectedServer = server;
		}
	}

	private void OpenDashboardConfigure(ServerInstance server, bool console)
	{
		SelectServer(server);
		if (_dashboardViewModel == null)
		{
			MessageBox.Show("Dashboard is not available yet.", "Configure server", MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		ServerInstance dashboardServer = _dashboardViewModel.Servers.FirstOrDefault((ServerInstance x) => x.Id == server.Id) ?? server;
		_configurePanel?.Open(dashboardServer, console);
		_dashboardViewModel.RequestConfigure(dashboardServer, console);
	}

	private void OnConfigureRequested(ServerInstance server, bool console)
	{
		if (!Dispatcher.CheckAccess())
		{
			Dispatcher.Invoke(delegate { OnConfigureRequested(server, console); });
			return;
		}
		_configurePanel?.Open(server, console);
	}

	private void ExecuteForServer(ServerInstance server, System.Windows.Input.ICommand? command)
	{
		SelectServer(server);
		if (command?.CanExecute(null) == true)
		{
			command.Execute(null);
		}
	}

	private async Task DeleteServerAsync(ServerInstance server)
	{
		SelectServer(server);
		if (_viewModel == null || !ServersViewModel.ConfirmRemoveServer(server))
		{
			return;
		}
		await _viewModel.RemoveServerAsync(server);
		RemoveDashboardServerReference(server);
	}

	private void OpenServerLocation(ServerInstance server)
	{
		try
		{
			SelectServer(server);
			if (string.IsNullOrWhiteSpace(server.InstallDirectory))
			{
				MessageBox.Show("This server does not have an install directory yet.", "File location", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			string installDirectory = server.InstallDirectory;
			if (!Directory.Exists(installDirectory))
			{
				MessageBoxResult result = MessageBox.Show(
					"The server install folder does not exist yet.\n\nCreate it now?",
					"File location",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question);
				if (result != MessageBoxResult.Yes)
				{
					return;
				}
				Directory.CreateDirectory(installDirectory);
			}

			Process.Start(new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = "\"" + installDirectory + "\"",
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			MessageBox.Show("Could not open the server folder:\n\n" + ex.Message, "File location", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private void RemoveDashboardServerReference(ServerInstance server)
	{
		if (_dashboardViewModel == null)
		{
			return;
		}
		ServerInstance? dashboardServer = _dashboardViewModel.Servers.FirstOrDefault((ServerInstance x) => x.Id == server.Id);
		if (dashboardServer != null)
		{
			_dashboardViewModel.Servers.Remove(dashboardServer);
		}
		if (_dashboardViewModel.SelectedServer?.Id == server.Id)
		{
			_dashboardViewModel.SelectedServer = _dashboardViewModel.Servers.FirstOrDefault();
		}
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
			return "Public IP unavailable";
		}
	}

	private static SolidColorBrush Brush(string color)
	{
		SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		brush.Freeze();
		return brush;
	}

	private static Color ColorFrom(string color)
	{
		return (Color)ColorConverter.ConvertFromString(color);
	}
}
