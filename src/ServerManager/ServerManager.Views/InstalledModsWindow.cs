using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ServerManager.Models;

namespace ServerManager.Views;

public class InstalledModsWindow : Window
{
	public InstalledModsWindow(string serverName, IEnumerable<ModEntry> mods)
	{
		Title = "Installed Mods - " + serverName;
		Width = 980.0;
		Height = 700.0;
		MinWidth = 760.0;
		MinHeight = 520.0;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = Brush("#0b1422");
		Content = BuildContent(serverName, mods.OrderBy(x => x.LoadOrder).ToList());
	}

	private UIElement BuildContent(string serverName, IReadOnlyList<ModEntry> mods)
	{
		Grid root = new Grid
		{
			Margin = new Thickness(18.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		Grid header = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 16.0)
		};
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel titleStack = new StackPanel();
		titleStack.Children.Add(new TextBlock
		{
			Text = "Installed Mods",
			Foreground = Brushes.White,
			FontSize = 24.0,
			FontWeight = FontWeights.Bold
		});
		titleStack.Children.Add(new TextBlock
		{
			Text = serverName + " - " + mods.Count + " mod" + (mods.Count == 1 ? string.Empty : "s"),
			Foreground = Brush("#9fb8d6"),
			FontSize = 13.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		});
		header.Children.Add(titleStack);

		Button close = Button("Close", "#203249");
		close.Width = 90.0;
		close.Height = 34.0;
		close.Click += delegate { Close(); };
		Grid.SetColumn(close, 1);
		header.Children.Add(close);
		root.Children.Add(header);

		ScrollViewer scroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		WrapPanel cards = new WrapPanel();
		if (mods.Count == 0)
		{
			cards.Children.Add(new TextBlock
			{
				Text = "No mods are added to this server yet.",
				Foreground = Brush("#d8e8ff"),
				FontSize = 15.0,
				Margin = new Thickness(4.0)
			});
		}
		foreach (ModEntry mod in mods)
		{
			cards.Children.Add(ModCard(mod));
		}
		scroll.Content = cards;
		Grid.SetRow(scroll, 1);
		root.Children.Add(scroll);
		return root;
	}

	private Border ModCard(ModEntry mod)
	{
		Border card = new Border
		{
			Width = 285.0,
			MinHeight = 330.0,
			Background = Brush("#101b2a"),
			BorderBrush = Brush("#263a58"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Margin = new Thickness(0.0, 0.0, 14.0, 14.0),
			Padding = new Thickness(12.0)
		};

		StackPanel stack = new StackPanel();
		stack.Children.Add(ModImage(mod));
		stack.Children.Add(new TextBlock
		{
			Text = string.IsNullOrWhiteSpace(mod.Title) ? "CurseForge Mod " + mod.WorkshopId : mod.Title,
			Foreground = Brushes.White,
			FontSize = 15.0,
			FontWeight = FontWeights.Bold,
			TextWrapping = TextWrapping.Wrap,
			MaxHeight = 46.0,
			Margin = new Thickness(0.0, 10.0, 0.0, 8.0)
		});
		stack.Children.Add(InfoLine("Project ID", mod.WorkshopId));
		stack.Children.Add(InfoLine("Author", mod.Author));
		stack.Children.Add(InfoLine("File", mod.LatestFileName));
		stack.Children.Add(InfoLine("Updated", mod.LastUpdatedText));
		stack.Children.Add(InfoLine("Downloads", mod.DownloadCountText));

		Button open = Button("Open CurseForge", "#4658ff");
		open.Height = 34.0;
		open.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		open.Click += delegate { OpenModLink(mod); };
		stack.Children.Add(open);

		card.Child = stack;
		return card;
	}

	private UIElement ModImage(ModEntry mod)
	{
		Border frame = new Border
		{
			Height = 120.0,
			Background = Brush("#071019"),
			BorderBrush = Brush("#28445f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(7.0),
			ClipToBounds = true
		};
		if (!string.IsNullOrWhiteSpace(mod.ThumbnailUrl) && Uri.TryCreate(mod.ThumbnailUrl, UriKind.Absolute, out Uri uri))
		{
			try
			{
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				image.UriSource = uri;
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				image.EndInit();
				frame.Child = new Image
				{
					Source = image,
					Stretch = Stretch.UniformToFill
				};
				return frame;
			}
			catch
			{
			}
		}
		frame.Child = new TextBlock
		{
			Text = "MOD",
			Foreground = Brush("#9fb8d6"),
			FontSize = 26.0,
			FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		return frame;
	}

	private TextBlock InfoLine(string label, string value)
	{
		return new TextBlock
		{
			Text = label + ": " + (string.IsNullOrWhiteSpace(value) ? "Unknown" : value),
			Foreground = Brush("#cfe0f5"),
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
		};
	}

	private static void OpenModLink(ModEntry mod)
	{
		string url = mod.ProjectUrl;
		if (string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(mod.WorkshopId))
		{
			url = "https://www.curseforge.com/ark-survival-ascended/search?search=" + Uri.EscapeDataString(mod.WorkshopId);
		}
		if (!string.IsNullOrWhiteSpace(url))
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
	}

	private Button Button(string text, string color)
	{
		Button button = new Button
		{
			Content = text,
			Background = Brush(color),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0.0),
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0)
		};
		button.Template = ButtonTemplate();
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
