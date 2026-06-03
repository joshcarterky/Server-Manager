using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServerManager.Models;

namespace ServerManager.Services;

public interface ISchedulerService
{
	Task InitializeAsync(AppConfig config);

	Task AddTaskAsync(SchedulerTask task);

	Task RemoveTaskAsync(Guid taskId);

	IAsyncEnumerable<SchedulerTask> GetScheduledTasksAsync();
}
