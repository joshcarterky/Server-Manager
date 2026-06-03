namespace ServerManager.ViewModels;

public class ValidationViewModel : PlaceholderModuleViewModel
{
	public ValidationViewModel()
		: base("Smart Validation", "Automatic checks for server, cluster, mod, port, and INI configuration problems.", "Duplicate port detection", "Invalid map and missing save folder checks", "Broken INI syntax and unsupported settings", "Duplicate mods, missing dependencies, and conflict warnings", "Auto-fix suggestions and severity highlighting")
	{
	}
}
