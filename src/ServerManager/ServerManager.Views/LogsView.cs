using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using ServerManager.Models;
using ServerManager.Services;
using ServerManager.ViewModels;

namespace ServerManager.Views;

public class LogsView : UserControl
{
	private readonly LogsViewModel _viewModel;
	private string _backgroundColor = "#07101c";
	private string _panelColor = "#111d2a";
	private string _inputColor = "#0b1422";
	private string _accentColor = "#4658ff";
	private string _textColor = "#ffffff";

	public LogsView(LogsViewModel viewModel)
	{
		_viewModel = viewModel;
		DataContext = viewModel;
		Content = BuildView();
	}

	public LogsView(LogsViewModel viewModel, IConfigService configService)
		: this(viewModel)
	{
		try
		{
			AppConfig config = configService.LoadAsync().GetAwaiter().GetResult();
			_backgroundColor = ColorOrDefault(config.UiBackgroundColor, _backgroundColor);
			_panelColor = ColorOrDefault(config.UiPanelColor, _panelColor);
			_inputColor = ColorOrDefault(config.UiInputColor, _inputColor);
			_accentColor = ColorOrDefault(config.UiAccentColor, _accentColor);
			_textColor = ColorOrDefault(config.UiTextColor, _textColor);
			Content = BuildView();
		}
		catch
		{
			Content = BuildView();
		}
	}

	private UIElement BuildView()
	{
		Grid root = new Grid
		{
			Background = Brush(_backgroundColor),
			Margin = new Thickness(0.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		StackPanel header = new StackPanel { Margin = new Thickness(28.0, 24.0, 28.0, 14.0) };
		header.Children.Add(new TextBlock
		{
			Text = "Logs",
			Foreground = Brush(_textColor),
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

		Grid summary = new Grid
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 12.0)
		};
		for (int i = 0; i < 5; i++)
		{
			summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		}
		AddSummaryCard(summary, 0, "Loaded", "LoadedLineCount", "#9fb8d6");
		AddSummaryCard(summary, 1, "Showing", "VisibleLineCount", "#9fb8d6");
		AddSummaryCard(summary, 2, "Errors", "ErrorCount", "#ff647c");
		AddSummaryCard(summary, 3, "Warnings", "WarningCount", "#ffd166");
		AddSummaryCard(summary, 4, "Info", "InfoCount", "#7dd3fc");
		Grid.SetRow(summary, 1);
		root.Children.Add(summary);

		Border tools = new Border
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 12.0),
			Background = Brush(_panelColor),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(14.0)
		};
		Grid toolGrid = new Grid();
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260.0) });
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14.0) });
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190.0) });
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14.0) });
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		toolGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		tools.Child = toolGrid;

		ComboBox files = CreateComboBox();
		files.DisplayMemberPath = "DisplayName";
		files.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("LogFiles"));
		files.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedLogFile") { Mode = BindingMode.TwoWay });
		toolGrid.Children.Add(files);

		ComboBox severity = CreateComboBox();
		severity.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Severities"));
		severity.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedSeverity") { Mode = BindingMode.TwoWay });
		Grid.SetColumn(severity, 2);
		toolGrid.Children.Add(severity);

		TextBox search = new TextBox
		{
			Height = 38.0,
			Background = Brush(_inputColor),
			Foreground = Brush(_textColor),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center,
			ToolTip = "Search logs"
		};
		search.SetBinding(TextBox.TextProperty, new Binding("SearchText")
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		Grid.SetColumn(search, 4);
		toolGrid.Children.Add(search);

		StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal };
		buttons.Children.Add(ActionButton("Refresh", "RefreshCommand", _accentColor));
		buttons.Children.Add(ActionButton("Open Folder", "OpenLogFolderCommand", "#203249"));
		buttons.Children.Add(ActionButton("Copy Line", "CopySelectedCommand", "#203249"));
		buttons.Children.Add(ActionButton("Export", "ExportCommand", "#12a66d"));
		Grid.SetColumn(buttons, 5);
		toolGrid.Children.Add(buttons);
		Grid.SetRow(tools, 2);
		root.Children.Add(tools);

		Grid logArea = new Grid
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 12.0)
		};
		logArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3.0, GridUnitType.Star) });
		logArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14.0) });
		logArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });

		DataGrid logLines = new DataGrid
		{
			Background = Brush(_panelColor),
			Foreground = Brush("#d8e8ff"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			FontFamily = new FontFamily("Consolas"),
			FontSize = 12.0,
			AutoGenerateColumns = false,
			CanUserAddRows = false,
			CanUserDeleteRows = false,
			CanUserReorderColumns = false,
			CanUserResizeRows = false,
			GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
			HeadersVisibility = DataGridHeadersVisibility.Column,
			IsReadOnly = true,
			RowBackground = Brush(_panelColor),
			AlternatingRowBackground = Brush("#0d1928"),
			HorizontalGridLinesBrush = Brush("#223650"),
			VerticalGridLinesBrush = Brushes.Transparent,
			SelectionMode = DataGridSelectionMode.Single,
			SelectionUnit = DataGridSelectionUnit.FullRow
		};
		logLines.ColumnHeaderStyle = CreateDataGridHeaderStyle();
		logLines.RowStyle = CreateDataGridRowStyle();
		logLines.Columns.Add(CreateTextColumn("Time", "Timestamp", 190.0));
		logLines.Columns.Add(CreateTextColumn("Level", "LevelText", 95.0));
		logLines.Columns.Add(CreateTextColumn("Message", "Message", new DataGridLength(1.0, DataGridLengthUnitType.Star)));
		logLines.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("VisibleEntries"));
		logLines.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedEntry") { Mode = BindingMode.TwoWay });
		logArea.Children.Add(logLines);

		Border details = new Border
		{
			Background = Brush(_panelColor),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(14.0)
		};
		StackPanel detailStack = new StackPanel();
		detailStack.Children.Add(new TextBlock
		{
			Text = "Selected Entry",
			Foreground = Brush(_textColor),
			FontSize = 18.0,
			FontWeight = FontWeights.Bold
		});
		TextBlock fileSummary = new TextBlock
		{
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 6.0, 0.0, 12.0)
		};
		fileSummary.SetBinding(TextBlock.TextProperty, new Binding("SelectedFileSummary"));
		detailStack.Children.Add(fileSummary);
		detailStack.Children.Add(new TextBlock
		{
			Text = "Latest Issue",
			Foreground = Brush("#9fb8d6"),
			FontSize = 11.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		});
		TextBlock latestProblem = new TextBlock
		{
			Foreground = Brush("#ffd166"),
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 0.0, 0.0, 16.0)
		};
		latestProblem.SetBinding(TextBlock.TextProperty, new Binding("LatestProblem"));
		detailStack.Children.Add(latestProblem);
		detailStack.Children.Add(new TextBlock
		{
			Text = "Raw Line",
			Foreground = Brush("#9fb8d6"),
			FontSize = 11.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		});
		TextBox rawLine = new TextBox
		{
			Background = Brush(_inputColor),
			Foreground = Brush("#d8e8ff"),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			FontFamily = new FontFamily("Consolas"),
			FontSize = 12.0,
			IsReadOnly = true,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			MinHeight = 220.0,
			Padding = new Thickness(10.0)
		};
		rawLine.SetBinding(TextBox.TextProperty, new Binding("SelectedEntry.RawLine"));
		detailStack.Children.Add(rawLine);
		details.Child = detailStack;
		Grid.SetColumn(details, 2);
		logArea.Children.Add(details);

		Grid.SetRow(logArea, 3);
		root.Children.Add(logArea);

		Border status = new Border
		{
			Background = Brush("#08111b"),
			BorderBrush = Brush("#25384f"),
			BorderThickness = new Thickness(1.0, 1.0, 0.0, 0.0),
			Padding = new Thickness(28.0, 10.0, 28.0, 10.0)
		};
		TextBlock statusText = new TextBlock
		{
			Foreground = Brush("#d8e8ff"),
			FontSize = 12.0
		};
		statusText.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
		status.Child = statusText;
		Grid.SetRow(status, 4);
		root.Children.Add(status);
		return root;
	}

	private void AddSummaryCard(Grid grid, int column, string label, string bindingPath, string valueColor)
	{
		Border card = new Border
		{
			Background = Brush(_panelColor),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			Margin = new Thickness(column == 0 ? 0.0 : 10.0, 0.0, 0.0, 0.0)
		};
		StackPanel stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = Brush("#9fb8d6"),
			FontSize = 11.0,
			FontWeight = FontWeights.Bold
		});
		TextBlock value = new TextBlock
		{
			Foreground = Brush(valueColor),
			FontSize = 22.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		value.SetBinding(TextBlock.TextProperty, new Binding(bindingPath) { StringFormat = "{0:N0}" });
		stack.Children.Add(value);
		card.Child = stack;
		Grid.SetColumn(card, column);
		grid.Children.Add(card);
	}

	private DataGridTextColumn CreateTextColumn(string header, string bindingPath, double width)
	{
		return CreateTextColumn(header, bindingPath, new DataGridLength(width));
	}

	private DataGridTextColumn CreateTextColumn(string header, string bindingPath, DataGridLength width)
	{
		Style textStyle = new Style(typeof(TextBlock));
		textStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brush("#d8e8ff")));
		textStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.NoWrap));
		textStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
		textStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(8.0, 0.0, 8.0, 0.0)));
		return new DataGridTextColumn
		{
			Header = header,
			Binding = new Binding(bindingPath),
			Width = width,
			ElementStyle = textStyle
		};
	}

	private Style CreateDataGridHeaderStyle()
	{
		Style style = new Style(typeof(DataGridColumnHeader));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#0b1422")));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#9fb8d6")));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#263a58")));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0.0, 0.0, 1.0, 1.0)));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8.0, 8.0, 8.0, 8.0)));
		style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
		return style;
	}

	private Style CreateDataGridRowStyle()
	{
		Style style = new Style(typeof(DataGridRow));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#d8e8ff")));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#223650")));
		style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0.0, 0.0, 0.0, 1.0)));
		Trigger selected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#2f5b8d")));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Triggers.Add(selected);
		return style;
	}

	private ComboBox CreateComboBox()
	{
		ComboBox combo = new ComboBox
		{
			Height = 38.0,
			Background = Brush(_inputColor),
			Foreground = Brush(_textColor),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(10.0, 0.0, 34.0, 0.0),
			VerticalContentAlignment = VerticalAlignment.Center
		};
		combo.Resources[SystemColors.WindowBrushKey] = Brush(_inputColor);
		combo.Resources[SystemColors.ControlBrushKey] = Brush(_inputColor);
		combo.Resources[SystemColors.ControlTextBrushKey] = Brush(_textColor);
		combo.Resources[SystemColors.HighlightBrushKey] = Brush(_accentColor);
		combo.Resources[SystemColors.HighlightTextBrushKey] = Brush(_textColor);
		combo.ItemContainerStyle = CreateComboBoxItemStyle();
		combo.Template = CreateComboBoxTemplate();
		return combo;
	}

	private ControlTemplate CreateComboBoxTemplate()
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
		content.SetValue(TextElement.ForegroundProperty, Brush(_textColor));
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
		popupBorder.SetValue(Border.BackgroundProperty, Brush(_inputColor));
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

	private Style CreateComboBoxItemStyle()
	{
		Style style = new Style(typeof(ComboBoxItem));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush(_inputColor)));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brush(_textColor)));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10.0, 7.0, 10.0, 7.0)));
		Trigger selected = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush(_accentColor)));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brush(_textColor)));
		style.Triggers.Add(selected);
		Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#203249")));
		style.Triggers.Add(hover);
		return style;
	}

	private static Button ActionButton(string text, string commandPath, string color)
	{
		Button button = new Button
		{
			Content = text,
			Height = 38.0,
			MinWidth = 98.0,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderBrush = Brush(color),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0)
		};
		button.SetBinding(Button.CommandProperty, new Binding(commandPath));
		return button;
	}

	private static string ColorOrDefault(string value, string fallback)
	{
		try
		{
			ColorConverter.ConvertFromString(value);
			return value;
		}
		catch
		{
			return fallback;
		}
	}

	private static SolidColorBrush Brush(string color)
	{
		SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorOrDefault(color, "#0b1422")));
		brush.Freeze();
		return brush;
	}
}
