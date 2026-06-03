using System;

namespace ServerManager.Models;

public class ServerStatus
{
	public bool IsOnline { get; set; }

	public bool IsJoinable { get; set; }

	public int PlayerCount { get; set; }

	public string CurrentMap { get; set; } = string.Empty;


	public double CpuUsage { get; set; }

	public long MemoryUsageMB { get; set; }

	public int PingMs { get; set; }

	public TimeSpan Uptime { get; set; }

	public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

}
