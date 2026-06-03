using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ServerManager.ViewModels;

namespace ServerManager.Views;

public class RconConsoleSurface : Grid
{
	private readonly ConsoleViewModel _viewModel;

	public RconConsoleSurface(ConsoleViewModel viewModel)
	{
		_viewModel = viewModel;
		DataContext = viewModel;
		Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		MinHeight = 260.0;
		VerticalAlignment = VerticalAlignment.Stretch;
		RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		Children.Add(BuildHeader());
		UIElement console = BuildConsoleOutput();
		Grid.SetRow(console, 1);
		Children.Add(console);
		UIElement commandBar = BuildCommandBar();
		Grid.SetRow(commandBar, 2);
		Children.Add(commandBar);
	}

	private UIElement BuildHeader()
	{
		Grid header = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel title = new StackPanel();
		title.Children.Add(new TextBlock
		{
			Text = "RCON Console",
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.Bold
		});
		TextBlock selected = new TextBlock
		{
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
		};
		selected.SetBinding(TextBlock.TextProperty, new Binding("SelectedServer.Name")
		{
			StringFormat = "Selected server: {0}",
			TargetNullValue = "Selected server: none"
		});
		title.Children.Add(selected);
		header.Children.Add(title);

		StackPanel actions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		};
		actions.Children.Add(HeaderCommandButton("Reconnect", "RefreshConnectionCommand", "#203249"));
		actions.Children.Add(HeaderCommandButton("Clear", "ClearConsoleCommand", "#203249"));
		Grid.SetColumn(actions, 1);
		header.Children.Add(actions);

		Border status = new Border
		{
			Background = Brush("#142235"),
			BorderBrush = Brush("#2a3d55"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(12.0, 7.0, 12.0, 7.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		TextBlock statusText = new TextBlock
		{
			Foreground = Brush("#d8e8ff"),
			FontWeight = FontWeights.SemiBold
		};
		statusText.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
		status.Child = statusText;
		Grid.SetColumn(status, 2);
		header.Children.Add(status);
		return header;
	}

	private UIElement BuildConsoleOutput()
	{
		Border frame = new Border
		{
			Background = Brush("#071019"),
			BorderBrush = Brush("#263a51"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(10.0),
			MinHeight = 160.0,
			MaxHeight = 430.0
		};
		ListBox lines = new ListBox
		{
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			Foreground = Brush("#d6e6f8"),
			FontFamily = new FontFamily("Consolas"),
			FontSize = 12.0
		};
		ScrollViewer.SetVerticalScrollBarVisibility(lines, ScrollBarVisibility.Auto);
		ScrollViewer.SetHorizontalScrollBarVisibility(lines, ScrollBarVisibility.Auto);
		lines.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ConsoleLines"));
		frame.Child = lines;
		return frame;
	}

	private UIElement BuildCommandBar()
	{
		Grid bar = new Grid
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0),
			MinHeight = 36.0,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		bar.ColumnDefinitions.Add(new ColumnDefinition());
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		TextBox command = new TextBox
		{
			Background = Brush("#101b28"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#2a3d55"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(10.0, 8.0, 10.0, 8.0),
			FontFamily = new FontFamily("Consolas"),
			FontSize = 12.0
		};
		command.SetBinding(TextBox.TextProperty, new Binding("CommandText")
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		command.KeyDown += delegate(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.Enter && _viewModel.SendCommandCommand.CanExecute(null))
			{
				_viewModel.SendCommandCommand.Execute(null);
				args.Handled = true;
			}
		};
		bar.Children.Add(command);
		bar.Children.Add(CommandButton("Send", "SendCommandCommand", "#4658ff", 1));
		bar.Children.Add(CommandButton("Reconnect", "RefreshConnectionCommand", "#203249", 2));
		bar.Children.Add(CommandButton("Connect", "ConnectCommand", "#203249", 3));
		bar.Children.Add(CommandButton("Disconnect", "DisconnectCommand", "#203249", 4));
		bar.Children.Add(CommandButton("Clear", "ClearConsoleCommand", "#203249", 5));
		return bar;
	}

	private static Button HeaderCommandButton(string text, string commandPath, string color)
	{
		Button button = new Button
		{
			Content = text,
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			MinWidth = 86.0,
			Height = 32.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			Template = ButtonTemplate()
		};
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		return button;
	}

	private static Button CommandButton(string text, string commandPath, string color, int column)
	{
		Button button = new Button
		{
			Content = text,
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			MinWidth = 86.0,
			Height = 36.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			Template = ButtonTemplate()
		};
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		Grid.SetColumn(button, column);
		return button;
	}

	private static ControlTemplate ButtonTemplate()
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
}
