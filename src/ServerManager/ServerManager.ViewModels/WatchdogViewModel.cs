namespace ServerManager.ViewModels;

public class WatchdogViewModel : PlaceholderModuleViewModel
{
	public WatchdogViewModel()
		: base("Watchdog", "Crash recovery and health monitoring for local and remote ASA servers.", "Detect crashes, freezes, failed saves, high memory use, and RCON timeouts", "Restart servers automatically after failure", "Create emergency backups before recovery", "Send Discord alerts and preserve crash logs")
	{
	}
}
