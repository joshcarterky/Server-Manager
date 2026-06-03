namespace ServerManager.Models;

public class ServerConfigSettings
{
	public bool UseBattleEye { get; set; }

	public bool UseRcon { get; set; } = true;


	public int MaxPlayers { get; set; } = 10;


	public int Port { get; set; } = 7777;


	public int QueryPort { get; set; } = 27015;


	public int RconPort { get; set; } = 32330;


	public string RconPassword { get; set; } = string.Empty;


	public string ServerPassword { get; set; } = string.Empty;


	public string AdminPassword { get; set; } = string.Empty;


	public string ClusterId { get; set; } = string.Empty;


	public string ClusterDirectory { get; set; } = string.Empty;


	public bool NoTransferFromFiltering { get; set; } = true;


	public bool NoTributeDownloads { get; set; }

	public bool PreventDownloadSurvivors { get; set; }

	public bool PreventDownloadItems { get; set; }

	public bool PreventDownloadDinos { get; set; }

	public bool PreventUploadSurvivors { get; set; }

	public bool PreventUploadItems { get; set; }

	public bool PreventUploadDinos { get; set; }

	public double MinimumDinoReuploadInterval { get; set; }

	public int TributeCharacterExpirationSeconds { get; set; }

	public int TributeDinoExpirationSeconds { get; set; }

	public int TributeItemExpirationSeconds { get; set; }

	public bool CrossplayEnabled { get; set; } = true;


	public bool AutoSaveEnabled { get; set; } = true;


	public int SaveIntervalMinutes { get; set; } = 15;

}
