using System.Collections.Generic;

namespace ServerManager.Models;

public class LaunchProfile
{
	public string ExecutablePath { get; set; } = string.Empty;

	public string WorkingDirectory { get; set; } = string.Empty;

	public string MapPackageName { get; set; } = "TheIsland_WP";

	public string MapUrl { get; set; } = string.Empty;

	public string Arguments { get; set; } = string.Empty;

	public List<string> ManagedArguments { get; set; } = new List<string>();

	public string CustomArguments { get; set; } = string.Empty;
}
