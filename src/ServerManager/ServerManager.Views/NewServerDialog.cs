using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using ServerManager.Models;

namespace ServerManager.Views;

public class NewServerDialog : Window
{
	private readonly TextBox _serverNameBox;
	private readonly ComboBox _gameBox;

	public string ServerName => _serverNameBox.Text.Trim();

	public string SelectedGameId => (_gameBox.SelectedItem as GameProfile)?.Id ?? GameProfileCatalog.Default.Id;

	public NewServerDialog(IEnumerable<GameProfile> gameProfiles)
	{
		Title = "New Server";
		Width = 460.0;
		Height = 390.0;
		MinHeight = 390.0;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		ResizeMode = ResizeMode.NoResize;
		Background = Brush("#0f1b2a");
		Foreground = Brushes.White;

		Border shell = new Border
		{
			Background = Brush("#0f1b2a"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(24.0)
		};
		Content = shell;

		Grid layout = new Grid();
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		shell.Child = layout;

		layout.Children.Add(new TextBlock
		{
			Text = "Create Server",
			Foreground = Brushes.White,
			FontSize = 24.0,
			FontWeight = FontWeights.Bold
		});

		TextBlock description = new TextBlock
		{
			Text = "Choose the game and give this server a name.",
			Foreground = Brush("#9fb8d6"),
			FontSize = 13.0,
			Margin = new Thickness(0.0, 6.0, 0.0, 20.0)
		};
		Grid.SetRow(description, 1);
		layout.Children.Add(description);

		StackPanel fields = new StackPanel();
		Grid.SetRow(fields, 2);
		layout.Children.Add(fields);

		fields.Children.Add(Label("Game"));
		_gameBox = new ComboBox
		{
			Height = 40.0,
			ItemsSource = gameProfiles.ToList(),
			DisplayMemberPath = "DisplayName",
			SelectedIndex = 0,
			Foreground = Brushes.White,
			Background = Brush("#101f31"),
			BorderBrush = Brush("#2b405f"),
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0),
			Margin = new Thickness(0.0, 6.0, 0.0, 16.0)
		};
		ApplyDarkComboBox(_gameBox);
		fields.Children.Add(_gameBox);

		fields.Children.Add(Label("Server Name"));
		_serverNameBox = new TextBox
		{
			Height = 40.0,
			Text = "New ASA Server",
			Foreground = Brushes.White,
			Background = Brush("#101f31"),
			BorderBrush = Brush("#2b405f"),
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		fields.Children.Add(_serverNameBox);

		StackPanel buttons = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
		};
		Grid.SetRow(buttons, 4);
		layout.Children.Add(buttons);

		Button cancel = DialogButton("Cancel", "#16263a");
		cancel.Click += delegate
		{
			DialogResult = false;
		};
		buttons.Children.Add(cancel);

		Button create = DialogButton("Create", "#3f57ff");
		create.Click += delegate
		{
			if (string.IsNullOrWhiteSpace(ServerName))
			{
				MessageBox.Show(this, "Enter a server name first.", "Server name required", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				_serverNameBox.Focus();
				return;
			}
			DialogResult = true;
		};
		buttons.Children.Add(create);

		Loaded += delegate
		{
			_serverNameBox.Focus();
			_serverNameBox.SelectAll();
		};
	}

	private static TextBlock Label(string text)
	{
		return new TextBlock
		{
			Text = text,
			Foreground = Brush("#b8cff0"),
			FontSize = 13.0,
			FontWeight = FontWeights.SemiBold
		};
	}

	private static Button DialogButton(string text, string color)
	{
		return new Button
		{
			Content = text,
			MinWidth = 104.0,
			Height = 40.0,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
			Padding = new Thickness(16.0, 0.0, 16.0, 0.0),
			Foreground = Brushes.White,
			Background = Brush(color),
			BorderBrush = Brush(color),
			BorderThickness = new Thickness(1.0),
			FontSize = 13.0,
			FontWeight = FontWeights.Bold,
			Cursor = System.Windows.Input.Cursors.Hand
		};
	}

	private static void ApplyDarkComboBox(ComboBox combo)
	{
		combo.Background = Brush("#101f31");
		combo.Foreground = Brushes.White;
		combo.BorderBrush = Brush("#2b405f");
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

	private static SolidColorBrush Brush(string color)
	{
		SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		brush.Freeze();
		return brush;
	}
}
