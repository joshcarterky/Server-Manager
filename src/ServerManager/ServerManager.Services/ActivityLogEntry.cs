using System;

namespace ServerManager.Services;

public class ActivityLogEntry
{
	public DateTime Timestamp { get; init; } = DateTime.Now;


	public string Level { get; init; } = "Info";


	public string Message { get; init; } = string.Empty;

}
