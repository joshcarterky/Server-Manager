using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class ConsoleViewModel : ObservableObject
{
	private readonly IRconService _rconService;

	private readonly IConfigService _configService;

	private readonly ILoggingService _loggingService;

	private readonly IServerConsoleService _serverConsoleService;

	private ServerInstance? _selectedServer;

	private string _commandText = string.Empty;

	private string _statusText = "Disconnected";

	private string _lastConnectionMessage = string.Empty;

	public ObservableCollection<ServerInstance> Servers { get; } = new ObservableCollection<ServerInstance>();


	public ObservableCollection<string> ConsoleLines => _serverConsoleService.Lines;


	public ServerInstance? SelectedServer
	{
		get
		{
			return _selectedServer;
		}
		set
		{
			SetProperty(ref _selectedServer, value, "SelectedServer");
			ConnectCommand.NotifyCanExecuteChanged();
			DisconnectCommand.NotifyCanExecuteChanged();
			RefreshConnectionCommand.NotifyCanExecuteChanged();
			SendCommandCommand.NotifyCanExecuteChanged();
		}
	}

	public string CommandText
	{
		get
		{
			return _commandText;
		}
		set
		{
			SetProperty(ref _commandText, value, "CommandText");
			SendCommandCommand.NotifyCanExecuteChanged();
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

	public IAsyncRelayCommand ConnectCommand { get; }

	public IAsyncRelayCommand DisconnectCommand { get; }

	public IAsyncRelayCommand RefreshConnectionCommand { get; }

	public IAsyncRelayCommand SendCommandCommand { get; }

	public IRelayCommand ClearConsoleCommand { get; }

	public ICommand RefreshServersCommand { get; }

	public ConsoleViewModel(IRconService rconService, IConfigService configService, ILoggingService loggingService, IServerConsoleService serverConsoleService)
	{
		_rconService = rconService;
		_configService = configService;
		_loggingService = loggingService;
		_serverConsoleService = serverConsoleService;
		ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
		DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => _rconService.IsConnected);
		RefreshConnectionCommand = new AsyncRelayCommand(RefreshConnectionAsync, () => SelectedServer != null);
		SendCommandCommand = new AsyncRelayCommand(SendCommandAsync, CanSendCommand);
		ClearConsoleCommand = new RelayCommand(delegate
		{
			_serverConsoleService.Clear();
		});
		RefreshServersCommand = new RelayCommand(LoadServers);
		LoadServers();
		AddConsoleLine("Console ready. Select a server and connect to RCON.");
	}

	public async Task ConnectToServerAsync(ServerInstance server, bool showErrorDialog = false, int retryCount = 0, int retryDelayMs = 2000)
	{
		SelectedServer = server;
		for (int attempt = 0; attempt <= retryCount; attempt++)
		{
			bool lastAttempt = attempt == retryCount;
			if (await ConnectCoreAsync(showErrorDialog && lastAttempt).ConfigureAwait(false))
			{
				return;
			}
			if (lastAttempt)
			{
				return;
			}
			await Task.Delay(retryDelayMs).ConfigureAwait(false);
		}
	}

	private void LoadServers()
	{
		AppConfig result = Task.Run(() => _configService.LoadAsync()).GetAwaiter().GetResult();
		Guid? selectedId = SelectedServer?.Id;
		Servers.Clear();
		foreach (ServerInstance server in result.Servers)
		{
			Servers.Add(server);
		}
		SelectedServer = Servers.FirstOrDefault(delegate(ServerInstance x)
		{
			Guid id = x.Id;
			Guid? guid = selectedId;
			return id == guid;
		}) ?? Servers.FirstOrDefault();
	}

	private bool CanConnect()
	{
		if (SelectedServer != null)
		{
			return !_rconService.IsConnected;
		}
		return false;
	}

	private bool CanSendCommand()
	{
		if (_rconService.IsConnected)
		{
			return !string.IsNullOrWhiteSpace(CommandText);
		}
		return false;
	}

	private async Task ConnectAsync()
	{
		await ConnectCoreAsync(showErrorDialog: true);
	}

	private async Task<bool> ConnectCoreAsync(bool showErrorDialog)
	{
		if (SelectedServer == null)
		{
			return false;
		}
		if (!await IsTcpPortOpenAsync("127.0.0.1", SelectedServer.RconPort, 750).ConfigureAwait(false))
		{
			string message = $"RCON is not listening yet on 127.0.0.1:{SelectedServer.RconPort}. Start the server or wait for it to finish booting.";
			SetStatusText("Waiting for RCON");
			AddConnectionMessage(message);
			if (showErrorDialog)
			{
				MessageBox.Show(message, "Console connection", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			RefreshCommandStates();
			return false;
		}
		string password = GetRconPassword(SelectedServer);
		try
		{
			AddConnectionMessage($"Connecting to {SelectedServer.Name} RCON on 127.0.0.1:{SelectedServer.RconPort}...");
			await _rconService.ConnectAsync("127.0.0.1", SelectedServer.RconPort, password);
			SetStatusText("Connected to " + SelectedServer.Name);
			AddConsoleLine("Connected.");
			_lastConnectionMessage = string.Empty;
			return true;
		}
		catch (Exception ex)
		{
			_loggingService.Logger.Error(ex, "Failed to connect RCON for {ServerName}", SelectedServer.Name);
			SetStatusText("Disconnected");
			AddConnectionMessage("Connection failed: " + ex.Message);
			if (showErrorDialog)
			{
				MessageBox.Show(ex.Message, "Console connection failed", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			return false;
		}
		finally
		{
			RefreshCommandStates();
		}
	}

	private static string GetRconPassword(ServerInstance server)
	{
		if (!string.IsNullOrWhiteSpace(server.AdminPassword))
		{
			return server.AdminPassword;
		}
		if (!string.IsNullOrWhiteSpace(server.Config.AdminPassword))
		{
			server.AdminPassword = server.Config.AdminPassword;
			return server.Config.AdminPassword;
		}
		if (!string.IsNullOrWhiteSpace(server.RconPassword))
		{
			server.AdminPassword = server.RconPassword;
			server.Config.AdminPassword = server.RconPassword;
			return server.RconPassword;
		}
		server.RconPassword = "admin";
		server.AdminPassword = "admin";
		server.Config.RconPassword = "admin";
		server.Config.AdminPassword = "admin";
		return "admin";
	}

	private async Task DisconnectAsync()
	{
		await _rconService.DisconnectAsync();
		SetStatusText("Disconnected");
		AddConsoleLine("Disconnected.");
		RefreshCommandStates();
	}

	private async Task RefreshConnectionAsync()
	{
		if (SelectedServer == null)
		{
			return;
		}
		AddConsoleLine("Refreshing RCON connection...");
		if (_rconService.IsConnected)
		{
			await _rconService.DisconnectAsync();
			SetStatusText("Disconnected");
		}
		await ConnectCoreAsync(showErrorDialog: true);
	}

	private async Task SendCommandAsync()
	{
		if (!string.IsNullOrWhiteSpace(CommandText))
		{
			string commandText = CommandText;
			CommandText = string.Empty;
			AddConsoleLine("> " + commandText);
			try
			{
				string text = await _rconService.SendCommandAsync(commandText);
				AddConsoleLine(string.IsNullOrWhiteSpace(text) ? "(no response)" : text);
			}
			catch (Exception ex)
			{
				_loggingService.Logger.Error(ex, "Console command failed");
				AddConsoleLine("Command failed: " + ex.Message);
			}
			RefreshCommandStates();
		}
	}

	private void RefreshCommandStates()
	{
		RunOnUiThread(delegate
		{
			ConnectCommand.NotifyCanExecuteChanged();
			DisconnectCommand.NotifyCanExecuteChanged();
			RefreshConnectionCommand.NotifyCanExecuteChanged();
			SendCommandCommand.NotifyCanExecuteChanged();
		});
	}

	private void AddConsoleLine(string line)
	{
		_serverConsoleService.AddLine(line);
	}

	private void AddConnectionMessage(string line)
	{
		RunOnUiThread(delegate
		{
			if (!string.Equals(_lastConnectionMessage, line, StringComparison.Ordinal))
			{
				_serverConsoleService.AddLine(line);
				_lastConnectionMessage = line;
			}
		});
	}

	private static async Task<bool> IsTcpPortOpenAsync(string host, int port, int timeoutMs)
	{
		try
		{
			using TcpClient client = new TcpClient();
			Task connectTask = client.ConnectAsync(host, port);
			Task completedTask = await Task.WhenAny(connectTask, Task.Delay(timeoutMs)).ConfigureAwait(false);
			if (completedTask != connectTask)
			{
				return false;
			}
			await connectTask.ConfigureAwait(false);
			return client.Connected;
		}
		catch
		{
			return false;
		}
	}

	private void SetStatusText(string statusText)
	{
		RunOnUiThread(delegate
		{
			StatusText = statusText;
		});
	}

	private static void RunOnUiThread(Action action)
	{
		Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
		if (dispatcher.CheckAccess())
		{
			action();
		}
		else
		{
			dispatcher.Invoke(action);
		}
	}
}
