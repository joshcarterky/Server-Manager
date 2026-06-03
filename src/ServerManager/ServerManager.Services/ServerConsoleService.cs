using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using ServerManager.Models;

namespace ServerManager.Services;

public class ServerConsoleService : IServerConsoleService
{
	private const int MaxLines = 2000;

	public ObservableCollection<string> Lines { get; } = new ObservableCollection<string>();

	public void AddLine(ServerInstance server, string message)
	{
		AddLine($"[{DateTime.Now:HH:mm:ss}] [{server.Name}] {message}");
	}

	public void AddLine(string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		RunOnUiThread(delegate
		{
			Lines.Add(message);
			while (Lines.Count > MaxLines)
			{
				Lines.RemoveAt(0);
			}
		});
	}

	public void Clear()
	{
		RunOnUiThread(Lines.Clear);
	}

	private static void RunOnUiThread(Action action)
	{
		Dispatcher? dispatcher = Application.Current?.Dispatcher;
		if (dispatcher == null || dispatcher.CheckAccess())
		{
			action();
		}
		else
		{
			dispatcher.BeginInvoke((Delegate)action, Array.Empty<object>());
		}
	}
}
