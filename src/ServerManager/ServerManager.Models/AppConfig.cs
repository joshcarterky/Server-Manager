using System.Collections.Generic;

namespace ServerManager.Models;

public class AppConfig
{
	public string SteamCmdDirectory { get; set; } = string.Empty;


	public string CurseForgeApiKey { get; set; } = string.Empty;


	public string BackupDirectory { get; set; } = string.Empty;


	public string PluginDirectory { get; set; } = string.Empty;


	public bool MinimizeToTray { get; set; } = true;


	public string UiBackgroundColor { get; set; } = "#07101c";


	public string UiPanelColor { get; set; } = "#111d2a";


	public string UiInputColor { get; set; } = "#0b1422";


	public string UiAccentColor { get; set; } = "#4658ff";


	public string UiTextColor { get; set; } = "#ffffff";

	public string UpdateManifestUrl { get; set; } = string.Empty;

	public NotificationConfig NotificationSettings { get; set; } = new NotificationConfig();


	public List<ServerInstance> Servers { get; set; } = new List<ServerInstance>();


	public List<SchedulerTask> ScheduledTasks { get; set; } = new List<SchedulerTask>();


	public List<ClusterDefinition> Clusters { get; set; } = new List<ClusterDefinition>();

}
