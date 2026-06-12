using System.Collections.Generic;

namespace ServerManager.Models;

public class AsaServerDiscoveryResult
{
	public bool IsValidInstall { get; set; }

	public string SelectedDirectory { get; set; } = string.Empty;

	public string InstallDirectory { get; set; } = string.Empty;

	public string ExecutablePath { get; set; } = string.Empty;

	public string ExecutableName { get; set; } = string.Empty;

	public string ShooterGameDirectory { get; set; } = string.Empty;

	public string SavedDirectory { get; set; } = string.Empty;

	public string ConfigDirectory { get; set; } = string.Empty;

	public string GameUserSettingsPath { get; set; } = string.Empty;

	public string GameIniPath { get; set; } = string.Empty;

	public string EngineIniPath { get; set; } = string.Empty;

	public string SavedArksDirectory { get; set; } = string.Empty;

	public string LogsDirectory { get; set; } = string.Empty;

	public string ModsDirectory { get; set; } = string.Empty;

	public List<string> Errors { get; } = new List<string>();

	public List<string> Warnings { get; } = new List<string>();
}
