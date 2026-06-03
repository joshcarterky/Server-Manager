using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using ServerManager.ViewModels;

namespace ServerManager.Views;

public class SettingsView : UserControl
{
	private readonly SettingsViewModel _viewModel;
	private static readonly ColorOption[] ColorOptions =
	{
		new("Deep Navy", "#07101c"),
		new("Night Panel", "#111d2a"),
		new("Input Navy", "#0b1422"),
		new("Black Navy", "#020617"),
		new("Void Navy", "#030712"),
		new("Ink Navy", "#06111f"),
		new("Deep Ocean", "#082f49"),
		new("Midnight", "#050816"),
		new("Blue Midnight", "#0b1026"),
		new("Charcoal", "#111827"),
		new("Dark Charcoal", "#0f172a"),
		new("Graphite", "#1f2937"),
		new("Carbon", "#18181b"),
		new("Obsidian", "#09090b"),
		new("Dark Zinc", "#27272a"),
		new("Dark Stone", "#292524"),
		new("Slate", "#203249"),
		new("Dark Slate", "#1e293b"),
		new("Storm Slate", "#172033"),
		new("Cool Slate", "#334155"),
		new("Steel Blue", "#28445f"),
		new("Dark Steel", "#1f3a4d"),
		new("Deep Cobalt", "#1e3a8a"),
		new("Royal Blue", "#2563eb"),
		new("Bright Blue", "#4658ff"),
		new("Dark Indigo", "#312e81"),
		new("Sky Blue", "#38bdf8"),
		new("Teal", "#1498a8"),
		new("Dark Teal", "#134e4a"),
		new("Pine Teal", "#0f3f3f"),
		new("Cyan", "#06b6d4"),
		new("Emerald", "#10b981"),
		new("Dark Emerald", "#064e3b"),
		new("Forest", "#14532d"),
		new("Green", "#12a66d"),
		new("Lime", "#84cc16"),
		new("Olive", "#3f6212"),
		new("Amber", "#f59e0b"),
		new("Dark Amber", "#92400e"),
		new("Orange", "#f97316"),
		new("Burnt Orange", "#9a3412"),
		new("Red", "#d93b5c"),
		new("Dark Red", "#7f1d1d"),
		new("Burgundy", "#881337"),
		new("Rose", "#e11d48"),
		new("Pink", "#ec4899"),
		new("Dark Pink", "#831843"),
		new("Purple", "#7137d8"),
		new("Dark Purple", "#581c87"),
		new("Violet", "#8b5cf6"),
		new("Indigo", "#6366f1"),
		new("Gold", "#f4c430"),
		new("Cream", "#f8fafc"),
		new("Silver", "#cbd5e1"),
		new("White", "#ffffff"),
		new("Soft Text", "#d8e8ff"),
		new("Muted Text", "#9fb8d6"),
		new("Dim Text", "#64748b"),
		new("Black", "#000000")
	};

	public SettingsView(SettingsViewModel viewModel)
	{
		_viewModel = viewModel;
		DataContext = viewModel;
		Content = BuildView();
		_viewModel.PropertyChanged += delegate(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(SettingsViewModel.UiBackgroundColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiPanelColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiInputColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiAccentColor) ||
				args.PropertyName == nameof(SettingsViewModel.UiTextColor))
			{
				Content = BuildView();
			}
		};
	}

	private UIElement BuildView()
	{
		Grid root = new Grid
		{
			Background = Brush(_viewModel.UiBackgroundColor)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		StackPanel header = new StackPanel { Margin = new Thickness(28.0, 24.0, 28.0, 14.0) };
		header.Children.Add(new TextBlock
		{
			Text = "Settings",
			Foreground = Brush(_viewModel.UiTextColor),
			FontSize = 28.0,
			FontWeight = FontWeights.Bold
		});
		header.Children.Add(new TextBlock
		{
			Text = _viewModel.Description,
			Foreground = Brush("#9fb8d6"),
			FontSize = 13.0,
			Margin = new Thickness(0.0, 5.0, 0.0, 0.0)
		});
		root.Children.Add(header);

		ScrollViewer scrollViewer = new ScrollViewer
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 12.0),
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel content = new StackPanel();
		scrollViewer.Content = content;

		Grid pathsGrid = FormGrid();
		AddPathRow(pathsGrid, 0, "Application Folder", "AppDirectory", null, "OpenAppDirectoryCommand", true);
		AddPathRow(pathsGrid, 1, "SteamCMD Folder", "SteamCmdDirectory", "BrowseSteamCmdDirectoryCommand", "OpenSteamCmdDirectoryCommand", false);
		AddPathRow(pathsGrid, 2, "Backup Folder", "BackupDirectory", "BrowseBackupDirectoryCommand", "OpenBackupDirectoryCommand", false);
		AddPathRow(pathsGrid, 3, "Plugin Folder", "PluginDirectory", "BrowsePluginDirectoryCommand", "OpenPluginDirectoryCommand", false);
		AddReadonlyRow(pathsGrid, 4, "Config File", "ConfigFilePath");
		content.Children.Add(Panel("Application Paths", pathsGrid));

		Grid colorsGrid = FormGrid();
		AddColorRow(colorsGrid, 0, "Background Color", "UiBackgroundColor");
		AddColorRow(colorsGrid, 1, "Panel Color", "UiPanelColor");
		AddColorRow(colorsGrid, 2, "Input Color", "UiInputColor");
		AddColorRow(colorsGrid, 3, "Accent Color", "UiAccentColor");
		AddColorRow(colorsGrid, 4, "Text Color", "UiTextColor");
		content.Children.Add(Panel("Colors", colorsGrid));
		Grid integrationsGrid = FormGrid();
		AddTextRow(integrationsGrid, 0, "CurseForge API Key", "CurseForgeApiKey");
		AddCheckboxRow(integrationsGrid, 1, "Minimize to tray", "MinimizeToTray");
		content.Children.Add(Panel("Integrations", integrationsGrid));

		Grid updatesGrid = FormGrid();
		AddReadonlyRow(updatesGrid, 0, "Current Version", "CurrentVersion");
		AddReadonlyRow(updatesGrid, 1, "Latest Version", "LatestUpdateVersion");
		AddTextRow(updatesGrid, 2, "Update Manifest URL", "UpdateManifestUrl");
		AddRows(updatesGrid, 3);
		StackPanel updateActions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 4.0, 0.0, 12.0)
		};
		updateActions.Children.Add(ActionButton("Check Updates", "CheckForUpdatesCommand", _viewModel.UiAccentColor));
		updateActions.Children.Add(ActionButton("Download & Install", "InstallUpdateCommand", "#12a66d"));
		updateActions.Children.Add(ActionButton("Open Download", "OpenLatestDownloadCommand", "#203249"));
		Grid.SetRow(updateActions, 3);
		Grid.SetColumn(updateActions, 1);
		Grid.SetColumnSpan(updateActions, 3);
		updatesGrid.Children.Add(updateActions);
		content.Children.Add(Panel("Updates", updatesGrid));

		StackPanel actions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		actions.Children.Add(ActionButton("Reset Defaults", "ResetDefaultsCommand", "#203249"));
		actions.Children.Add(ActionButton("Reload Plugins", "ReloadPluginsCommand", "#203249"));
		actions.Children.Add(ActionButton("Save Settings", "SaveCommand", _viewModel.UiAccentColor));
		content.Children.Add(actions);

		Grid.SetRow(scrollViewer, 1);
		root.Children.Add(scrollViewer);

		Border status = new Border
		{
			Background = Brush(_viewModel.UiPanelColor),
			BorderBrush = Brush("#25384f"),
			BorderThickness = new Thickness(1.0, 1.0, 0.0, 0.0),
			Padding = new Thickness(28.0, 10.0, 28.0, 10.0)
		};
		TextBlock statusText = new TextBlock
		{
			Foreground = Brush(_viewModel.UiTextColor),
			FontSize = 12.0
		};
		statusText.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
		status.Child = statusText;
		Grid.SetRow(status, 2);
		root.Children.Add(status);

		return root;
	}

	private Border Panel(string title, UIElement child)
	{
		StackPanel panelContent = new StackPanel();
		panelContent.Children.Add(new TextBlock
		{
			Text = title,
			Foreground = Brush(_viewModel.UiTextColor),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		});
		panelContent.Children.Add(child);

		return new Border
		{
			Background = Brush(_viewModel.UiPanelColor),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(16.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0),
			Child = panelContent
		};
	}

	private static Grid FormGrid()
	{
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190.0) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		return grid;
	}

	private void AddPathRow(Grid grid, int row, string label, string valueBinding, string browseCommand, string openCommand, bool readOnly)
	{
		AddRows(grid, row);
		AddLabel(grid, row, label);

		TextBox textBox = InputTextBox(readOnly);
		textBox.SetBinding(TextBox.TextProperty, new Binding(valueBinding)
		{
			Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		Grid.SetRow(textBox, row);
		Grid.SetColumn(textBox, 1);
		grid.Children.Add(textBox);

		if (!string.IsNullOrWhiteSpace(browseCommand))
		{
			Button browse = SmallButton("Browse", browseCommand, _viewModel.UiAccentColor);
			Grid.SetRow(browse, row);
			Grid.SetColumn(browse, 2);
			grid.Children.Add(browse);
		}

		Button open = SmallButton("Open", openCommand, "#203249");
		Grid.SetRow(open, row);
		Grid.SetColumn(open, 3);
		grid.Children.Add(open);
	}

	private void AddReadonlyRow(Grid grid, int row, string label, string valueBinding)
	{
		AddRows(grid, row);
		AddLabel(grid, row, label);

		TextBox textBox = InputTextBox(true);
		textBox.SetBinding(TextBox.TextProperty, new Binding(valueBinding) { Mode = BindingMode.OneWay });
		Grid.SetRow(textBox, row);
		Grid.SetColumn(textBox, 1);
		Grid.SetColumnSpan(textBox, 3);
		grid.Children.Add(textBox);
	}

	private void AddTextRow(Grid grid, int row, string label, string valueBinding)
	{
		AddRows(grid, row);
		AddLabel(grid, row, label);

		TextBox textBox = InputTextBox(false);
		textBox.SetBinding(TextBox.TextProperty, new Binding(valueBinding)
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		Grid.SetRow(textBox, row);
		Grid.SetColumn(textBox, 1);
		Grid.SetColumnSpan(textBox, 3);
		grid.Children.Add(textBox);
	}

	private void AddColorRow(Grid grid, int row, string label, string valueBinding)
	{
		AddRows(grid, row);
		AddLabel(grid, row, label);

		ComboBox comboBox = DarkComboBox();
		comboBox.ItemsSource = ColorOptions;
		comboBox.SelectedValuePath = nameof(ColorOption.Hex);
		comboBox.SetBinding(Selector.SelectedValueProperty, new Binding(valueBinding)
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		Grid.SetRow(comboBox, row);
		Grid.SetColumn(comboBox, 1);
		Grid.SetColumnSpan(comboBox, 3);
		grid.Children.Add(comboBox);
	}

	private void AddCheckboxRow(Grid grid, int row, string label, string valueBinding)
	{
		AddRows(grid, row);
		AddLabel(grid, row, label);

		CheckBox checkBox = new CheckBox
		{
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = Brush(_viewModel.UiTextColor)
		};
		checkBox.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding(valueBinding)
		{
			Mode = BindingMode.TwoWay
		});
		Grid.SetRow(checkBox, row);
		Grid.SetColumn(checkBox, 1);
		grid.Children.Add(checkBox);
	}

	private static void AddRows(Grid grid, int row)
	{
		while (grid.RowDefinitions.Count <= row)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		}
	}

	private void AddLabel(Grid grid, int row, string label)
	{
		TextBlock textBlock = new TextBlock
		{
			Text = label,
			Foreground = Brush("#9fb8d6"),
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 14.0, 12.0)
		};
		Grid.SetRow(textBlock, row);
		grid.Children.Add(textBlock);
	}

	private TextBox InputTextBox(bool readOnly)
	{
		return new TextBox
		{
			Height = 38.0,
			Background = Brush(readOnly ? _viewModel.UiPanelColor : _viewModel.UiInputColor),
			Foreground = Brush(_viewModel.UiTextColor),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center,
			IsReadOnly = readOnly,
			Margin = new Thickness(0.0, 0.0, 10.0, 12.0)
		};
	}

	private ComboBox DarkComboBox()
	{
		ComboBox comboBox = new ComboBox
		{
			Height = 38.0,
			Background = Brush(_viewModel.UiInputColor),
			Foreground = Brush(_viewModel.UiTextColor),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(10.0, 0.0, 34.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 10.0, 12.0)
		};
		comboBox.Resources[SystemColors.WindowBrushKey] = Brush(_viewModel.UiInputColor);
		comboBox.Resources[SystemColors.ControlBrushKey] = Brush(_viewModel.UiInputColor);
		comboBox.Resources[SystemColors.ControlTextBrushKey] = Brush(_viewModel.UiTextColor);
		comboBox.Resources[SystemColors.HighlightBrushKey] = Brush(_viewModel.UiAccentColor);
		comboBox.Resources[SystemColors.HighlightTextBrushKey] = Brush(_viewModel.UiTextColor);
		comboBox.ItemContainerStyle = CreateComboBoxItemStyle();
		comboBox.ItemTemplate = CreateColorOptionTemplate();
		comboBox.Template = CreateComboBoxTemplate();
		return comboBox;
	}

	private static DataTemplate CreateColorOptionTemplate()
	{
		DataTemplate template = new DataTemplate(typeof(ColorOption));
		FrameworkElementFactory panel = new FrameworkElementFactory(typeof(StackPanel));
		panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
		panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

		FrameworkElementFactory swatch = new FrameworkElementFactory(typeof(Border));
		swatch.SetValue(FrameworkElement.WidthProperty, 18.0);
		swatch.SetValue(FrameworkElement.HeightProperty, 18.0);
		swatch.SetValue(Border.CornerRadiusProperty, new CornerRadius(4.0));
		swatch.SetValue(Border.BorderBrushProperty, Brush("#28445f"));
		swatch.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
		swatch.SetBinding(Border.BackgroundProperty, new Binding(nameof(ColorOption.Hex)) { Converter = new BrushConverterBinding() });
		panel.AppendChild(swatch);

		FrameworkElementFactory name = new FrameworkElementFactory(typeof(TextBlock));
		name.SetBinding(TextBlock.TextProperty, new Binding(nameof(ColorOption.Name)));
		name.SetValue(TextBlock.ForegroundProperty, Brushes.White);
		name.SetValue(TextBlock.MarginProperty, new Thickness(10.0, 0.0, 0.0, 0.0));
		name.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		panel.AppendChild(name);

		template.VisualTree = panel;
		return template;
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

	private static Button SmallButton(string text, string commandPath, string color)
	{
		Button button = new Button
		{
			Content = text,
			Height = 38.0,
			MinWidth = 82.0,
			Margin = new Thickness(0.0, 0.0, 10.0, 12.0),
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderBrush = Brush(color),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			Template = ButtonTemplate()
		};
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		return button;
	}

	private static Button ActionButton(string text, string commandPath, string color)
	{
		Button button = new Button
		{
			Content = text,
			Height = 40.0,
			MinWidth = 130.0,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderBrush = Brush(color),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(14.0, 0.0, 14.0, 0.0),
			Template = ButtonTemplate()
		};
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		return button;
	}

	private static ControlTemplate ButtonTemplate()
	{
		ControlTemplate template = new ControlTemplate(typeof(Button));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.Name = "ButtonBorder";
		border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
		border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
		border.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4.0));
		FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
		content.Name = "ButtonContent";
		content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		border.AppendChild(content);
		template.VisualTree = border;

		Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
		disabled.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#1a2a3e")));
		disabled.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#2a405e")));
		disabled.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#7f94ad")));
		disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.75));
		template.Triggers.Add(disabled);

		return template;
	}

	private static SolidColorBrush Brush(string color)
	{
		SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		brush.Freeze();
		return brush;
	}

	private sealed record ColorOption(string Name, string Hex);

	private sealed class BrushConverterBinding : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string color = value as string ?? "#0b1422";
			try
			{
				return Brush(color);
			}
			catch
			{
				return Brush("#0b1422");
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
