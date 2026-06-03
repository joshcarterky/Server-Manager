using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ServerManager.ViewModels;

namespace ServerManager.Views;

public class TemplatesView : UserControl
{
	private readonly TemplatesViewModel _viewModel;

	private readonly StackPanel _templatePanel;

	private readonly StackPanel _previewPanel;

	private readonly TextBlock _statusText;

	private readonly Button _renameTemplateButton;

	private readonly Button _deleteTemplateButton;

	public TemplatesView(TemplatesViewModel viewModel)
	{
		_viewModel = viewModel;
		DataContext = viewModel;
		Grid root = new Grid
		{
			Background = BrushFrom("#0b121b"),
			Margin = new Thickness(0.0)
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		StackPanel header = new StackPanel
		{
			Margin = new Thickness(28.0, 24.0, 28.0, 16.0)
		};
		header.Children.Add(new TextBlock
		{
			Text = "Server Templates",
			Foreground = Brushes.White,
			FontSize = 24.0,
			FontWeight = FontWeights.SemiBold
		});
		header.Children.Add(new TextBlock
		{
			Text = "Create a ready-to-edit server from a preset, or save your current server settings as a custom template.",
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 13.0,
			Margin = new Thickness(0.0, 5.0, 0.0, 0.0)
		});
		root.Children.Add(header);

		Grid content = new Grid
		{
			Margin = new Thickness(28.0, 0.0, 28.0, 18.0)
		};
		content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.0, GridUnitType.Star) });
		content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18.0) });
		content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		ScrollViewer templateScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		_templatePanel = new StackPanel();
		templateScroll.Content = _templatePanel;
		content.Children.Add(templateScroll);

		Border previewFrame = new Border
		{
			Background = BrushFrom("#111d2a"),
			BorderBrush = BrushFrom("#25384f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(16.0)
		};
		Grid.SetColumn(previewFrame, 2);
		StackPanel previewRoot = new StackPanel();
		previewRoot.Children.Add(new TextBlock
		{
			Text = "Template Preview",
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.SemiBold
		});
		TextBlock selectedName = new TextBlock
		{
			Foreground = BrushFrom("#9fb8d6"),
			FontSize = 13.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 14.0)
		};
		selectedName.SetBinding(TextBlock.TextProperty, "SelectedTemplate.Name");
		previewRoot.Children.Add(selectedName);
		_previewPanel = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 18.0)
		};
		previewRoot.Children.Add(_previewPanel);
		previewRoot.Children.Add(CreateActionButton("Create Server From Template", delegate
		{
			if (_viewModel.ApplyTemplateCommand.CanExecute(_viewModel.SelectedTemplate))
			{
				_viewModel.ApplyTemplateCommand.Execute(_viewModel.SelectedTemplate);
			}
		}, "#5d7cff"));
		previewRoot.Children.Add(CreateActionButton("Save Selected Server as Template", delegate
		{
			if (_viewModel.SaveSelectedServerAsTemplateCommand.CanExecute(null))
			{
				_viewModel.SaveSelectedServerAsTemplateCommand.Execute(null);
			}
		}, "#203249"));
		_renameTemplateButton = CreateActionButton("Rename Custom Template", async delegate
		{
			string? newName = ShowRenameTemplateDialog(_viewModel.SelectedTemplate?.Name ?? string.Empty);
			if (newName != null)
			{
				await _viewModel.RenameSelectedTemplateAsync(newName);
			}
		}, "#203249");
		previewRoot.Children.Add(_renameTemplateButton);
		_deleteTemplateButton = CreateActionButton("Delete Custom Template", delegate
		{
			if (_viewModel.DeleteSelectedTemplateCommand.CanExecute(null))
			{
				_viewModel.DeleteSelectedTemplateCommand.Execute(null);
			}
		}, "#9b2f3b");
		previewRoot.Children.Add(_deleteTemplateButton);
		previewRoot.Children.Add(CreateActionButton("Refresh Templates", delegate
		{
			if (_viewModel.RefreshTemplatesCommand.CanExecute(null))
			{
				_viewModel.RefreshTemplatesCommand.Execute(null);
			}
		}, "#203249"));
		previewFrame.Child = previewRoot;
		content.Children.Add(previewFrame);
		Grid.SetRow(content, 1);
		root.Children.Add(content);

		Border statusBar = new Border
		{
			Background = BrushFrom("#08111b"),
			BorderBrush = BrushFrom("#25384f"),
			BorderThickness = new Thickness(1.0, 1.0, 0.0, 0.0),
			Padding = new Thickness(28.0, 10.0, 28.0, 10.0)
		};
		_statusText = new TextBlock
		{
			Foreground = BrushFrom("#d8e8ff"),
			FontSize = 12.0
		};
		statusBar.Child = _statusText;
		Grid.SetRow(statusBar, 2);
		root.Children.Add(statusBar);
		Content = root;

		_viewModel.PropertyChanged += ViewModel_PropertyChanged;
		_viewModel.Templates.CollectionChanged += delegate { RebuildTemplateCards(); };
		_viewModel.PreviewLines.CollectionChanged += delegate { RebuildPreview(); };
		RebuildTemplateCards();
		RebuildPreview();
		UpdateStatus();
		UpdateCustomTemplateButtons();
	}

	private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "SelectedTemplate")
		{
			RebuildTemplateCards();
			RebuildPreview();
			UpdateCustomTemplateButtons();
		}
		if (e.PropertyName == "StatusText")
		{
			UpdateStatus();
		}
	}

	private void RebuildTemplateCards()
	{
		_templatePanel.Children.Clear();
		foreach (var gameGroup in _viewModel.Templates
			.GroupBy(template => template.GameDisplayName)
			.OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
		{
			_templatePanel.Children.Add(CreateGameGroupHeader(gameGroup.Key, gameGroup.Count()));
			WrapPanel groupPanel = new WrapPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(0.0, 0.0, 0.0, 18.0)
			};
			foreach (ServerTemplatePreset template in gameGroup
				.OrderBy(template => template.Category, StringComparer.OrdinalIgnoreCase)
				.ThenBy(template => template.Name, StringComparer.OrdinalIgnoreCase))
			{
				groupPanel.Children.Add(CreateTemplateCard(template));
			}
			_templatePanel.Children.Add(groupPanel);
		}
	}

	private Border CreateGameGroupHeader(string gameName, int templateCount)
	{
		Grid header = new Grid();
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		header.Children.Add(new TextBlock
		{
			Text = gameName,
			Foreground = Brushes.White,
			FontSize = 18.0,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 0.0, 0.0, 2.0)
		});
		Border countBadge = new Border
		{
			Background = BrushFrom("#18263a"),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(9.0, 4.0, 9.0, 4.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		countBadge.Child = new TextBlock
		{
			Text = templateCount == 1 ? "1 template" : templateCount + " templates",
			Foreground = BrushFrom("#d8e8ff"),
			FontSize = 11.0,
			FontWeight = FontWeights.SemiBold
		};
		Grid.SetColumn(countBadge, 1);
		header.Children.Add(countBadge);

		return new Border
		{
			Background = BrushFrom("#0d1824"),
			BorderBrush = BrushFrom("#25384f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
			Margin = new Thickness(0.0, 0.0, 14.0, 12.0),
			Child = header
		};
	}

	private Border CreateTemplateCard(ServerTemplatePreset template)
	{
		Border card = new Border
		{
			Width = 292.0,
			MinHeight = 178.0,
			Background = BrushFrom(_viewModel.SelectedTemplate == template ? "#172842" : "#111d2a"),
			BorderBrush = BrushFrom(_viewModel.SelectedTemplate == template ? "#5d7cff" : "#25384f"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(14.0),
			Margin = new Thickness(0.0, 0.0, 14.0, 14.0),
			Cursor = System.Windows.Input.Cursors.Hand
		};
		card.MouseLeftButtonUp += delegate { _viewModel.SelectedTemplate = template; };
		StackPanel body = new StackPanel();
		Grid titleRow = new Grid();
		titleRow.ColumnDefinitions.Add(new ColumnDefinition());
		titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		titleRow.Children.Add(new TextBlock
		{
			Text = template.Name,
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 16.0
		});
		Border category = new Border
		{
			Background = BrushFrom(template.IsCustom ? "#254d34" : "#18263a"),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(8.0, 4.0, 8.0, 4.0)
		};
		category.Child = new TextBlock
		{
			Text = template.Category,
			Foreground = BrushFrom("#d8e8ff"),
			FontSize = 11.0,
			FontWeight = FontWeights.SemiBold
		};
		Grid.SetColumn(category, 1);
		titleRow.Children.Add(category);
		body.Children.Add(titleRow);
		body.Children.Add(new TextBlock
		{
			Text = template.Description,
			Foreground = BrushFrom("#9fb8d6"),
			TextWrapping = TextWrapping.Wrap,
			FontSize = 12.0,
			Margin = new Thickness(0.0, 9.0, 0.0, 12.0)
		});
		body.Children.Add(CreateMetricRow("Type", template.Category, "Map", template.MapName));
		body.Children.Add(CreateMetricRow("Players", template.MaxPlayers.ToString(), "XP", template.XPMultiplier + "x"));
		body.Children.Add(CreateMetricRow("Harvest", template.HarvestMultiplier + "x", "Taming", template.TamingSpeedMultiplier + "x"));
		Button applyButton = CreateSmallButton("Use Template", "#5d7cff");
		applyButton.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		applyButton.Click += delegate
		{
			_viewModel.SelectedTemplate = template;
			if (_viewModel.ApplyTemplateCommand.CanExecute(template))
			{
				_viewModel.ApplyTemplateCommand.Execute(template);
			}
		};
		body.Children.Add(applyButton);
		if (template.IsCustom)
		{
			Button renameButton = CreateSmallButton("Rename Custom Template", "#203249");
			renameButton.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
			renameButton.Click += async delegate
			{
				_viewModel.SelectedTemplate = template;
				string? newName = ShowRenameTemplateDialog(template.Name);
				if (newName != null)
				{
					await _viewModel.RenameSelectedTemplateAsync(newName);
				}
			};
			body.Children.Add(renameButton);
			Button deleteButton = CreateSmallButton("Delete Custom Template", "#9b2f3b");
			deleteButton.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
			deleteButton.Click += delegate
			{
				_viewModel.SelectedTemplate = template;
				if (_viewModel.DeleteSelectedTemplateCommand.CanExecute(null))
				{
					_viewModel.DeleteSelectedTemplateCommand.Execute(null);
				}
			};
			body.Children.Add(deleteButton);
		}
		card.Child = body;
		return card;
	}

	private void RebuildPreview()
	{
		_previewPanel.Children.Clear();
		foreach (string line in _viewModel.PreviewLines)
		{
			_previewPanel.Children.Add(new TextBlock
			{
				Text = line,
				Foreground = BrushFrom("#d8e8ff"),
				FontSize = 13.0,
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
			});
		}
	}

	private Grid CreateMetricRow(string leftLabel, string leftValue, string rightLabel, string rightValue)
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.Children.Add(CreateMetricText(leftLabel, leftValue));
		StackPanel right = CreateMetricText(rightLabel, rightValue);
		Grid.SetColumn(right, 1);
		grid.Children.Add(right);
		return grid;
	}

	private StackPanel CreateMetricText(string label, string value)
	{
		StackPanel stack = new StackPanel();
		stack.Children.Add(new TextBlock
		{
			Text = label.ToUpperInvariant(),
			Foreground = BrushFrom("#8fb0d0"),
			FontSize = 10.0
		});
		stack.Children.Add(new TextBlock
		{
			Text = value,
			Foreground = Brushes.White,
			FontWeight = FontWeights.SemiBold,
			FontSize = 12.0
		});
		return stack;
	}

	private Button CreateActionButton(string text, Action action, string color)
	{
		Button button = CreateSmallButton(text, color);
		button.HorizontalAlignment = HorizontalAlignment.Stretch;
		button.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		button.Click += delegate { action(); };
		return button;
	}

	private Button CreateSmallButton(string text, string color)
	{
		return new Button
		{
			Content = text,
			Background = BrushFrom(color),
			Foreground = Brushes.White,
			BorderBrush = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(12.0, 9.0, 12.0, 9.0),
			MinHeight = 34.0
		};
	}

	private void UpdateStatus()
	{
		_statusText.Text = _viewModel.StatusText;
	}

	private void UpdateCustomTemplateButtons()
	{
		Visibility visibility = _viewModel.SelectedTemplate?.IsCustom == true ? Visibility.Visible : Visibility.Collapsed;
		_renameTemplateButton.Visibility = visibility;
		_deleteTemplateButton.Visibility = visibility;
	}

	private string? ShowRenameTemplateDialog(string currentName)
	{
		Window dialog = new Window
		{
			Title = "Rename Template",
			Width = 360.0,
			Height = 178.0,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			ResizeMode = ResizeMode.NoResize,
			Background = BrushFrom("#111d2a"),
			Foreground = Brushes.White,
			Owner = Window.GetWindow(this)
		};
		StackPanel root = new StackPanel
		{
			Margin = new Thickness(18.0)
		};
		root.Children.Add(new TextBlock
		{
			Text = "Template Name",
			Foreground = BrushFrom("#d8e8ff"),
			FontSize = 13.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		});
		TextBox nameBox = new TextBox
		{
			Text = currentName,
			Background = BrushFrom("#0d1824"),
			Foreground = Brushes.White,
			BorderBrush = BrushFrom("#25384f"),
			Padding = new Thickness(8.0),
			MinHeight = 34.0
		};
		nameBox.SelectAll();
		root.Children.Add(nameBox);
		StackPanel buttons = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0)
		};
		Button cancelButton = CreateSmallButton("Cancel", "#203249");
		cancelButton.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
		cancelButton.Click += delegate { dialog.DialogResult = false; };
		Button saveButton = CreateSmallButton("Rename", "#5d7cff");
		saveButton.Click += delegate { dialog.DialogResult = true; };
		buttons.Children.Add(cancelButton);
		buttons.Children.Add(saveButton);
		root.Children.Add(buttons);
		dialog.Content = root;
		bool? result = dialog.ShowDialog();
		return result == true ? nameBox.Text.Trim() : null;
	}

	private static Brush BrushFrom(string color)
	{
		return (Brush)new BrushConverter().ConvertFromString(color);
	}
}
