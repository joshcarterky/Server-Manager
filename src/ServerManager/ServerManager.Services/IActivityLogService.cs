using System.Collections.ObjectModel;

namespace ServerManager.Services;

public interface IActivityLogService
{
	ObservableCollection<ActivityLogEntry> Entries { get; }

	void Info(string message);

	void Warning(string message);

	void Error(string message);

	void Clear();
}
