using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ServerManager.Helpers;
using ServerManager.Models;

namespace ServerManager.Services;

public class SteamCmdService : ISteamCmdService
{
	private const string SteamCmdUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";

	private readonly IConfigService _configService;

	private readonly IHttpClientFactory _httpClientFactory;

	private readonly ILoggingService _logger;

	public SteamCmdService(IConfigService configService, IHttpClientFactory httpClientFactory, ILoggingService logger)
	{
		_configService = configService;
		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	public async Task<string> GetSteamCmdPathAsync()
	{
		AppConfig appConfig = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (string.IsNullOrWhiteSpace(appConfig.SteamCmdDirectory))
		{
			appConfig.SteamCmdDirectory = Path.Combine(AppContext.BaseDirectory, "steamcmd");
		}
		string steamCmdPath = Path.Combine(appConfig.SteamCmdDirectory, "steamcmd.exe");
		if (!File.Exists(steamCmdPath))
		{
			await EnsureSteamCmdAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		return steamCmdPath;
	}

	public async Task EnsureSteamCmdAsync()
	{
		AppConfig appConfig = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		string installDirectory = (string.IsNullOrWhiteSpace(appConfig.SteamCmdDirectory) ? Path.Combine(AppContext.BaseDirectory, "steamcmd") : appConfig.SteamCmdDirectory);
		FileHelpers.EnsureDirectory(installDirectory);
		string steamCmdZip = Path.Combine(installDirectory, "steamcmd.zip");
		if (File.Exists(Path.Combine(installDirectory, "steamcmd.exe")))
		{
			return;
		}
		if (File.Exists(steamCmdZip))
		{
			File.Delete(steamCmdZip);
		}
		using HttpClient client = _httpClientFactory.CreateClient();
		using HttpResponseMessage response = await client.GetAsync(SteamCmdUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			FileStream fileStream = File.Create(steamCmdZip);
			try
			{
				await stream.CopyToAsync(fileStream).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				if (fileStream != null)
				{
					await fileStream.DisposeAsync();
				}
			}
			ZipFile.ExtractToDirectory(steamCmdZip, installDirectory, overwriteFiles: true);
			File.Delete(steamCmdZip);
		}
		finally
		{
			if (stream != null)
			{
				await stream.DisposeAsync();
			}
		}
	}

	public async Task InstallOrUpdateServerAsync(ServerInstance server, IProgress<string> progress)
	{
		string steamCmdPath = await GetSteamCmdPathAsync().ConfigureAwait(continueOnCapturedContext: false);
		string installDirectory = server.InstallDirectory;
		FileHelpers.EnsureDirectory(installDirectory);
		int appId = GetAppId(server);
		string arguments = $"+force_install_dir \"{installDirectory}\" +login anonymous +app_update {appId} validate +quit";
		progress.Report("Starting SteamCMD to install or update the server...");
		int num = await RunSteamCmdInTerminalAsync(steamCmdPath, arguments, installDirectory, server.Name).ConfigureAwait(continueOnCapturedContext: false);
		if (num != 0)
		{
			_logger.Logger.Warning("SteamCMD returned exit code {ExitCode} for server {ServerName}", num, server.Name);
			throw new InvalidOperationException($"SteamCMD failed with exit code {num}.");
		}
		progress.Report("Server files installed/updated successfully.");
	}

	public async Task ValidateServerAsync(ServerInstance server, IProgress<string> progress)
	{
		string fileName = await GetSteamCmdPathAsync().ConfigureAwait(continueOnCapturedContext: false);
		string installDirectory = server.InstallDirectory;
		FileHelpers.EnsureDirectory(installDirectory);
		string arguments = $"+force_install_dir \"{installDirectory}\" +login anonymous +app_update {GetAppId(server)} validate +quit";
		progress.Report("Validating server files with SteamCMD...");
		int num = await ProcessRunner.RunAsync(fileName, arguments, installDirectory, progress).ConfigureAwait(continueOnCapturedContext: false);
		if (num != 0)
		{
			throw new InvalidOperationException($"SteamCMD validate failed with status {num}.");
		}
		progress.Report("Validation complete.");
	}

	private static int GetAppId(ServerInstance server)
	{
		if (server.AppId != 1895330)
		{
			return server.AppId;
		}
		return 2430930;
	}

	private static async Task<int> RunSteamCmdInTerminalAsync(string steamCmdPath, string arguments, string installDirectory, string serverName)
	{
		string scriptPath = Path.Combine(Path.GetDirectoryName(steamCmdPath) ?? AppContext.BaseDirectory, "server-install-update.cmd");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("@echo off");
		stringBuilder.AppendLine("setlocal");
		stringBuilder.AppendLine("title Dedicated Server Manager - Install / Update");
		stringBuilder.AppendLine("echo Dedicated Server Manager - Install / Update");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder2);
		handler.AppendLiteral("echo Server: ");
		handler.AppendFormatted(serverName);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder2);
		handler.AppendLiteral("echo Install directory: ");
		handler.AppendFormatted(installDirectory);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder.AppendLine("echo.");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
		handler.AppendLiteral("\"");
		handler.AppendFormatted(steamCmdPath);
		handler.AppendLiteral("\" ");
		handler.AppendFormatted(arguments);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine("set STEAM_EXIT=%ERRORLEVEL%");
		stringBuilder.AppendLine("if not \"%STEAM_EXIT%\"==\"7\" goto steamcmd_done");
		stringBuilder.AppendLine("echo.");
		stringBuilder.AppendLine("echo SteamCMD reported exit code 7. This can happen on first run while SteamCMD finishes setup.");
		stringBuilder.AppendLine("echo Waiting 10 seconds, then retrying once...");
		stringBuilder.AppendLine("timeout /t 10 /nobreak >nul");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
		handler.AppendLiteral("\"");
		handler.AppendFormatted(steamCmdPath);
		handler.AppendLiteral("\" ");
		handler.AppendFormatted(arguments);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder.AppendLine("set STEAM_EXIT=%ERRORLEVEL%");
		stringBuilder.AppendLine(":steamcmd_done");
		stringBuilder.AppendLine("echo.");
		stringBuilder.AppendLine("echo SteamCMD exited with code %STEAM_EXIT%.");
		stringBuilder.AppendLine("echo Press any key to close this window...");
		stringBuilder.AppendLine("pause >nul");
		stringBuilder.AppendLine("exit /b %STEAM_EXIT%");
		await File.WriteAllTextAsync(scriptPath, stringBuilder.ToString()).ConfigureAwait(continueOnCapturedContext: false);
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = "/c \"\"" + scriptPath + "\"\"",
			WorkingDirectory = installDirectory,
			UseShellExecute = true
		};
		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start SteamCMD terminal.");
		await process.WaitForExitAsync().ConfigureAwait(continueOnCapturedContext: false);
		return process.ExitCode;
	}
}
