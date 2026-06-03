using CommunityToolkit.Mvvm.ComponentModel;

namespace ServerManager.ViewModels;

public class MenuItemViewModel : ObservableObject
{
	public string Title { get; set; }

	public string Subtitle { get; set; }

	public string Icon { get; set; }

	public object Page { get; set; }

	public MenuItemViewModel(string title, string subtitle, string icon, object page)
	{
		Title = title;
		Subtitle = subtitle;
		Icon = icon;
		Page = page;
	}
}
