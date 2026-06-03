using System;

namespace ServerManager.Models;

public class SchedulerTask
{
	public Guid Id { get; set; } = Guid.NewGuid();


	public string Name { get; set; } = string.Empty;


	public string CronExpression { get; set; } = "0 0 * * *";


	public bool Enabled { get; set; } = true;


	public string ActionType { get; set; } = "Restart";


	public Guid? ServerId { get; set; }

	public int WarningSeconds { get; set; } = 60;

}
