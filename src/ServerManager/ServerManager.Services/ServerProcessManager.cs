using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ServerManager.Helpers;
using ServerManager.Models;

namespace ServerManager.Services;

public class ServerProcessManager : IServerProcessManager
{
	private const int SW_HIDE = 0;

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	private sealed class ManagedServerProcess
	{
		private readonly Process _process;

		private TimeSpan _previousTotalProcessorTime;

		private DateTime _previousSampleTime;

		public Process Process => _process;

		public ServerInstance Server { get; }

		public bool IsRunning => !_process.HasExited;

		public ManagedServerProcess(ServerInstance server, Process process)
		{
			Server = server;
			_process = process;
			_previousSampleTime = DateTime.UtcNow;
			_previousTotalProcessorTime = _process.TotalProcessorTime;
		}

		public ServerStatus GetStatus()
		{
			ServerStatus serverStatus = new ServerStatus
			{
				IsOnline = !_process.HasExited,
				MemoryUsageMB = _process.WorkingSet64 / 1024 / 1024,
				IsJoinable = IsUdpPortOpen(Server.GamePort) || IsUdpPortOpen(Server.QueryPort),
				CurrentMap = Server.MapName,
				PlayerCount = 0,
				PingMs = 0,
				LastUpdated = DateTime.UtcNow,
				Uptime = DateTime.UtcNow - _process.StartTime
			};
			DateTime utcNow = DateTime.UtcNow;
			TimeSpan timeSpan = _process.TotalProcessorTime - _previousTotalProcessorTime;
			TimeSpan timeSpan2 = utcNow - _previousSampleTime;
			if (timeSpan2.TotalMilliseconds > 0.0)
			{
				serverStatus.CpuUsage = Math.Round(timeSpan.TotalMilliseconds / timeSpan2.TotalMilliseconds / (double)Environment.ProcessorCount * 100.0, 1);
			}
			_previousTotalProcessorTime = _process.TotalProcessorTime;
			_previousSampleTime = utcNow;
			return serverStatus;
		}
	}

	private readonly Dictionary<Guid, ManagedServerProcess> _processes = new Dictionary<Guid, ManagedServerProcess>();

	private readonly List<ServerInstance> _servers = new List<ServerInstance>();

	private readonly ILoggingService _loggingService;

	private readonly IServerConsoleService _serverConsoleService;

	public IReadOnlyList<ServerInstance> Servers => _servers.AsReadOnly();

	public ServerProcessManager(ILoggingService loggingService, IServerConsoleService serverConsoleService)
	{
		_loggingService = loggingService;
		_serverConsoleService = serverConsoleService;
	}

	public Task InitializeAsync(AppConfig config)
	{
		_servers.Clear();
		_servers.AddRange(config.Servers);
		return Task.CompletedTask;
	}

	public void AddServer(ServerInstance server)
	{
		ServerInstance server2 = server;
		int existingIndex = _servers.FindIndex((ServerInstance x) => x.Id == server2.Id);
		if (existingIndex >= 0)
		{
			_servers[existingIndex] = server2;
		}
		else
		{
			_servers.Add(server2);
		}
	}

	public void RemoveServer(ServerInstance server)
	{
		ServerInstance server2 = server;
		_servers.RemoveAll((ServerInstance x) => x.Id == server2.Id);
		_processes.Remove(server2.Id);
	}

	public async Task StartServerAsync(ServerInstance server)
	{
		ServerInstance server2 = server;
		string fileName = ResolveServerExecutable(server2);
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new FileNotFoundException("Server executable not found. Run Install / Update first, or choose the folder that contains the installed ASA server files.", Path.Combine(server2.InstallDirectory, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe"));
		}
		if (TryAttachRunningServerProcess(server2, fileName, out ManagedServerProcess? existingProcess))
		{
			_processes[server2.Id] = existingProcess;
			_serverConsoleService.AddLine(server2, $"Attached to running server process {existingProcess.Process.Id}. Stop duplicate ASA processes before starting another copy.");
			return;
		}
		if (!_processes.ContainsKey(server2.Id) || !_processes[server2.Id].IsRunning)
		{
			FileHelpers.EnsureDirectory(server2.InstallDirectory);
			if (!string.IsNullOrWhiteSpace(server2.ClusterId) && !string.IsNullOrWhiteSpace(server2.ClusterDirectory))
			{
				FileHelpers.EnsureDirectory(server2.ClusterDirectory);
			}
			HydrateManagedIniValues(server2);
			EnsureRconConfiguration(server2);
			string arguments = BuildLaunchArguments(server2);
			ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments)
			{
				WorkingDirectory = Path.GetDirectoryName(fileName) ?? server2.InstallDirectory,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true
			};
			Process process = new Process
			{
				StartInfo = startInfo,
				EnableRaisingEvents = true
			};
			process.OutputDataReceived += delegate(object _, DataReceivedEventArgs args)
			{
				if (!string.IsNullOrWhiteSpace(args.Data))
				{
					_loggingService.Logger.Information("[{Server}] {Output}", server2.Name, args.Data);
				}
			};
			process.ErrorDataReceived += delegate(object _, DataReceivedEventArgs args)
			{
				if (!string.IsNullOrWhiteSpace(args.Data))
				{
					_loggingService.Logger.Warning("[{Server}] {Output}", server2.Name, args.Data);
				}
			};
			process.Exited += delegate
			{
				_loggingService.Logger.Warning("Server process exited: {Server}", server2.Name);
				_serverConsoleService.AddLine(server2, "Server process exited.");
			};
			_serverConsoleService.AddLine(server2, "Starting server process...");
			_serverConsoleService.AddLine(server2, Path.GetFileName(fileName) + " " + arguments);
			process.Start();
			_ = HideProcessWindowAsync(process);
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			_processes[server2.Id] = new ManagedServerProcess(server2, process);
			await Task.CompletedTask;
		}
	}

	private static bool IsUdpPortOpen(int port)
	{
		try
		{
			return IPGlobalProperties.GetIPGlobalProperties()
				.GetActiveUdpListeners()
				.Any(endpoint => endpoint.Port == port);
		}
		catch
		{
			return false;
		}
	}

	private static string ResolveServerExecutable(ServerInstance server)
	{
		string[] candidatePaths = new string[4]
		{
			Path.Combine(server.InstallDirectory, server.ExecutableName),
			Path.Combine(server.InstallDirectory, "ShooterGame", "Binaries", "Win64", server.ExecutableName),
			Path.Combine(server.InstallDirectory, "ShooterGame", "Binaries", "Win64", "ArkAscendedServer.exe"),
			Path.Combine(server.InstallDirectory, "ShooterGame", "Binaries", "Win64", "ShooterGameServer.exe")
		};
		foreach (string path in candidatePaths)
		{
			if (File.Exists(path))
			{
				server.ExecutableName = Path.GetFileName(path);
				return path;
			}
		}
		try
		{
			string? discovered = Directory.EnumerateFiles(server.InstallDirectory, "ArkAscendedServer.exe", SearchOption.AllDirectories).FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(discovered))
			{
				server.ExecutableName = Path.GetFileName(discovered);
				return discovered;
			}
		}
		catch
		{
		}
		return string.Empty;
	}

	private static async Task HideProcessWindowAsync(Process process)
	{
		for (int i = 0; i < 30; i++)
		{
			if (process.HasExited)
			{
				return;
			}
			process.Refresh();
			if (process.MainWindowHandle != IntPtr.Zero)
			{
				ShowWindow(process.MainWindowHandle, SW_HIDE);
				return;
			}
			await Task.Delay(250).ConfigureAwait(false);
		}
	}

	public Task StopServerAsync(ServerInstance server)
	{
		if (!_processes.TryGetValue(server.Id, out ManagedServerProcess value) || value.Process.HasExited)
		{
			return Task.CompletedTask;
		}
		try
		{
			value.Process.CloseMainWindow();
			if (!value.Process.WaitForExit(5000))
			{
				value.Process.Kill(entireProcessTree: true);
			}
		}
		catch (Exception exception)
		{
			_loggingService.Logger.Error(exception, "Failed to stop server {Server}", server.Name);
		}
		return Task.CompletedTask;
	}

	public async Task RestartServerAsync(ServerInstance server)
	{
		await StopServerAsync(server).ConfigureAwait(continueOnCapturedContext: false);
		await StartServerAsync(server).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task SendCommandAsync(ServerInstance server, string command)
	{
		if (!_processes.TryGetValue(server.Id, out ManagedServerProcess value) || value.Process.HasExited)
		{
			throw new InvalidOperationException("Server is not running.");
		}
		try
		{
			value.Process.StandardInput.WriteLine(command);
		}
		catch (Exception exception)
		{
			_loggingService.Logger.Error(exception, "Failed to send command to {Server}", server.Name);
		}
		return Task.CompletedTask;
	}

	public ServerStatus? GetStatus(ServerInstance server)
	{
		if (!_processes.TryGetValue(server.Id, out ManagedServerProcess value) || !value.IsRunning)
		{
			string fileName = ResolveServerExecutable(server);
			if (!string.IsNullOrWhiteSpace(fileName) && TryAttachRunningServerProcess(server, fileName, out ManagedServerProcess? attachedProcess))
			{
				_processes[server.Id] = attachedProcess;
				value = attachedProcess;
			}
			else
			{
				return new ServerStatus
				{
					IsOnline = false
				};
			}
		}
		return value.GetStatus();
	}

	private static bool TryAttachRunningServerProcess(ServerInstance server, string executablePath, out ManagedServerProcess? managedProcess)
	{
		managedProcess = null;
		string normalizedExecutable = NormalizePath(executablePath);
		string normalizedInstallDirectory = NormalizePath(server.InstallDirectory);
		Process? process = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executablePath))
			.Where((Process candidate) => !candidate.HasExited)
			.Select(delegate(Process candidate)
			{
				try
				{
					string mainModulePath = NormalizePath(candidate.MainModule?.FileName ?? string.Empty);
					bool exactExecutable = string.Equals(mainModulePath, normalizedExecutable, StringComparison.OrdinalIgnoreCase);
					bool underInstallDirectory = !string.IsNullOrWhiteSpace(normalizedInstallDirectory) && mainModulePath.StartsWith(normalizedInstallDirectory, StringComparison.OrdinalIgnoreCase);
					return new
					{
						Process = candidate,
						Matches = exactExecutable || underInstallDirectory,
						StartTime = candidate.StartTime
					};
				}
				catch
				{
					return new
					{
						Process = candidate,
						Matches = false,
						StartTime = DateTime.MinValue
					};
				}
			})
			.Where(x => x.Matches)
			.OrderByDescending(x => x.StartTime)
			.Select(x => x.Process)
			.FirstOrDefault();
		if (process == null)
		{
			return false;
		}
		managedProcess = new ManagedServerProcess(server, process);
		return true;
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}
		try
		{
			return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
		catch
		{
			return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
	}

	public async IAsyncEnumerable<ServerStatus> MonitorServersAsync()
	{
		while (true)
		{
			foreach (ServerInstance server in _servers)
			{
				yield return GetStatus(server) ?? new ServerStatus
				{
					IsOnline = false
				};
			}
			await Task.Delay(2000).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private static string BuildLaunchArguments(ServerInstance server)
	{
		string rconPassword = GetRconPassword(server);
		string mapOptions = GetMapPackageName(server.MapName) + "?SessionName=" + CleanMapOptionValue(server.Name) + "?ServerAdminPassword=" + CleanMapOptionValue(rconPassword) + "?RCONEnabled=True?RCONPort=" + server.RconPort;
		List<string> list = new List<string>
		{
			QuoteLaunchArgument(mapOptions),
			"-server",
			"-NoLogWindow",
			"-stdout",
			"-FullStdOutLogOutput",
			$"-port={server.GamePort}",
			$"-queryport={server.QueryPort}",
			$"-RCONPort={server.RconPort}",
			"-RCONEnabled=" + (server.Config.UseRcon ? "True" : "False"),
			"-ServerAdminPassword=" + rconPassword,
			$"-MaxPlayers={server.MaxPlayers}",
			"-Crossplay=" + (server.CrossplayEnabled ? "true" : "false")
		};
		if (!server.BattleEyeEnabled || !server.Config.UseBattleEye)
		{
			list.Add("-NoBattlEye");
		}
		if (!string.IsNullOrWhiteSpace(server.ServerPassword))
		{
			list.Add("-serverpassword=" + server.ServerPassword);
		}
		if (!string.IsNullOrWhiteSpace(server.AdminPassword))
		{
			list.Add("-adminpassword=" + server.AdminPassword);
		}
		if (!string.IsNullOrWhiteSpace(server.ClusterId))
		{
			list.Add("-clusterid=" + server.ClusterId);
			if (!string.IsNullOrWhiteSpace(server.ClusterDirectory))
			{
				list.Add("-ClusterDirOverride=\"" + server.ClusterDirectory.Trim().Trim('"') + "\"");
			}
			if (server.NoTransferFromFiltering)
			{
				list.Add("-NoTransferFromFiltering");
			}
		}
		if (!string.IsNullOrWhiteSpace(server.LaunchParameters))
		{
			list.Add(RemoveManagedLaunchArguments(server.LaunchParameters));
		}
		if (server.Mods.Count > 0 && !list.Any((string x) => x.StartsWith("-mods=", StringComparison.OrdinalIgnoreCase)))
		{
			string text = string.Join(",", from x in server.Mods
				orderby x.LoadOrder
				select x.WorkshopId into x
				where !string.IsNullOrWhiteSpace(x)
				select x);
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add("-mods=" + text);
			}
		}
		return string.Join(' ', list);
	}

	private static string CleanMapOptionValue(string value)
	{
		return (value ?? string.Empty).Trim().Replace("\"", string.Empty).Replace("?", string.Empty);
	}

	private static string QuoteLaunchArgument(string argument)
	{
		if (string.IsNullOrWhiteSpace(argument))
		{
			return "\"\"";
		}
		if (!argument.Any(char.IsWhiteSpace) && !argument.Contains('"'))
		{
			return argument;
		}
		return "\"" + argument.Replace("\"", "\\\"") + "\"";
	}

	private static string GetMapPackageName(string mapName)
	{
		return (mapName ?? string.Empty).Trim() switch
		{
			"TheIsland" => "TheIsland_WP",
			"TheCenter" => "TheCenter_WP",
			"ScorchedEarth" => "ScorchedEarth_WP",
			"Ragnarok" => "Ragnarok_WP",
			"Aberration" => "Aberration_WP",
			"Extinction" => "Extinction_WP",
			"Astraeos" => "Astraeos_WP",
			"Valguero" => "Valguero_WP",
			"LostColony" or "Lost Colony" => "LostColony_WP",
			"ClubARK" or "Club ARK" or "BobsMissions" => "BobsMissions_WP",
			string value when value.EndsWith("_WP", StringComparison.OrdinalIgnoreCase) => value,
			string value when !string.IsNullOrWhiteSpace(value) => value,
			_ => "TheIsland_WP"
		};
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

	private static void EnsureRconConfiguration(ServerInstance server)
	{
		string password = GetRconPassword(server);
		string configDirectory = Path.Combine(server.InstallDirectory, "ShooterGame", "Saved", "Config", "WindowsServer");
		Directory.CreateDirectory(configDirectory);
		string settingsPath = Path.Combine(configDirectory, "GameUserSettings.ini");
		List<string> lines = File.Exists(settingsPath) ? File.ReadAllLines(settingsPath).ToList() : new List<string>();
		UpsertIniValue(lines, "ServerSettings", "RCONEnabled", server.Config.UseRcon ? "True" : "False");
		UpsertIniValue(lines, "ServerSettings", "RCONPort", server.RconPort.ToString());
		UpsertIniValue(lines, "ServerSettings", "ServerAdminPassword", password);
		UpsertIniValue(lines, "/Script/Engine.GameSession", "MaxPlayers", server.MaxPlayers.ToString());
		File.WriteAllLines(settingsPath, lines);
	}

	private static void HydrateManagedIniValues(ServerInstance server)
	{
		string settingsPath = Path.Combine(server.InstallDirectory, "ShooterGame", "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
		if (!File.Exists(settingsPath))
		{
			return;
		}
		Dictionary<string, Dictionary<string, string>> values = ReadIniFile(settingsPath);
		if (TryGetIniValue(values, "MaxPlayers", out string? maxPlayersValue) && int.TryParse(maxPlayersValue, out int maxPlayers))
		{
			server.MaxPlayers = maxPlayers;
			server.Config.MaxPlayers = maxPlayers;
		}
		if (TryGetIniValue(values, "ActiveMods", out string? activeMods))
		{
			List<string> modIds = activeMods
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select((string value) => value.Trim())
				.Where((string value) => !string.IsNullOrWhiteSpace(value))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (modIds.Count > 0)
			{
				Dictionary<string, ModEntry> existingMods = server.Mods
					.Where((ModEntry mod) => !string.IsNullOrWhiteSpace(mod.WorkshopId))
					.GroupBy((ModEntry mod) => mod.WorkshopId, StringComparer.OrdinalIgnoreCase)
					.ToDictionary((IGrouping<string, ModEntry> group) => group.Key, (IGrouping<string, ModEntry> group) => group.First(), StringComparer.OrdinalIgnoreCase);
				List<ModEntry> mods = new List<ModEntry>();
				for (int i = 0; i < modIds.Count; i++)
				{
					string modId = modIds[i];
					if (!existingMods.TryGetValue(modId, out ModEntry? mod))
					{
						mod = new ModEntry
						{
							Title = "CurseForge Mod " + modId,
							WorkshopId = modId
						};
					}
					mod.LoadOrder = i + 1;
					mods.Add(mod);
				}
				server.Mods = mods;
			}
		}
	}

	private static Dictionary<string, Dictionary<string, string>> ReadIniFile(string path)
	{
		Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		string currentSection = string.Empty;
		sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string rawLine in File.ReadLines(path))
		{
			string line = rawLine.Trim();
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
			{
				continue;
			}
			if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
			{
				currentSection = line;
				if (!sections.ContainsKey(currentSection))
				{
					sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				}
				continue;
			}
			int equalsIndex = line.IndexOf('=');
			if (equalsIndex <= 0)
			{
				continue;
			}
			string key = line.Substring(0, equalsIndex).Trim();
			string value = line.Substring(equalsIndex + 1).Trim();
			sections[currentSection][key] = value;
		}
		return sections;
	}

	private static bool TryGetIniValue(Dictionary<string, Dictionary<string, string>> values, string key, out string? value)
	{
		foreach (Dictionary<string, string> sectionValues in values.Values)
		{
			if (sectionValues.TryGetValue(key, out value))
			{
				return true;
			}
		}
		value = null;
		return false;
	}

	private static void UpsertIniValue(List<string> lines, string section, string key, string value)
	{
		string sectionHeader = "[" + section + "]";
		int sectionIndex = lines.FindIndex((string line) => string.Equals(line.Trim(), sectionHeader, StringComparison.OrdinalIgnoreCase));
		if (sectionIndex < 0)
		{
			if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
			{
				lines.Add(string.Empty);
			}
			lines.Add(sectionHeader);
			lines.Add(key + "=" + value);
			return;
		}
		int nextSectionIndex = lines.FindIndex(sectionIndex + 1, (string line) => line.TrimStart().StartsWith("[", StringComparison.Ordinal) && line.TrimEnd().EndsWith("]", StringComparison.Ordinal));
		int searchEnd = nextSectionIndex < 0 ? lines.Count : nextSectionIndex;
		for (int i = sectionIndex + 1; i < searchEnd; i++)
		{
			string line = lines[i].TrimStart();
			if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
			{
				lines[i] = key + "=" + value;
				return;
			}
		}
		lines.Insert(searchEnd, key + "=" + value);
	}

	private static string RemoveManagedLaunchArguments(string launchParameters)
	{
		if (string.IsNullOrWhiteSpace(launchParameters))
		{
			return string.Empty;
		}
		IEnumerable<string> values = from part in launchParameters.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			where !part.StartsWith("-clusterid=", StringComparison.OrdinalIgnoreCase) && !part.StartsWith("-ClusterDirOverride=", StringComparison.OrdinalIgnoreCase) && !part.Equals("-NoTransferFromFiltering", StringComparison.OrdinalIgnoreCase) && !part.Equals("-NoBattlEye", StringComparison.OrdinalIgnoreCase) && !part.Equals("-NoBattleEye", StringComparison.OrdinalIgnoreCase) && !part.Equals("-log", StringComparison.OrdinalIgnoreCase) && !part.Equals("-NoLogWindow", StringComparison.OrdinalIgnoreCase) && !part.Equals("-stdout", StringComparison.OrdinalIgnoreCase) && !part.Equals("-FullStdOutLogOutput", StringComparison.OrdinalIgnoreCase)
			select part;
		return string.Join(' ', values);
	}
}
