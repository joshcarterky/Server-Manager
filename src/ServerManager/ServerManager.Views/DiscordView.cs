using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ServerManager.ViewModels;

namespace ServerManager.Views;

public class DiscordView : UserControl
{
	public DiscordView(DiscordViewModel viewModel)
	{
		DataContext = viewModel;
		Content = BuildContent();
	}

	private UIElement BuildContent()
	{
		Grid root = new Grid
		{
			Background = new LinearGradientBrush(ColorFrom("#06111f"), ColorFrom("#0b1430"), 45.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		StackPanel header = new StackPanel { Margin = new Thickness(28.0, 28.0, 28.0, 20.0) };
		header.Children.Add(new TextBlock
		{
			Text = "Discord Integration",
			Foreground = Brushes.White,
			FontSize = 32.0,
			FontWeight = FontWeights.Bold
		});
		header.Children.Add(new TextBlock
		{
			Text = "Webhook notifications for server automation, moderation, and maintenance events.",
			Foreground = Brush("#9fb8d6"),
			FontSize = 14.0,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		});
		root.Children.Add(header);

		Border panel = new Border
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 28.0),
			Padding = new Thickness(20.0),
			Background = Brush("#aa0d1828"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0)
		};
		Grid.SetRow(panel, 1);

		Grid form = new Grid();
		form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		form.Children.Add(SectionTitle("Webhook"));
		StackPanel webhookStack = new StackPanel { Margin = new Thickness(0.0, 34.0, 0.0, 18.0) };
		webhookStack.Children.Add(Label("Discord Webhook URL"));
		TextBox webhook = CreateTextBox();
		webhook.SetBinding(TextBox.TextProperty, new Binding("WebhookUrl") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
		webhookStack.Children.Add(webhook);
		Grid.SetRow(webhookStack, 0);
		form.Children.Add(webhookStack);

		StackPanel notifications = new StackPanel { Margin = new Thickness(0.0, 12.0, 0.0, 20.0) };
		notifications.Children.Add(SectionTitle("Notifications"));
		notifications.Children.Add(CheckBox("Restart notifications", "SendRestartNotifications"));
		notifications.Children.Add(CheckBox("Crash alerts", "SendCrashAlerts"));
		notifications.Children.Add(CheckBox("Update alerts", "SendUpdateAlerts"));
		Grid.SetRow(notifications, 1);
		form.Children.Add(notifications);

		StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0.0, 12.0, 0.0, 18.0) };
		Button save = CreateButton("Save Settings", "#4658ff");
		save.SetBinding(Button.CommandProperty, new Binding("SaveCommand"));
		actions.Children.Add(save);
		Button test = CreateButton("Send Test Message", "#16a34a");
		test.SetBinding(Button.CommandProperty, new Binding("TestWebhookCommand"));
		actions.Children.Add(test);
		Grid.SetRow(actions, 2);
		form.Children.Add(actions);

		Border statusCard = new Border
		{
			Background = Brush("#111d2a"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(12.0),
			MaxWidth = 760.0,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		TextBlock status = new TextBlock { Foreground = Brush("#bcd6f7"), FontSize = 13.0, TextWrapping = TextWrapping.Wrap };
		status.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
		statusCard.Child = status;
		Grid.SetRow(statusCard, 3);
		form.Children.Add(statusCard);

		panel.Child = form;
		root.Children.Add(panel);
		return root;
	}

	private static TextBlock SectionTitle(string text)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = Brushes.White,
			FontSize = 20.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
	}

	private static TextBlock Label(string text)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
	}

	private static TextBox CreateTextBox()
	{
		return new TextBox
		{
			Height = 42.0,
			MaxWidth = 760.0,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Foreground = Brushes.White,
			Background = Brush("#0b1422"),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center
		};
	}

	private static CheckBox CheckBox(string text, string bindingPath)
	{
		CheckBox checkBox = new CheckBox
		{
			Content = text,
			Foreground = Brushes.White,
			FontSize = 14.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		checkBox.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding(bindingPath) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
		return checkBox;
	}

	private static Button CreateButton(string text, string color)
	{
		Button button = new Button
		{
			Content = text,
			MinWidth = 150.0,
			Height = 40.0,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
			Foreground = Brushes.White,
			Background = Brush(color),
			BorderBrush = Brush(color),
			BorderThickness = new Thickness(1.0),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(14.0, 0.0, 14.0, 0.0)
		};
		button.Template = ButtonTemplate();
		return button;
	}

	private static ControlTemplate ButtonTemplate()
	{
		ControlTemplate template = new ControlTemplate(typeof(Button));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
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
		SolidColorBrush brush = new SolidColorBrush(ColorFrom(color));
		brush.Freeze();
		return brush;
	}

	private static Color ColorFrom(string color)
	{
		return (Color)ColorConverter.ConvertFromString(color);
	}
}