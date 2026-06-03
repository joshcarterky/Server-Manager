using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace ServerManager.Views;

public class PlaceholderModuleView : UserControl, IComponentConnector
{
	private bool _contentLoaded;

	public PlaceholderModuleView()
	{
		InitializeComponent();
	}

	private UIElement BuildContent()
	{
		Grid root = new Grid
		{
			Background = new LinearGradientBrush(ColorFrom("#06111f"), ColorFrom("#0b1430"), 45.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		StackPanel header = new StackPanel
		{
			Margin = new Thickness(28.0, 28.0, 28.0, 20.0)
		};
		TextBlock title = new TextBlock
		{
			Foreground = Brushes.White,
			FontSize = 32.0,
			FontWeight = FontWeights.Bold
		};
		title.SetBinding(TextBlock.TextProperty, new Binding("Title"));
		header.Children.Add(title);
		TextBlock description = new TextBlock
		{
			Foreground = Brush("#9fb8d6"),
			FontSize = 14.0,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		description.SetBinding(TextBlock.TextProperty, new Binding("Description"));
		header.Children.Add(description);
		root.Children.Add(header);

		Border panel = new Border
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 28.0),
			Background = Brush("#aa0d1828"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(20.0)
		};
		Grid.SetRow(panel, 1);

		Grid content = new Grid();
		content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		TextBlock heading = new TextBlock
		{
			Text = "Planned Capabilities",
			Foreground = Brushes.White,
			FontSize = 20.0,
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
		content.Children.Add(heading);

		ItemsControl list = new ItemsControl();
		list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Capabilities"));
		list.ItemTemplate = CreateCapabilityTemplate();
		Grid.SetRow(list, 1);
		content.Children.Add(list);

		TextBlock status = new TextBlock
		{
			Foreground = Brush("#9fb8d6"),
			FontSize = 13.0,
			Margin = new Thickness(0.0, 18.0, 0.0, 0.0)
		};
		status.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
		Grid.SetRow(status, 2);
		content.Children.Add(status);

		panel.Child = content;
		root.Children.Add(panel);
		return root;
	}

	private static DataTemplate CreateCapabilityTemplate()
	{
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.SetValue(Border.BackgroundProperty, Brush("#111d2a"));
		border.SetValue(Border.BorderBrushProperty, Brush("#263a58"));
		border.SetValue(Border.BorderThicknessProperty, new Thickness(1.0));
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6.0));
		border.SetValue(Border.PaddingProperty, new Thickness(12.0));
		border.SetValue(Border.MarginProperty, new Thickness(0.0, 0.0, 0.0, 9.0));

		FrameworkElementFactory text = new FrameworkElementFactory(typeof(TextBlock));
		text.SetBinding(TextBlock.TextProperty, new Binding());
		text.SetValue(TextBlock.ForegroundProperty, Brushes.White);
		text.SetValue(TextBlock.FontSizeProperty, 13.0);
		text.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
		border.AppendChild(text);
		return new DataTemplate { VisualTree = border };
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Content = BuildContent();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		_contentLoaded = true;
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