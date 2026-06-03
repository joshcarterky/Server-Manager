using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServerManager.Models;
using NCrontab;

namespace ServerManager.Services;

public class SchedulerService : ISchedulerService, IDisposable
{
	private readonly IConfigService _configService;

	private readonly INotificationService _notificationService;

	private readonly ConcurrentDictionary<Guid, Timer> _timers = new ConcurrentDictionary<Guid, Timer>();

	private AppConfig? _config;

	public SchedulerService(IConfigService configService, INotificationService notificationService)
	{
		_configService = configService;
		_notificationService = notificationService;
	}

	public async Task InitializeAsync(AppConfig config)
	{
		_config = config;
		foreach (SchedulerTask item in config.ScheduledTasks.Where((SchedulerTask t) => t.Enabled))
		{
			ScheduleTask(item);
		}
		await Task.CompletedTask;
	}

	public async Task AddTaskAsync(SchedulerTask task)
	{
		if (_config == null)
		{
			_config = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_config.ScheduledTasks.Add(task);
		ScheduleTask(task);
	}

	public async Task RemoveTaskAsync(Guid taskId)
	{
		if (_config == null)
		{
			_config = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		SchedulerTask schedulerTask = _config.ScheduledTasks.FirstOrDefault((SchedulerTask x) => x.Id == taskId);
		if (schedulerTask != null)
		{
			_config.ScheduledTasks.Remove(schedulerTask);
		}
		if (_timers.TryRemove(taskId, out Timer value))
		{
			value.Dispose();
		}
	}

	public async IAsyncEnumerable<SchedulerTask> GetScheduledTasksAsync()
	{
		if (_config == null)
		{
			_config = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		foreach (SchedulerTask scheduledTask in _config.ScheduledTasks)
		{
			yield return scheduledTask;
		}
	}

	private void ScheduleTask(SchedulerTask task)
	{
		SchedulerTask task2 = task;
		if (task2.Enabled)
		{
			CrontabSchedule crontabSchedule;
			try
			{
				crontabSchedule = CrontabSchedule.Parse(task2.CronExpression);
			}
			catch
			{
				return;
			}
			TimeSpan timeSpan = crontabSchedule.GetNextOccurrence(DateTime.UtcNow) - DateTime.UtcNow;
			if (timeSpan < TimeSpan.Zero)
			{
				timeSpan = TimeSpan.FromSeconds(10.0);
			}
			Timer timer = new Timer(async delegate
			{
				await ExecuteTaskAsync(task2).ConfigureAwait(continueOnCapturedContext: false);
			}, null, timeSpan, Timeout.InfiniteTimeSpan);
			_timers.AddOrUpdate(task2.Id, timer, delegate(Guid _, Timer old)
			{
				old.Dispose();
				return timer;
			});
		}
	}

	private async Task ExecuteTaskAsync(SchedulerTask task)
	{
		SchedulerTask task2 = task;
		if (_config == null)
		{
			return;
		}
		if (task2.ServerId.HasValue)
		{
			ServerInstance serverInstance = _config.Servers.FirstOrDefault((ServerInstance x) => x.Id == task2.ServerId.Value);
			if (serverInstance != null)
			{
				await _notificationService.NotifyRestartAsync(serverInstance).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		Reschedule(task2);
	}

	private void Reschedule(SchedulerTask task)
	{
		if (task.Enabled)
		{
			CrontabSchedule crontabSchedule;
			try
			{
				crontabSchedule = CrontabSchedule.Parse(task.CronExpression);
			}
			catch
			{
				return;
			}
			TimeSpan dueTime = crontabSchedule.GetNextOccurrence(DateTime.UtcNow) - DateTime.UtcNow;
			if (_timers.TryGetValue(task.Id, out Timer value))
			{
				value.Change(dueTime, Timeout.InfiniteTimeSpan);
			}
			else
			{
				ScheduleTask(task);
			}
		}
	}

	public void Dispose()
	{
		foreach (Timer value in _timers.Values)
		{
			value.Dispose();
		}
	}
}
