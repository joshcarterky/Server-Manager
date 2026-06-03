using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class SchedulerViewModel : ObservableObject
{
	private readonly ISchedulerService _schedulerService;

	public ObservableCollection<SchedulerTask> ScheduledTasks { get; } = new ObservableCollection<SchedulerTask>();


	public SchedulerTask NewTask { get; } = new SchedulerTask();


	public IAsyncRelayCommand AddTaskCommand { get; }

	public SchedulerViewModel(ISchedulerService schedulerService)
	{
		_schedulerService = schedulerService;
		AddTaskCommand = new AsyncRelayCommand(AddTaskAsync);
		Task.Run(() => LoadTasksAsync()).GetAwaiter().GetResult();
	}

	private async Task LoadTasksAsync()
	{
		await foreach (SchedulerTask item in _schedulerService.GetScheduledTasksAsync())
		{
			ScheduledTasks.Add(item);
		}
	}

	private async Task AddTaskAsync()
	{
		SchedulerTask task = new SchedulerTask
		{
			Name = NewTask.Name,
			CronExpression = NewTask.CronExpression,
			ActionType = NewTask.ActionType,
			Enabled = true
		};
		await _schedulerService.AddTaskAsync(task).ConfigureAwait(continueOnCapturedContext: false);
		ScheduledTasks.Add(task);
	}
}
