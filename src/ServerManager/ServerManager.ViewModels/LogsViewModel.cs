using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ServerManager.ViewModels;

public class LogsViewModel : ObservableObject
{
	private readonly IActivityLogService _activityLog;

	private LogFileOption? _selectedLogFile;

	private string _searchText = string.Empty;

	private string _selectedSeverity = "All";

	private string _statusText = "Load a log file to inspect recent application activity.";

	private IReadOnlyList<string> _allLines = Array.Empty<string>();

	private IReadOnlyList<LogLineEntry> _allEntries = Array.Empty<LogLineEntry>();

	private LogLineEntry? _selectedEntry;

	private int _loadedLineCount;

	private int _visibleLineCount;

	private int _errorCount;

	private int _warningCount;

	private int _infoCount;

	private int _debugCount;

	private string _selectedFileSummary = "No log file selected.";

	private string _latestProblem = "No errors or warnings loaded.";

	public string Title => "Logs";

	public string Description => "Searchable operational logs for the manager, servers, backups, crashes, and console output.";

	public string LogDirectory => Path.Combine(AppContext.BaseDirectory, "logs");

	public ObservableCollection<LogFileOption> LogFiles { get; } = new ObservableCollection<LogFileOption>();

	public ObservableCollection<string> Severities { get; } = new ObservableCollection<string> { "All", "Errors + Warnings", "Error", "Warning", "Info", "Debug" };

	public ObservableCollection<LogLineEntry> VisibleEntries { get; } = new ObservableCollection<LogLineEntry>();

	public IAsyncRelayCommand RefreshCommand { get; }

	public IRelayCommand OpenLogFolderCommand { get; }

	public IRelayCommand ExportCommand { get; }

	public IRelayCommand CopySelectedCommand { get; }

	public LogFileOption? SelectedLogFile
	{
		get
		{
			return _selectedLogFile;
		}
		set
		{
			if (SetProperty(ref _selectedLogFile, value, "SelectedLogFile"))
			{
				_ = LoadSelectedLogAsync();
			}
		}
	}

	public string SearchText
	{
		get
		{
			return _searchText;
		}
		set
		{
			if (SetProperty(ref _searchText, value, "SearchText"))
			{
				ApplyFilters();
			}
		}
	}

	public string SelectedSeverity
	{
		get
		{
			return _selectedSeverity;
		}
		set
		{
			if (SetProperty(ref _selectedSeverity, string.IsNullOrWhiteSpace(value) ? "All" : value, "SelectedSeverity"))
			{
				ApplyFilters();
			}
		}
	}

	public string StatusText
	{
		get
		{
			return _statusText;
		}
		set
		{
			SetProperty(ref _statusText, value, "StatusText");
		}
	}

	public LogLineEntry? SelectedEntry
	{
		get
		{
			return _selectedEntry;
		}
		set
		{
			SetProperty(ref _selectedEntry, value, "SelectedEntry");
			CopySelectedCommand.NotifyCanExecuteChanged();
		}
	}

	public int LoadedLineCount
	{
		get { return _loadedLineCount; }
		set { SetProperty(ref _loadedLineCount, value, "LoadedLineCount"); }
	}

	public int VisibleLineCount
	{
		get { return _visibleLineCount; }
		set { SetProperty(ref _visibleLineCount, value, "VisibleLineCount"); }
	}

	public int ErrorCount
	{
		get { return _errorCount; }
		set { SetProperty(ref _errorCount, value, "ErrorCount"); }
	}

	public int WarningCount
	{
		get { return _warningCount; }
		set { SetProperty(ref _warningCount, value, "WarningCount"); }
	}

	public int InfoCount
	{
		get { return _infoCount; }
		set { SetProperty(ref _infoCount, value, "InfoCount"); }
	}

	public int DebugCount
	{
		get { return _debugCount; }
		set { SetProperty(ref _debugCount, value, "DebugCount"); }
	}

	public string SelectedFileSummary
	{
		get { return _selectedFileSummary; }
		set { SetProperty(ref _selectedFileSummary, value, "SelectedFileSummary"); }
	}

	public string LatestProblem
	{
		get { return _latestProblem; }
		set { SetProperty(ref _latestProblem, value, "LatestProblem"); }
	}

	public LogsViewModel(IActivityLogService activityLog)
	{
		_activityLog = activityLog;
		RefreshCommand = new AsyncRelayCommand(RefreshAsync);
		OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
		ExportCommand = new RelayCommand(ExportVisibleLines);
		CopySelectedCommand = new RelayCommand(CopySelectedLine, () => SelectedEntry != null);
		_ = RefreshAsync();
	}

	private async Task RefreshAsync()
	{
		Directory.CreateDirectory(LogDirectory);
		Guid? selectedId = SelectedLogFile?.Id;
		LogFiles.Clear();
		foreach (LogFileOption file in Directory.EnumerateFiles(LogDirectory, "*.log", SearchOption.TopDirectoryOnly)
			.Select((string path) => new LogFileOption(path))
			.OrderByDescending((LogFileOption file) => file.LastWriteTime))
		{
			LogFiles.Add(file);
		}
		SelectedLogFile = LogFiles.FirstOrDefault((LogFileOption file) => file.Id == selectedId) ?? LogFiles.FirstOrDefault();
		if (SelectedLogFile == null)
		{
			_allLines = Array.Empty<string>();
			_allEntries = Array.Empty<LogLineEntry>();
			VisibleEntries.Clear();
			UpdateSummary();
			StatusText = "No log files found in " + LogDirectory + ".";
		}
		await Task.CompletedTask;
	}

	private async Task LoadSelectedLogAsync()
	{
		LogFileOption? selected = SelectedLogFile;
		if (selected == null || !File.Exists(selected.Path))
		{
			_allLines = Array.Empty<string>();
			_allEntries = Array.Empty<LogLineEntry>();
			ApplyFilters();
			return;
		}
		try
		{
			_allLines = await ReadLogTailAsync(selected.Path, 5000);
			_allEntries = _allLines.Select((string line, int index) => LogLineEntry.Parse(line, index + 1)).ToList();
			ApplyFilters();
		}
		catch (Exception ex)
		{
			_allLines = Array.Empty<string>();
			_allEntries = Array.Empty<LogLineEntry>();
			VisibleEntries.Clear();
			UpdateSummary();
			StatusText = "Could not read log file: " + ex.Message;
			_activityLog.Error(StatusText);
		}
	}

	private void ApplyFilters()
	{
		string search = SearchText?.Trim() ?? string.Empty;
		string severity = SelectedSeverity ?? "All";
		IEnumerable<LogLineEntry> entries = _allEntries;
		if (!string.Equals(severity, "All", StringComparison.OrdinalIgnoreCase))
		{
			entries = entries.Where((LogLineEntry entry) => MatchesSeverity(entry.Level, severity));
		}
		if (!string.IsNullOrWhiteSpace(search))
		{
			entries = entries.Where((LogLineEntry entry) => entry.RawLine.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
		}
		List<LogLineEntry> filtered = entries.TakeLast(2000).ToList();
		VisibleEntries.Clear();
		foreach (LogLineEntry entry in filtered)
		{
			VisibleEntries.Add(entry);
		}
		VisibleLineCount = VisibleEntries.Count;
		if (SelectedEntry == null || !VisibleEntries.Contains(SelectedEntry))
		{
			SelectedEntry = VisibleEntries.LastOrDefault();
		}
		UpdateSummary();
		StatusText = SelectedLogFile == null
			? "No log file selected."
			: $"Showing {VisibleEntries.Count:N0} of {_allEntries.Count:N0} loaded lines from {SelectedLogFile.Name}.";
	}

	private void UpdateSummary()
	{
		LoadedLineCount = _allEntries.Count;
		ErrorCount = _allEntries.Count((LogLineEntry entry) => entry.IsError);
		WarningCount = _allEntries.Count((LogLineEntry entry) => entry.IsWarning);
		InfoCount = _allEntries.Count((LogLineEntry entry) => entry.IsInfo);
		DebugCount = _allEntries.Count((LogLineEntry entry) => entry.IsDebug);
		LogLineEntry? latestProblem = _allEntries.LastOrDefault((LogLineEntry entry) => entry.IsError || entry.IsWarning);
		LatestProblem = latestProblem?.Summary ?? "No errors or warnings loaded.";
		SelectedFileSummary = SelectedLogFile == null
			? "No log file selected."
			: $"{SelectedLogFile.Name} - {SelectedLogFile.SizeText} - modified {SelectedLogFile.LastWriteTime:g}";
	}

	private void OpenLogFolder()
	{
		Directory.CreateDirectory(LogDirectory);
		Process.Start(new ProcessStartInfo
		{
			FileName = "explorer.exe",
			Arguments = "\"" + LogDirectory + "\"",
			UseShellExecute = true
		});
	}

	private void ExportVisibleLines()
	{
		if (VisibleEntries.Count == 0)
		{
			MessageBox.Show("There are no visible log lines to export.", "Export logs", MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		SaveFileDialog dialog = new SaveFileDialog
		{
			Title = "Export visible logs",
			FileName = "server-manager-logs.txt",
			Filter = "Text files|*.txt|Log files|*.log|All files|*.*",
			OverwritePrompt = true
		};
		if (!dialog.ShowDialog(Application.Current?.MainWindow).GetValueOrDefault())
		{
			return;
		}
		File.WriteAllLines(dialog.FileName, VisibleEntries.Select((LogLineEntry entry) => entry.RawLine));
		StatusText = "Exported " + VisibleEntries.Count + " log line(s) to " + dialog.FileName + ".";
		_activityLog.Info(StatusText);
	}

	private void CopySelectedLine()
	{
		if (SelectedEntry == null)
		{
			return;
		}
		Clipboard.SetText(SelectedEntry.RawLine);
		StatusText = "Copied selected log line to clipboard.";
	}

	private static async Task<IReadOnlyList<string>> ReadLogTailAsync(string path, int maxLines)
	{
		string[] lines = await File.ReadAllLinesAsync(path).ConfigureAwait(false);
		if (lines.Length <= maxLines)
		{
			return lines;
		}
		return lines.Skip(lines.Length - maxLines).ToArray();
	}

	private static bool MatchesSeverity(string level, string severity)
	{
		return severity switch
		{
			"Errors + Warnings" => level.Equals("ERR", StringComparison.OrdinalIgnoreCase) || level.Equals("FTL", StringComparison.OrdinalIgnoreCase) || level.Equals("WRN", StringComparison.OrdinalIgnoreCase),
			"Error" => level.Equals("ERR", StringComparison.OrdinalIgnoreCase) || level.Equals("FTL", StringComparison.OrdinalIgnoreCase),
			"Warning" => level.Equals("WRN", StringComparison.OrdinalIgnoreCase),
			"Info" => level.Equals("INF", StringComparison.OrdinalIgnoreCase),
			"Debug" => level.Equals("DBG", StringComparison.OrdinalIgnoreCase) || level.Equals("VRB", StringComparison.OrdinalIgnoreCase),
			_ => true
		};
	}
}

public class LogLineEntry
{
	public int LineNumber { get; }

	public string Timestamp { get; }

	public string Level { get; }

	public string Message { get; }

	public string RawLine { get; }

	public bool IsError => Level.Equals("ERR", StringComparison.OrdinalIgnoreCase) || Level.Equals("FTL", StringComparison.OrdinalIgnoreCase);

	public bool IsWarning => Level.Equals("WRN", StringComparison.OrdinalIgnoreCase);

	public bool IsInfo => Level.Equals("INF", StringComparison.OrdinalIgnoreCase);

	public bool IsDebug => Level.Equals("DBG", StringComparison.OrdinalIgnoreCase) || Level.Equals("VRB", StringComparison.OrdinalIgnoreCase);

	public string LevelText => Level switch
	{
		"ERR" => "Error",
		"FTL" => "Fatal",
		"WRN" => "Warning",
		"INF" => "Info",
		"DBG" => "Debug",
		"VRB" => "Verbose",
		_ => "Other"
	};

	public string Summary => string.IsNullOrWhiteSpace(Timestamp)
		? LevelText + ": " + Message
		: Timestamp + " " + LevelText + ": " + Message;

	private LogLineEntry(int lineNumber, string timestamp, string level, string message, string rawLine)
	{
		LineNumber = lineNumber;
		Timestamp = timestamp;
		Level = level;
		Message = message;
		RawLine = rawLine;
	}

	public static LogLineEntry Parse(string rawLine, int lineNumber)
	{
		string line = rawLine ?? string.Empty;
		string timestamp = string.Empty;
		string level = "OTH";
		string message = line;
		int levelStart = line.IndexOf('[');
		int levelEnd = levelStart >= 0 ? line.IndexOf(']', levelStart + 1) : -1;
		if (levelStart > 0 && levelEnd > levelStart)
		{
			timestamp = line.Substring(0, levelStart).Trim();
			level = line.Substring(levelStart + 1, levelEnd - levelStart - 1).Trim();
			message = line.Substring(levelEnd + 1).Trim();
		}
		return new LogLineEntry(lineNumber, timestamp, level, message, line);
	}
}

public class LogFileOption
{
	public Guid Id { get; } = Guid.NewGuid();

	public string Path { get; }

	public string Name => System.IO.Path.GetFileName(Path);

	public DateTime LastWriteTime => File.GetLastWriteTime(Path);

	public string DisplayName => Name + "  (" + LastWriteTime.ToString("g") + ")";

	public string SizeText
	{
		get
		{
			long length = File.Exists(Path) ? new FileInfo(Path).Length : 0L;
			if (length >= 1024L * 1024L)
			{
				return (length / 1024d / 1024d).ToString("0.0") + " MB";
			}
			if (length >= 1024L)
			{
				return (length / 1024d).ToString("0.0") + " KB";
			}
			return length + " B";
		}
	}

	public LogFileOption(string path)
	{
		Path = path;
	}

	public override string ToString()
	{
		return DisplayName;
	}
}
