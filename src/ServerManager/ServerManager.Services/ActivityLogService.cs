using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace ServerManager.Services;

public class ActivityLogService : IActivityLogService
{
	private const int MaxEntries = 500;

	public ObservableCollection<ActivityLogEntry> Entries { get; } = new ObservableCollection<ActivityLogEntry>();


	public void Info(string message)
	{
		Add("Info", message);
	}

	public void Warning(string message)
	{
		Add("Warning", message);
	}

	public void Error(string message)
	{
		Add("Error", message);
	}

	public void Clear()
	{
		RunOnUiThread(Entries.Clear);
	}

	private void Add(string level, string message)
	{
		string level2 = level;
		string message2 = message;
		RunOnUiThread(delegate
		{
			Entries.Insert(0, new ActivityLogEntry
			{
				Timestamp = DateTime.Now,
				Level = level2,
				Message = message2
			});
			while (Entries.Count > 500)
			{
				Entries.RemoveAt(Entries.Count - 1);
			}
		});
	}

	private static void RunOnUiThread(Action action)
	{
		Application current = Application.Current;
		Dispatcher val = ((current != null) ? ((DispatcherObject)current).Dispatcher : null);
		if (val == null || val.CheckAccess())
		{
			action();
		}
		else
		{
			val.BeginInvoke((Delegate)action, Array.Empty<object>());
		}
	}
}
