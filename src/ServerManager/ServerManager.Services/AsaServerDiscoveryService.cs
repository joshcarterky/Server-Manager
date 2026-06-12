using System;
using System.IO;
using System.Linq;
using ServerManager.Models;

namespace ServerManager.Services;

public class AsaServerDiscoveryService : IAsaServerDiscoveryService
{
	private static readonly string[] ExecutableNames =
	{
		"ArkAscendedServer.exe",
		"ShooterGameServer.exe"
	};

	public AsaServerDiscoveryResult Discover(string selectedDirectory)
	{
		AsaServerDiscoveryResult result = new AsaServerDiscoveryResult
		{
			SelectedDirectory = selectedDirectory ?? string.Empty
		};

		if (string.IsNullOrWhiteSpace(selectedDirectory))
		{
			result.Errors.Add("No folder was selected.");
			return result;
		}

		string selectedPath;
		try
		{
			selectedPath = Path.GetFullPath(selectedDirectory);
		}
		catch (Exception ex)
		{
			result.Errors.Add("Selected folder path is invalid: " + ex.Message);
			return result;
		}

		if (!Directory.Exists(selectedPath))
		{
			result.Errors.Add("Selected folder does not exist.");
			return result;
		}

		result.InstallDirectory = ResolveInstallDirectory(selectedPath);
		result.ShooterGameDirectory = Path.Combine(result.InstallDirectory, "ShooterGame");
		result.ExecutablePath = ResolveExecutablePath(result.InstallDirectory);
		result.ExecutableName = string.IsNullOrWhiteSpace(result.ExecutablePath) ? string.Empty : Path.GetFileName(result.ExecutablePath);
		result.SavedDirectory = ResolveSavedDirectory(result.InstallDirectory, selectedPath);
		result.ConfigDirectory = Path.Combine(result.SavedDirectory, "Config", "WindowsServer");
		result.GameUserSettingsPath = Path.Combine(result.ConfigDirectory, "GameUserSettings.ini");
		result.GameIniPath = Path.Combine(result.ConfigDirectory, "Game.ini");
		result.EngineIniPath = Path.Combine(result.ConfigDirectory, "Engine.ini");
		result.SavedArksDirectory = ResolveFirstExistingDirectory(
			Path.Combine(result.SavedDirectory, "SavedArks"),
			Path.Combine(result.SavedDirectory, "SavedArksLocal"));
		result.LogsDirectory = Path.Combine(result.SavedDirectory, "Logs");
		result.ModsDirectory = ResolveFirstExistingDirectory(
			Path.Combine(result.InstallDirectory, "ShooterGame", "Content", "Mods"),
			Path.Combine(result.SavedDirectory, "Mods"),
			Path.Combine(result.InstallDirectory, "Mods"));

		if (!Directory.Exists(result.ShooterGameDirectory))
		{
			result.Errors.Add("ShooterGame folder was not found.");
		}
		if (string.IsNullOrWhiteSpace(result.ExecutablePath))
		{
			result.Errors.Add("ASA server executable was not found. Expected ArkAscendedServer.exe or ShooterGameServer.exe.");
		}
		if (!Directory.Exists(result.SavedDirectory))
		{
			result.Warnings.Add("ShooterGame/Saved folder was not found. It may be created on first launch.");
		}
		if (!Directory.Exists(result.ConfigDirectory))
		{
			result.Warnings.Add("Config/WindowsServer folder was not found. It may be created on first launch.");
		}
		if (!File.Exists(result.GameUserSettingsPath))
		{
			result.Warnings.Add("GameUserSettings.ini was not found.");
		}
		if (!File.Exists(result.GameIniPath))
		{
			result.Warnings.Add("Game.ini was not found.");
		}
		if (!File.Exists(result.EngineIniPath))
		{
			result.Warnings.Add("Engine.ini was not found.");
		}
		if (string.IsNullOrWhiteSpace(result.SavedArksDirectory))
		{
			result.Warnings.Add("SavedArks folder was not found. A new server may not have save data yet.");
		}
		if (!Directory.Exists(result.LogsDirectory))
		{
			result.Warnings.Add("Logs folder was not found. It may be created on first launch.");
		}
		if (string.IsNullOrWhiteSpace(result.ModsDirectory))
		{
			result.Warnings.Add("Mods folder was not found. This is normal for unmodded servers.");
		}

		result.IsValidInstall = result.Errors.Count == 0;
		return result;
	}

	public void ApplyToServer(ServerInstance server, AsaServerDiscoveryResult discovery)
	{
		if (server == null || discovery == null)
		{
			return;
		}

		if (!string.IsNullOrWhiteSpace(discovery.InstallDirectory))
		{
			server.InstallDirectory = discovery.InstallDirectory;
		}
		if (!string.IsNullOrWhiteSpace(discovery.ExecutableName))
		{
			server.ExecutableName = discovery.ExecutableName;
		}
		if (!string.IsNullOrWhiteSpace(discovery.SavedDirectory))
		{
			server.SaveDirectory = discovery.SavedDirectory;
		}
		if (!string.IsNullOrWhiteSpace(discovery.ConfigDirectory))
		{
			server.ConfigDirectory = discovery.ConfigDirectory;
		}
		if (!string.IsNullOrWhiteSpace(discovery.LogsDirectory))
		{
			server.LogsDirectory = discovery.LogsDirectory;
		}
		if (!string.IsNullOrWhiteSpace(discovery.ModsDirectory))
		{
			server.ModsDirectory = discovery.ModsDirectory;
		}
	}

	private static string ResolveInstallDirectory(string selectedPath)
	{
		DirectoryInfo? current = new DirectoryInfo(selectedPath);
		while (current != null)
		{
			if (string.Equals(current.Name, "ShooterGame", StringComparison.OrdinalIgnoreCase))
			{
				return current.Parent?.FullName ?? selectedPath;
			}
			if (Directory.Exists(Path.Combine(current.FullName, "ShooterGame")))
			{
				return current.FullName;
			}
			current = current.Parent;
		}

		string? executablePath = FindExecutable(selectedPath);
		if (!string.IsNullOrWhiteSpace(executablePath))
		{
			DirectoryInfo? executableDirectory = new DirectoryInfo(Path.GetDirectoryName(executablePath) ?? selectedPath);
			while (executableDirectory != null)
			{
				if (string.Equals(executableDirectory.Name, "ShooterGame", StringComparison.OrdinalIgnoreCase))
				{
					return executableDirectory.Parent?.FullName ?? selectedPath;
				}
				executableDirectory = executableDirectory.Parent;
			}
		}

		return selectedPath;
	}

	private static string ResolveExecutablePath(string installDirectory)
	{
		string[] candidates = ExecutableNames
			.SelectMany(name => new[]
			{
				Path.Combine(installDirectory, name),
				Path.Combine(installDirectory, "ShooterGame", "Binaries", "Win64", name)
			})
			.ToArray();

		string? candidate = candidates.FirstOrDefault(File.Exists);
		if (!string.IsNullOrWhiteSpace(candidate))
		{
			return candidate;
		}

		return FindExecutable(installDirectory) ?? string.Empty;
	}

	private static string ResolveSavedDirectory(string installDirectory, string selectedPath)
	{
		if (string.Equals(new DirectoryInfo(selectedPath).Name, "Saved", StringComparison.OrdinalIgnoreCase))
		{
			return selectedPath;
		}

		string savedDirectory = ResolveFirstExistingDirectory(
			Path.Combine(selectedPath, "ShooterGame", "Saved"),
			Path.Combine(installDirectory, "ShooterGame", "Saved"),
			Path.Combine(selectedPath, "Saved"));
		return string.IsNullOrWhiteSpace(savedDirectory) ? Path.Combine(installDirectory, "ShooterGame", "Saved") : savedDirectory;
	}

	private static string? FindExecutable(string rootDirectory)
	{
		if (!Directory.Exists(rootDirectory))
		{
			return null;
		}

		foreach (string executableName in ExecutableNames)
		{
			try
			{
				string? path = Directory.EnumerateFiles(rootDirectory, executableName, SearchOption.AllDirectories).FirstOrDefault();
				if (!string.IsNullOrWhiteSpace(path))
				{
					return path;
				}
			}
			catch
			{
				return null;
			}
		}

		return null;
	}

	private static string ResolveFirstExistingDirectory(params string[] paths)
	{
		return paths.FirstOrDefault(Directory.Exists) ?? string.Empty;
	}
}
