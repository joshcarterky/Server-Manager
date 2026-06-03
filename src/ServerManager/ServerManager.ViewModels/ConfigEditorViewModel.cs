namespace ServerManager.ViewModels;

public class ConfigEditorViewModel : PlaceholderModuleViewModel
{
	public ConfigEditorViewModel()
		: base("INI Configuration Editor", "Visual editing for Game.ini, GameUserSettings.ini, and Engine.ini backed by SQLite metadata.", "Categorized settings with search", "Typed controls for toggles, sliders, dropdowns, and numeric values", "Default values, reset actions, validation, and restart-required indicators", "Metadata database for variable descriptions, ranges, files, and INI sections")
	{
	}
}
