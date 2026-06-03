namespace ServerManager.Models;

public class NotificationConfig
{
	public string DiscordWebhookUrl { get; set; } = string.Empty;


	public bool SendRestartNotifications { get; set; } = true;


	public bool SendCrashAlerts { get; set; } = true;


	public bool SendUpdateAlerts { get; set; } = true;

}
