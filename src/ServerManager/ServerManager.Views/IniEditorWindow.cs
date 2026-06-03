using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using ServerManager.ViewModels;

namespace ServerManager.Views;

public class IniEditorWindow : Window, IComponentConnector
{
	private readonly Func<Task>? _saveAsync;
	private TextBlock? _statusText;
	private bool _contentLoaded;

	public IniEditorWindow(ObservableCollection<IniSettingViewModel> settings, Func<Task>? saveAsync = null)
	{
		_saveAsync = saveAsync;
		InitializeComponent();
		base.DataContext = new IniEditorWindowViewModel(settings);
	}

	private void Done_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = true;
		Close();
	}

	private async void Save_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_statusText != null)
			{
				_statusText.Text = "Saving INI settings...";
			}
			if (_saveAsync != null)
			{
				await _saveAsync();
			}
			if (_statusText != null)
			{
				_statusText.Text = "Saved. Configure Server values refreshed.";
			}
		}
		catch (Exception ex)
		{
			if (_statusText != null)
			{
				_statusText.Text = "Save failed.";
			}
			MessageBox.Show(this, ex.Message, "INI save failed", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Title = "INI Configuration Editor";
			Width = 1180.0;
			Height = 780.0;
			MinWidth = 980.0;
			MinHeight = 640.0;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			WindowStyle = WindowStyle.None;
			ResizeMode = ResizeMode.CanResize;
			Background = Brush("#07101c");
			Foreground = Brushes.White;
			Content = BuildContent();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			((Button)target).Click += Done_Click;
		}
		else
		{
			_contentLoaded = true;
		}
	}

	private FrameworkElement BuildContent()
	{
		Border shell = new Border
		{
			Background = Brush("#07101c"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0)
		};

		Grid root = new Grid();
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(52.0) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		shell.Child = root;

		root.Children.Add(CreateTitleBar());

		Grid content = new Grid
		{
			Margin = new Thickness(18.0, 0.0, 18.0, 0.0)
		};
		content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		Grid.SetRow(content, 1);
		Grid.SetRowSpan(content, 2);
		root.Children.Add(content);

		Border tools = new Border
		{
			Background = Brush("#111d2a"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(14.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
		Grid filters = new Grid();
		filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14.0) });
		filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220.0) });
		filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14.0) });
		filters.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		tools.Child = filters;

		TextBox search = CreateTextBox("SearchText");
		search.MinWidth = 320.0;
		search.ToolTip = "Search settings";
		filters.Children.Add(search);

		ComboBox category = CreateComboBox();
		category.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Categories"));
		category.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedCategory")
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		Grid.SetColumn(category, 2);
		filters.Children.Add(category);

		Button reset = CreateButton("Reset Selected", "#203249");
		reset.SetBinding(Button.CommandProperty, new Binding("ResetSelectedCommand"));
		Grid.SetColumn(reset, 4);
		filters.Children.Add(reset);
		content.Children.Add(tools);

		Grid body = new Grid();
		body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star), MinWidth = 620.0 });
		body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18.0) });
		body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360.0), MinWidth = 320.0 });
		Grid.SetRow(body, 1);
		content.Children.Add(body);

		DataGrid settings = CreateSettingsGrid();
		settings.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("FilteredSettings"));
		settings.SetBinding(Selector.SelectedItemProperty, new Binding("SelectedSetting")
		{
			Mode = BindingMode.TwoWay
		});
		body.Children.Add(settings);

		Border details = CreateDetailsPanel();
		Grid.SetColumn(details, 2);
		body.Children.Add(details);

		Border footer = new Border
		{
			Background = Brush("#08111b"),
			BorderBrush = Brush("#25384f"),
			BorderThickness = new Thickness(0.0, 1.0, 0.0, 0.0),
			Padding = new Thickness(18.0, 12.0, 18.0, 12.0)
		};
		Grid footerGrid = new Grid();
		footerGrid.ColumnDefinitions.Add(new ColumnDefinition());
		footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		footer.Child = footerGrid;
		_statusText = new TextBlock
		{
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_statusText.SetBinding(TextBlock.TextProperty, new Binding("FilteredSettings.Count") { StringFormat = "{0} settings shown" });
		footerGrid.Children.Add(_statusText);

		StackPanel footerButtons = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};

		Button save = CreateButton("Save", "#00a875");
		save.MinWidth = 120.0;
		save.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
		save.Click += Save_Click;
		footerButtons.Children.Add(save);
		Button done = CreateButton("Done", "#4658ff");
		done.MinWidth = 120.0;
		done.Click += Done_Click;
		footerButtons.Children.Add(done);
		Grid.SetColumn(footerButtons, 1);
		footerGrid.Children.Add(footerButtons);
		Grid.SetRow(footer, 3);
		root.Children.Add(footer);

		return shell;
	}

	private UIElement CreateTitleBar()
	{
		Grid bar = new Grid
		{
			Background = Brush("#08111f")
		};
		bar.ColumnDefinitions.Add(new ColumnDefinition());
		bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		bar.MouseLeftButtonDown += delegate
		{
			try
			{
				DragMove();
			}
			catch
			{
			}
		};

		StackPanel title = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(18.0, 0.0, 0.0, 0.0)
		};
		title.Children.Add(new TextBlock
		{
			Text = "INI",
			Foreground = Brush("#4658ff"),
			FontSize = 19.0,
			FontWeight = FontWeights.Black,
			Margin = new Thickness(0.0, 0.0, 12.0, 0.0)
		});
		StackPanel copy = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		copy.Children.Add(new TextBlock
		{
			Text = "INI Settings Editor",
			Foreground = Brushes.White,
			FontSize = 16.0,
			FontWeight = FontWeights.Bold
		});
		copy.Children.Add(new TextBlock
		{
			Text = "Search, inspect, and edit server configuration values",
			Foreground = Brush("#9fb8d6"),
			FontSize = 11.0,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		});
		title.Children.Add(copy);
		bar.Children.Add(title);

		Button close = CreateChromeButton("x");
		close.Click += delegate { Close(); };
		Grid.SetColumn(close, 1);
		bar.Children.Add(close);
		return bar;
	}

	private static DataGrid CreateSettingsGrid()
	{
		DataGrid grid = new DataGrid
		{
			AutoGenerateColumns = false,
			CanUserAddRows = false,
			CanUserDeleteRows = false,
			HeadersVisibility = DataGridHeadersVisibility.Column,
			GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
			SelectionMode = DataGridSelectionMode.Single,
			SelectionUnit = DataGridSelectionUnit.FullRow,
			Background = Brush("#0b1422"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
			HorizontalGridLinesBrush = Brush("#1e3149"),
			RowBackground = Brush("#0b1422"),
			AlternatingRowBackground = Brush("#101d2c"),
			RowHeight = 34.0,
			ColumnHeaderHeight = 38.0,
			EnableRowVirtualization = true,
			EnableColumnVirtualization = true,
			ColumnHeaderStyle = CreateGridHeaderStyle(),
			CellStyle = CreateGridCellStyle(),
			RowStyle = CreateGridRowStyle(),
			Resources =
			{
				[SystemColors.HighlightBrushKey] = Brush("#28486f"),
				[SystemColors.HighlightTextBrushKey] = Brushes.White,
				[SystemColors.ControlBrushKey] = Brush("#0b1422"),
				[SystemColors.ControlTextBrushKey] = Brushes.White
			}
		};
		Style textStyle = CreateGridTextStyle();
		grid.Columns.Add(new DataGridTextColumn { Header = "Category", Binding = new Binding("Category"), Width = new DataGridLength(130.0), ElementStyle = textStyle });
		grid.Columns.Add(new DataGridTextColumn { Header = "Setting", Binding = new Binding("DisplayName"), Width = new DataGridLength(1.25, DataGridLengthUnitType.Star), ElementStyle = textStyle });
		grid.Columns.Add(new DataGridTextColumn { Header = "Variable", Binding = new Binding("VariableName"), Width = new DataGridLength(1.0, DataGridLengthUnitType.Star), ElementStyle = textStyle });
		grid.Columns.Add(new DataGridTextColumn
		{
			Header = "Value",
			Binding = new Binding("Value")
			{
				Mode = BindingMode.TwoWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			},
			Width = new DataGridLength(1.0, DataGridLengthUnitType.Star),
			ElementStyle = textStyle,
			EditingElementStyle = CreateGridEditingTextStyle()
		});
		grid.Columns.Add(new DataGridTextColumn { Header = "File", Binding = new Binding("FileName"), Width = new DataGridLength(110.0), ElementStyle = textStyle });
		return grid;
	}

	private static Border CreateDetailsPanel()
	{
		Border panel = new Border
		{
			Background = Brush("#0b1422"),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(16.0)
		};
		ScrollViewer scroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		StackPanel stack = new StackPanel();
		stack.Children.Add(CreateDetailText("DisplayName", 22.0, FontWeights.Bold, Brushes.White));
		stack.Children.Add(CreateDetailText("Description", 13.0, FontWeights.Normal, Brush("#b8c8df"), new Thickness(0.0, 8.0, 0.0, 16.0)));
		stack.Children.Add(CreateDivider());
		stack.Children.Add(CreateLabeledValue("Variable", "VariableName"));
		stack.Children.Add(CreateLabeledValue("File", "FileName"));
		stack.Children.Add(CreateLabeledValue("Section", "Section"));
		stack.Children.Add(CreateLabeledValue("Type", "DataType"));
		stack.Children.Add(CreateLabeledValue("Default", "DisplayDefaultValue"));
		stack.Children.Add(CreateLabeledValue("Range", "ValidRange"));
		stack.Children.Add(CreateLabeledValue("Requires restart", "RequiresRestart"));
		stack.Children.Add(CreateValueEditor());
		scroll.Content = stack;
		panel.Child = scroll;
		return panel;
	}

	private static UIElement CreateDivider()
	{
		return new Border
		{
			Height = 1.0,
			Background = Brush("#203249"),
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
	}

	private static FrameworkElement CreateValueEditor()
	{
		StackPanel group = new StackPanel { Margin = new Thickness(0.0, 18.0, 0.0, 0.0) };
		group.Children.Add(new TextBlock
		{
			Text = "Value",
			Foreground = Brush("#9fb8d6"),
			FontSize = 12.0,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		});
		TextBox value = CreateTextBox("SelectedSetting.Value");
		value.AcceptsReturn = true;
		value.TextWrapping = TextWrapping.Wrap;
		value.MinHeight = 96.0;
		value.VerticalContentAlignment = VerticalAlignment.Top;
		group.Children.Add(value);
		return group;
	}

	private static FrameworkElement CreateLabeledValue(string label, string path)
	{
		Grid row = new Grid { Margin = new Thickness(0.0, 0.0, 0.0, 10.0) };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(118.0) });
		row.ColumnDefinitions.Add(new ColumnDefinition());
		row.Children.Add(new TextBlock
		{
			Text = label,
			Foreground = Brush("#9fb8d6"),
			FontSize = 11.0,
			FontWeight = FontWeights.SemiBold
		});
		TextBlock value = new TextBlock
		{
			Foreground = Brushes.White,
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap
		};
		value.SetBinding(TextBlock.TextProperty, new Binding("SelectedSetting." + path));
		Grid.SetColumn(value, 1);
		row.Children.Add(value);
		return row;
	}

	private static TextBlock CreateDetailText(string path, double size, FontWeight weight, Brush foreground, Thickness? margin = null)
	{
		TextBlock text = new TextBlock
		{
			Foreground = foreground,
			FontSize = size,
			FontWeight = weight,
			TextWrapping = TextWrapping.Wrap,
			Margin = margin ?? new Thickness(0.0)
		};
		text.SetBinding(TextBlock.TextProperty, new Binding("SelectedSetting." + path));
		return text;
	}

	private static TextBox CreateTextBox(string bindingPath)
	{
		TextBox box = new TextBox
		{
			Background = Brush("#0b1422"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			CaretBrush = Brushes.White,
			Padding = new Thickness(10.0, 7.0, 10.0, 7.0),
			MinHeight = 34.0,
			VerticalContentAlignment = VerticalAlignment.Center,
			Resources =
			{
				[SystemColors.HighlightBrushKey] = Brush("#4658ff"),
				[SystemColors.HighlightTextBrushKey] = Brushes.White
			}
		};
		box.SetBinding(TextBox.TextProperty, new Binding(bindingPath)
		{
			Mode = BindingMode.TwoWay,
			UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
		});
		return box;
	}

	private static ComboBox CreateComboBox()
	{
		ComboBox combo = new ComboBox
		{
			Height = 34.0,
			Background = Brush("#0b1422"),
			Foreground = Brushes.White,
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			Padding = new Thickness(10.0, 0.0, 34.0, 0.0),
			MinHeight = 34.0,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		ApplyDarkComboBox(combo);
		return combo;
	}

	private static Button CreateButton(string text, string color)
	{
		Button button = new Button
		{
			Content = text,
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderBrush = Brush(color),
			BorderThickness = new Thickness(1.0),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(14.0, 8.0, 14.0, 8.0),
			MinHeight = 34.0,
			Template = CreateButtonTemplate(),
			Cursor = Cursors.Hand
		};
		return button;
	}

	private static Button CreateChromeButton(string text)
	{
		return new Button
		{
			Content = text,
			Width = 46.0,
			Height = 36.0,
			Margin = new Thickness(0.0, 8.0, 10.0, 8.0),
			Background = Brushes.Transparent,
			Foreground = Brush("#d8e8ff"),
			BorderThickness = new Thickness(0.0),
			FontSize = 16.0,
			FontWeight = FontWeights.Bold,
			Template = CreateButtonTemplate(),
			Cursor = Cursors.Hand
		};
	}

	private static Style CreateGridHeaderStyle()
	{
		Style style = new Style(typeof(DataGridColumnHeader));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#101f31")));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#d8e8ff")));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#28445f")));
		style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.Bold));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8.0, 8.0, 8.0, 8.0)));
		return style;
	}

	private static Style CreateGridCellStyle()
	{
		Style style = new Style(typeof(DataGridCell));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8.0, 4.0, 8.0, 4.0)));
		Trigger selected = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#2b5280")));
		selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Triggers.Add(selected);
		return style;
	}

	private static Style CreateGridRowStyle()
	{
		Style style = new Style(typeof(DataGridRow));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 34.0));
		Trigger selected = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
		selected.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#2b5280")));
		style.Triggers.Add(selected);

		Trigger hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		hover.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#13243a")));
		style.Triggers.Add(hover);
		return style;
	}

	private static Style CreateGridTextStyle()
	{
		Style style = new Style(typeof(TextBlock));
		style.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
		style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
		style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0.0)));
		return style;
	}

	private static Style CreateGridEditingTextStyle()
	{
		Style style = new Style(typeof(TextBox));
		style.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#07101c")));
		style.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
		style.Setters.Add(new Setter(Control.BorderBrushProperty, Brush("#4658ff")));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(6.0, 2.0, 6.0, 2.0)));
		style.Setters.Add(new Setter(TextBox.CaretBrushProperty, Brushes.White));
		return style;
	}

	private static ControlTemplate CreateButtonTemplate()
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

	private static void ApplyDarkComboBox(ComboBox combo)
	{
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

	private static SolidColorBrush Brush(string color)
	{
		SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
		brush.Freeze();
		return brush;
	}
}
