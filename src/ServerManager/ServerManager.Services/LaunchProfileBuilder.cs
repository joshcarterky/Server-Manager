using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServerManager.Models;

namespace ServerManager.Services;

public class LaunchProfileBuilder : ILaunchProfileBuilder
{
	public LaunchProfile Build(ServerInstance server, string executablePath)
	{
		string rconPassword = GetRconPassword(server);
		string mapPackageName = GetMapPackageName(server.MapName);
		string mapUrl = mapPackageName
			+ "?SessionName=" + CleanMapOptionValue(server.Name)
			+ "?ServerAdminPassword=" + CleanMapOptionValue(rconPassword)
			+ "?RCONEnabled=" + (server.Config.UseRcon ? "True" : "False")
			+ "?RCONPort=" + server.RconPort;

		List<string> managedArguments = new List<string>
		{
			QuoteLaunchArgument(mapUrl),
			"-server",
			"-NoLogWindow",
			"-stdout",
			"-FullStdOutLogOutput",
			$"-port={server.GamePort}",
			$"-queryport={server.QueryPort}",
			$"-RCONPort={server.RconPort}",
			"-RCONEnabled=" + (server.Config.UseRcon ? "True" : "False"),
			"-ServerAdminPassword=" + rconPassword,
			"-Crossplay=" + (server.CrossplayEnabled ? "true" : "false")
		};

		if (!server.BattleEyeEnabled || !server.Config.UseBattleEye)
		{
			managedArguments.Add("-NoBattlEye");
		}
		if (!string.IsNullOrWhiteSpace(server.ServerPassword))
		{
			managedArguments.Add("-serverpassword=" + server.ServerPassword);
		}
		if (!string.IsNullOrWhiteSpace(server.AdminPassword))
		{
			managedArguments.Add("-adminpassword=" + server.AdminPassword);
		}
		if (!string.IsNullOrWhiteSpace(server.ClusterId))
		{
			managedArguments.Add("-clusterid=" + server.ClusterId);
			if (!string.IsNullOrWhiteSpace(server.ClusterDirectory))
			{
				managedArguments.Add("-ClusterDirOverride=\"" + server.ClusterDirectory.Trim().Trim('"') + "\"");
			}
			if (server.NoTransferFromFiltering)
			{
				managedArguments.Add("-NoTransferFromFiltering");
			}
		}

		string customArguments = RemoveManagedLaunchArguments(server.LaunchParameters);
		if (!string.IsNullOrWhiteSpace(customArguments))
		{
			managedArguments.Add(customArguments);
		}

		if (server.Mods.Count > 0 && !managedArguments.Any((string x) => x.StartsWith("-mods=", StringComparison.OrdinalIgnoreCase)))
		{
			string modIds = string.Join(",", server.Mods
				.OrderBy((ModEntry x) => x.LoadOrder)
				.Select((ModEntry x) => x.WorkshopId)
				.Where((string x) => !string.IsNullOrWhiteSpace(x)));
			if (!string.IsNullOrWhiteSpace(modIds))
			{
				managedArguments.Add("-mods=" + modIds);
			}
		}

		return new LaunchProfile
		{
			ExecutablePath = executablePath,
			WorkingDirectory = Path.GetDirectoryName(executablePath) ?? server.InstallDirectory,
			MapPackageName = mapPackageName,
			MapUrl = mapUrl,
			ManagedArguments = managedArguments,
			CustomArguments = customArguments,
			Arguments = string.Join(' ', managedArguments.Where((string x) => !string.IsNullOrWhiteSpace(x)))
		};
	}

	public string RemoveManagedLaunchArguments(string launchParameters)
	{
		if (string.IsNullOrWhiteSpace(launchParameters))
		{
			return string.Empty;
		}

		IEnumerable<string> values = launchParameters.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Where((string part) =>
				!part.StartsWith("-clusterid=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-ClusterDirOverride=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-MaxPlayers=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-port=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-queryport=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-RCONPort=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-RCONEnabled=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-ServerAdminPassword=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-serverpassword=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-adminpassword=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-Crossplay=", StringComparison.OrdinalIgnoreCase)
				&& !part.StartsWith("-mods=", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-server", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-NoTransferFromFiltering", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-NoBattlEye", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-NoBattleEye", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-log", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-NoLogWindow", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-stdout", StringComparison.OrdinalIgnoreCase)
				&& !part.Equals("-FullStdOutLogOutput", StringComparison.OrdinalIgnoreCase));
		return string.Join(' ', values);
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
}
