using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ServerManager.Services;
using ServerManager.ViewModels;
using ServerManager.Views;
using Serilog;
using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
using DrawingIcon = System.Drawing.Icon;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;
using WpfButton = System.Windows.Controls.Button;

namespace ServerManager;

public class MainWindow : Window, IComponentConnector
{
	private static readonly HexBrushConverter BrushConverter = new HexBrushConverter();

	private readonly NotifyIcon _trayIcon;

	private readonly IConfigService _configService;

	private bool _isClosing;

	private bool _contentLoaded;

	public MainWindow(MainViewModel mainViewModel, IConfigService configService)
	{
		Log.Information("MainWindow constructor start");
		string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.1.0";
		base.Title = "Dedicated Server Manager v" + version;
		Log.Information("MainWindow initialized");
		_configService = configService;
		base.DataContext = mainViewModel;
		base.Icon = new BitmapImage(new Uri("pack://application:,,,/assets/app/server-manager.png", UriKind.Absolute));
		base.Content = BuildModernShell(mainViewModel, version);
		base.MinWidth = 1180.0;
		base.MinHeight = 720.0;
		base.Width = 1440.0;
		base.Height = 860.0;
		base.SourceInitialized += delegate
		{
			ApplyDarkWindowChrome();
		};
		base.Loaded += delegate
		{
			Log.Information("MainWindow loaded");
		};
		_trayIcon = new NotifyIcon
		{
			Icon = GetApplicationIcon(),
			Visible = true,
			Text = "Dedicated Server Manager v" + version,
			ContextMenuStrip = new ContextMenuStrip()
		};
		_trayIcon.ContextMenuStrip.Items.Add("Open", null, delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)ShowWindow);
		});
		_trayIcon.ContextMenuStrip.Items.Add("Exit", null, delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
			{
				_isClosing = true;
				Close();
			});
		});
		_trayIcon.DoubleClick += delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)ShowWindow);
		};
	}

	private static DrawingIcon GetApplicationIcon()
	{
		string? executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
		if (!string.IsNullOrWhiteSpace(executablePath))
		{
			DrawingIcon? icon = DrawingIcon.ExtractAssociatedIcon(executablePath);
			if (icon != null)
			{
				return icon;
			}
		}
		return System.Drawing.SystemIcons.Application;
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		_isClosing = true;
		if (_configService != null && base.DataContext is MainViewModel mainViewModel)
		{
			_configService.SaveAsync(mainViewModel.AppConfig).GetAwaiter().GetResult();
		}
		_trayIcon.Visible = false;
		_trayIcon.ContextMenuStrip?.Dispose();
		_trayIcon.Dispose();
		base.OnClosing(e);
	}

	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
		System.Windows.Application.Current.Shutdown();
	}

	protected override void OnStateChanged(EventArgs e)
	{
		base.OnStateChanged(e);
		if (!_isClosing && base.WindowState == WindowState.Minimized)
		{
			Hide();
			_trayIcon.ShowBalloonTip(1500, "Dedicated Server Manager", "Running in the background. Double-click the tray icon to restore.", ToolTipIcon.Info);
		}
	}

	private void ShowWindow()
	{
		Show();
		base.WindowState = WindowState.Normal;
		Activate();
	}

	private void ApplyDarkWindowChrome()
	{
		IntPtr hwnd = new WindowInteropHelper(this).Handle;
		if (hwnd == IntPtr.Zero)
		{
			return;
		}

		int enabled = 1;
		if (DwmSetWindowAttribute(hwnd, DwmWindowAttribute.UseImmersiveDarkMode, ref enabled, Marshal.SizeOf<int>()) != 0)
		{
			DwmSetWindowAttribute(hwnd, DwmWindowAttribute.UseImmersiveDarkModeBefore20H1, ref enabled, Marshal.SizeOf<int>());
		}

		int captionColor = ColorRef("#020617");
		int borderColor = ColorRef("#020617");
		int textColor = ColorRef("#ffffff");
		DwmSetWindowAttribute(hwnd, DwmWindowAttribute.CaptionColor, ref captionColor, Marshal.SizeOf<int>());
		DwmSetWindowAttribute(hwnd, DwmWindowAttribute.BorderColor, ref borderColor, Marshal.SizeOf<int>());
		DwmSetWindowAttribute(hwnd, DwmWindowAttribute.TextColor, ref textColor, Marshal.SizeOf<int>());
	}

	private static int ColorRef(string hex)
	{
		System.Windows.Media.Color color = ColorFrom(hex);
		return color.R | (color.G << 8) | (color.B << 16);
	}

	private FrameworkElement BuildModernShell(MainViewModel viewModel, string version)
	{
		Grid shell = new Grid
		{
			Background = BrushFrom("#07101c")
		};
		shell.Resources.Add(typeof(ScrollBar), CreateDarkScrollBarStyle());
		shell.Resources[SystemColors.WindowBrushKey] = BrushFrom("#020617");
		shell.Resources[SystemColors.ControlBrushKey] = BrushFrom("#020617");
		shell.Resources[SystemColors.ControlLightBrushKey] = BrushFrom("#0f172a");
		shell.Resources[SystemColors.ControlDarkBrushKey] = BrushFrom("#334155");
		shell.Resources[SystemColors.ControlTextBrushKey] = BrushFrom("#ffffff");
		shell.Resources["SecondaryTextBrush"] = BrushFrom("#9fb8d6");
		BindBrush(shell, Panel.BackgroundProperty, nameof(MainViewModel.UiBackgroundColor), "#07101c");
		shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250.0) });
		shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		Border sidebar = BuildSidebar(viewModel);
		shell.Children.Add(sidebar);

		Border contentFrame = new Border();
		BindBrush(contentFrame, Border.BackgroundProperty, nameof(MainViewModel.UiBackgroundColor), "#07101c");
		Grid.SetColumn(contentFrame, 1);
		shell.Children.Add(contentFrame);

		ContentControl contentHost = new ContentControl();
		contentFrame.Child = contentHost;

		void RefreshPage()
		{
			contentHost.Content = CreatePage(viewModel.SelectedMenuItem);
		}

		viewModel.PropertyChanged += delegate(object? sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "SelectedMenuItem")
			{
				RefreshPage();
			}
		};
		RefreshPage();
		return shell;
	}

	private Border BuildSidebar(MainViewModel viewModel)
	{
		Border sidebar = new Border
		{
			Background = BrushFrom("#07101c"),
			BorderBrush = BrushFrom("#263a58"),
			BorderThickness = new Thickness(0.0, 0.0, 1.0, 0.0),
			Padding = new Thickness(16.0, 28.0, 16.0, 14.0)
		};
		BindBrush(sidebar, Border.BackgroundProperty, nameof(MainViewModel.UiBackgroundColor), "#07101c");
		Grid layout = new Grid();
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		StackPanel brand = new StackPanel { Orientation = Orientation.Horizontal };
		brand.Children.Add(new TextBlock
		{
			Text = "D",
			Foreground = BrushFrom("#8b5cf6"),
			FontSize = 46.0,
			FontWeight = FontWeights.Black,
			Margin = new Thickness(0.0, 0.0, 14.0, 0.0)
		});
		BindBrush((FrameworkElement)brand.Children[0], TextBlock.ForegroundProperty, nameof(MainViewModel.UiAccentColor), "#4658ff");
		StackPanel brandText = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		brandText.Children.Add(new TextBlock
		{
			Text = "Dedicated Server Manager",
			Foreground = Brushes.White,
			FontSize = 17.0,
			FontWeight = FontWeights.Bold
		});
		BindBrush((FrameworkElement)brandText.Children[0], TextBlock.ForegroundProperty, nameof(MainViewModel.UiTextColor), "#ffffff");
		brandText.Children.Add(new TextBlock
		{
			Text = "Manage dedicated game servers",
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 11.0,
			LineHeight = 17.0
		});
		brand.Children.Add(brandText);
		layout.Children.Add(brand);

		StackPanel nav = new StackPanel
		{
			Margin = new Thickness(0.0, 34.0, 0.0, 0.0)
		};
		foreach (MenuItemViewModel item in viewModel.MenuItems)
		{
			nav.Children.Add(CreateNavButton(viewModel, item));
		}
		Grid.SetRow(nav, 1);
		layout.Children.Add(nav);

		Border status = new Border
		{
			Background = BrushFrom("#99111d2a"),
			BorderBrush = BrushFrom("#24435f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(12.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 0.0)
		};
		BindBrush(status, Border.BackgroundProperty, nameof(MainViewModel.UiPanelColor), "#111d2a");
		StackPanel statusStack = new StackPanel { Orientation = Orientation.Horizontal };
		statusStack.Children.Add(new TextBlock
		{
			Text = "●",
			Foreground = BrushFrom("#22c55e"),
			FontSize = 18.0,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		});
		StackPanel statusText = new StackPanel();
		statusText.Children.Add(new TextBlock
		{
			Text = "System Status",
			Foreground = BrushFrom("#a7bad8"),
			FontSize = 11.0
		});
		statusText.Children.Add(new TextBlock
		{
			Text = "All Systems Operational",
			Foreground = BrushFrom("#22c55e"),
			FontSize = 11.0,
			FontWeight = FontWeights.SemiBold
		});
		statusStack.Children.Add(statusText);
		status.Child = statusStack;
		Grid.SetRow(status, 2);
		layout.Children.Add(status);

		sidebar.Child = layout;
		return sidebar;
	}

	private WpfButton CreateNavButton(MainViewModel viewModel, MenuItemViewModel item)
	{
		WpfButton button = new WpfButton
		{
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			Foreground = Brushes.White,
			Padding = new Thickness(0.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			MinHeight = 58.0
		};
		BindBrush(button, Control.ForegroundProperty, nameof(MainViewModel.UiTextColor), "#ffffff");
		button.Template = CreateButtonTemplate(8.0);
		Grid row = new Grid { Margin = new Thickness(0.0) };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		row.ColumnDefinitions.Add(new ColumnDefinition());
		Border icon = new Border
		{
			Width = 32.0,
			Height = 32.0,
			CornerRadius = new CornerRadius(6.0),
			Background = BrushFrom("#13243a"),
			Margin = new Thickness(8.0, 0.0, 12.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		BindBrush(icon, Border.BackgroundProperty, nameof(MainViewModel.UiInputColor), "#0b1422");
		icon.Child = new TextBlock
		{
			Text = item.Icon,
			Foreground = BrushFrom("#d8e8ff"),
			FontSize = 15.0,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		BindBrush((FrameworkElement)icon.Child, TextBlock.ForegroundProperty, nameof(MainViewModel.UiTextColor), "#ffffff");
		row.Children.Add(icon);
		StackPanel text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		text.Children.Add(new TextBlock
		{
			Text = item.Title,
			Foreground = Brushes.White,
			FontSize = 13.0,
			FontWeight = FontWeights.Bold
		});
		BindBrush((FrameworkElement)text.Children[0], TextBlock.ForegroundProperty, nameof(MainViewModel.UiTextColor), "#ffffff");
		text.Children.Add(new TextBlock
		{
			Text = item.Subtitle,
			Foreground = BrushFrom("#b4c4dc"),
			FontSize = 11.0,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		});
		Grid.SetColumn(text, 1);
		row.Children.Add(text);
		button.Content = row;
		button.Click += delegate { viewModel.SelectedMenuItem = item; };
		button.Loaded += delegate { UpdateNavButton(button, viewModel, item); };
		viewModel.PropertyChanged += delegate(object? sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "SelectedMenuItem")
			{
				UpdateNavButton(button, viewModel, item);
			}
			if (args.PropertyName == nameof(MainViewModel.UiAccentColor))
			{
				UpdateNavButton(button, viewModel, item);
			}
		};
		return button;
	}

	private void UpdateNavButton(WpfButton button, MainViewModel viewModel, MenuItemViewModel item)
	{
		button.Background = ReferenceEquals(viewModel.SelectedMenuItem, item)
			? BrushFrom(viewModel.UiAccentColor)
			: Brushes.Transparent;
	}

	private static void BindBrush(DependencyObject target, DependencyProperty property, string sourcePath, string fallback)
	{
		BindingOperations.SetBinding(target, property, new Binding(sourcePath)
		{
			Converter = BrushConverter,
			ConverterParameter = fallback,
			Mode = BindingMode.OneWay
		});
	}

	private object CreatePage(MenuItemViewModel item)
	{
		if (item.Page is DashboardViewModel dashboardViewModel)
		{
			return new ModernDashboardView(dashboardViewModel);
		}
		if (item.Page is FrameworkElement element)
		{
			return element;
		}
		return new PlaceholderModuleView
		{
			DataContext = item.Page
		};
	}

	private ControlTemplate CreateButtonTemplate(double radius)
	{
		ControlTemplate template = new ControlTemplate(typeof(WpfButton));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(System.Windows.Controls.Control.BackgroundProperty));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		border.AppendChild(content);
		template.VisualTree = border;
		return template;
	}

	private static SolidColorBrush BrushFrom(string hex)
	{
		return new SolidColorBrush(ColorFrom(hex));
	}

	private static Style CreateDarkScrollBarStyle()
	{
		Style style = new Style(typeof(ScrollBar));
		style.Setters.Add(new Setter(Control.BackgroundProperty, BrushFrom("#020617")));
		style.Setters.Add(new Setter(Control.ForegroundProperty, BrushFrom("#334155")));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, BrushFrom("#0f172a")));
		style.Setters.Add(new Setter(Control.TemplateProperty, CreateVerticalDarkScrollBarTemplate()));
		style.Setters.Add(new Setter(FrameworkElement.WidthProperty, 14.0));
		style.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 14.0));

		Trigger horizontal = new Trigger { Property = ScrollBar.OrientationProperty, Value = Orientation.Horizontal };
		horizontal.Setters.Add(new Setter(Control.TemplateProperty, CreateHorizontalDarkScrollBarTemplate()));
		horizontal.Setters.Add(new Setter(FrameworkElement.HeightProperty, 14.0));
		horizontal.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 14.0));
		horizontal.Setters.Add(new Setter(FrameworkElement.WidthProperty, double.NaN));
		horizontal.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 0.0));
		style.Triggers.Add(horizontal);

		return style;
	}

	private static ControlTemplate CreateVerticalDarkScrollBarTemplate()
	{
		const string xaml = """
			<ControlTemplate
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				TargetType="{x:Type ScrollBar}">
				<Grid Background="{TemplateBinding Background}" Width="{TemplateBinding Width}">
					<Track x:Name="PART_Track" IsDirectionReversed="true">
						<Track.DecreaseRepeatButton>
							<RepeatButton Command="{x:Static ScrollBar.PageUpCommand}" Focusable="false" Opacity="0" />
						</Track.DecreaseRepeatButton>
						<Track.Thumb>
							<Thumb Background="{TemplateBinding Foreground}">
								<Thumb.Template>
									<ControlTemplate TargetType="{x:Type Thumb}">
										<Border Background="{TemplateBinding Background}" CornerRadius="5" Margin="3,2" />
									</ControlTemplate>
								</Thumb.Template>
							</Thumb>
						</Track.Thumb>
						<Track.IncreaseRepeatButton>
							<RepeatButton Command="{x:Static ScrollBar.PageDownCommand}" Focusable="false" Opacity="0" />
						</Track.IncreaseRepeatButton>
					</Track>
				</Grid>
			</ControlTemplate>
			""";
		return (ControlTemplate)XamlReader.Parse(xaml);
	}

	private static ControlTemplate CreateHorizontalDarkScrollBarTemplate()
	{
		const string xaml = """
			<ControlTemplate
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				TargetType="{x:Type ScrollBar}">
				<Grid Background="{TemplateBinding Background}" Height="{TemplateBinding Height}">
					<Track x:Name="PART_Track">
						<Track.DecreaseRepeatButton>
							<RepeatButton Command="{x:Static ScrollBar.PageLeftCommand}" Focusable="false" Opacity="0" />
						</Track.DecreaseRepeatButton>
						<Track.Thumb>
							<Thumb Background="{TemplateBinding Foreground}">
								<Thumb.Template>
									<ControlTemplate TargetType="{x:Type Thumb}">
										<Border Background="{TemplateBinding Background}" CornerRadius="5" Margin="2,3" />
									</ControlTemplate>
								</Thumb.Template>
							</Thumb>
						</Track.Thumb>
						<Track.IncreaseRepeatButton>
							<RepeatButton Command="{x:Static ScrollBar.PageRightCommand}" Focusable="false" Opacity="0" />
						</Track.IncreaseRepeatButton>
					</Track>
				</Grid>
			</ControlTemplate>
			""";
		return (ControlTemplate)XamlReader.Parse(xaml);
	}

	private static System.Windows.Media.Color ColorFrom(string hex)
	{
		return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute attribute, ref int attributeValue, int attributeSize);

	private enum DwmWindowAttribute
	{
		UseImmersiveDarkModeBefore20H1 = 19,
		UseImmersiveDarkMode = 20,
		BorderColor = 34,
		CaptionColor = 35,
		TextColor = 36
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			// Window content is built in code; old recovered BAML is intentionally not loaded.
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
	}
}
