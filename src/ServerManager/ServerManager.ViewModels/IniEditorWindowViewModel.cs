using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class IniEditorWindowViewModel : ObservableObject
{
	private readonly ObservableCollection<IniSettingViewModel> _allSettings;

	private string _searchText = string.Empty;

	private string _selectedCategory = "All";

	private IniSettingViewModel? _selectedSetting;

	public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>();


	public ObservableCollection<IniSettingViewModel> FilteredSettings { get; } = new ObservableCollection<IniSettingViewModel>();


	public IRelayCommand ResetSelectedCommand { get; }

	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			SetProperty(ref _searchText, value, "SearchText");
			ApplyFilter();
		}
	}

	public string SelectedCategory
	{
		get
		{
			return _selectedCategory;
		}
		set
		{
			SetProperty(ref _selectedCategory, value, "SelectedCategory");
			ApplyFilter();
		}
	}

	public IniSettingViewModel? SelectedSetting
	{
		get
		{
			return _selectedSetting;
		}
		set
		{
			SetProperty(ref _selectedSetting, value, "SelectedSetting");
			ResetSelectedCommand.NotifyCanExecuteChanged();
		}
	}

	public IniEditorWindowViewModel(ObservableCollection<IniSettingViewModel> settings)
	{
		_allSettings = settings;
		ResetSelectedCommand = new RelayCommand(ResetSelected, () => SelectedSetting != null);
		Categories.Add("All");
		foreach (string item in from x in settings.Select((IniSettingViewModel x) => x.Category).Distinct()
			orderby x
			select x)
		{
			Categories.Add(item);
		}
		ApplyFilter();
	}

	private void ApplyFilter()
	{
		IEnumerable<IniSettingViewModel> source = _allSettings.AsEnumerable();
		if (SelectedCategory != "All")
		{
			source = source.Where((IniSettingViewModel x) => x.Category == SelectedCategory);
		}
		if (!string.IsNullOrWhiteSpace(SearchText))
		{
			source = source.Where((IniSettingViewModel x) => x.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || x.VariableName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
		}
		FilteredSettings.Clear();
		foreach (IniSettingViewModel item in from x in source
			orderby x.Category, x.DisplayName
			select x)
		{
			FilteredSettings.Add(item);
		}
		SelectedSetting = FilteredSettings.FirstOrDefault();
	}

	private void ResetSelected()
	{
		if (SelectedSetting != null)
		{
			SelectedSetting.Value = SelectedSetting.DefaultValue;
		}
	}
}
