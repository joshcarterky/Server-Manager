using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ServerManager.Models;

public class ServerInstance : INotifyPropertyChanged
{
	private bool _isOnline;

	private bool _isJoinable;

	private double _cpuUsage;

	private long _memoryUsageMb;

	private int _playerCount;

	private TimeSpan _uptime;

	private string _mapName = "TheIsland_WP";

	private string _installDirectory = string.Empty;

	private string _saveDirectory = string.Empty;

	private string _configDirectory = string.Empty;

	private string _logsDirectory = string.Empty;

	private string _modsDirectory = string.Empty;

	private string _clusterDirectory = string.Empty;

	private string _name = "New Server";

	private string _gameId = GameProfileIds.ArkSurvivalAscended;

	public Guid Id { get; set; } = Guid.NewGuid();


	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			SetField(ref _name, value, "Name");
		}
	}


	public string InstallDirectory
	{
		get
		{
			return _installDirectory;
		}
		set
		{
			SetField(ref _installDirectory, value, "InstallDirectory");
		}
	}

	public string ExecutableName { get; set; } = "ArkAscendedServer.exe";

	public string SaveDirectory
	{
		get
		{
			return _saveDirectory;
		}
		set
		{
			SetField(ref _saveDirectory, value, "SaveDirectory");
		}
	}

	public string ConfigDirectory
	{
		get
		{
			return _configDirectory;
		}
		set
		{
			SetField(ref _configDirectory, value, "ConfigDirectory");
		}
	}

	public string LogsDirectory
	{
		get
		{
			return _logsDirectory;
		}
		set
		{
			SetField(ref _logsDirectory, value, "LogsDirectory");
		}
	}

	public string ModsDirectory
	{
		get
		{
			return _modsDirectory;
		}
		set
		{
			SetField(ref _modsDirectory, value, "ModsDirectory");
		}
	}


	public string LaunchParameters { get; set; } = "";


	public int AppId { get; set; } = 2430930;


	public string GameId
	{
		get
		{
			return _gameId;
		}
		set
		{
			if (SetField(ref _gameId, string.IsNullOrWhiteSpace(value) ? GameProfileIds.ArkSurvivalAscended : value, "GameId"))
			{
				OnPropertyChanged("GameDisplayName");
			}
		}
	}

	[JsonIgnore]
	public string GameDisplayName => GameProfileCatalog.Get(GameId).DisplayName;


	public int QueryPort { get; set; } = 27015;


	public int GamePort { get; set; } = 7777;


	public int RconPort { get; set; } = 32330;


	public string RconPassword { get; set; } = string.Empty;


	public string ServerPassword { get; set; } = string.Empty;


	public string AdminPassword { get; set; } = string.Empty;


	public int MaxPlayers { get; set; } = 10;


	public double XPMultiplier { get; set; } = 1.0;


	public double HarvestMultiplier { get; set; } = 1.0;


	public double TamingSpeedMultiplier { get; set; } = 1.0;


	public double DifficultyOffset { get; set; } = 1.0;


	public bool CrossplayEnabled { get; set; } = true;


	public bool BattleEyeEnabled { get; set; }

	public bool AutoUpdateEnabled { get; set; } = true;


	public bool AutoRestartOnCrash { get; set; } = true;


	public string MapName
	{
		get
		{
			return _mapName;
		}
		set
		{
			if (SetField(ref _mapName, value, "MapName"))
			{
				OnPropertyChanged("MapImageSource");
			}
		}
	}

	public string ClusterId { get; set; } = string.Empty;


	public string ClusterDirectory
	{
		get
		{
			return _clusterDirectory;
		}
		set
		{
			SetField(ref _clusterDirectory, value, "ClusterDirectory");
		}
	}

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

	public List<ModEntry> Mods { get; set; } = new List<ModEntry>();


	public ServerConfigSettings Config { get; set; } = new ServerConfigSettings();


	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


	[JsonIgnore]
	public bool IsOnline
	{
		get
		{
			return _isOnline;
		}
		set
		{
			SetField(ref _isOnline, value, "IsOnline");
		}
	}

	[JsonIgnore]
	public bool IsJoinable
	{
		get
		{
			return _isJoinable;
		}
		set
		{
			SetField(ref _isJoinable, value, "IsJoinable");
		}
	}

	[JsonIgnore]
	public double CpuUsage
	{
		get
		{
			return _cpuUsage;
		}
		set
		{
			if (SetField(ref _cpuUsage, value, "CpuUsage"))
			{
				OnPropertyChanged("CpuUsageText");
			}
		}
	}

	[JsonIgnore]
	public long MemoryUsageMB
	{
		get
		{
			return _memoryUsageMb;
		}
		set
		{
			if (SetField(ref _memoryUsageMb, value, "MemoryUsageMB"))
			{
				OnPropertyChanged("MemoryUsageText");
			}
		}
	}

	[JsonIgnore]
	public int PlayerCount
	{
		get
		{
			return _playerCount;
		}
		set
		{
			SetField(ref _playerCount, value, "PlayerCount");
		}
	}

	[JsonIgnore]
	public TimeSpan Uptime
	{
		get
		{
			return _uptime;
		}
		set
		{
			SetField(ref _uptime, value, "Uptime");
		}
	}

	[JsonIgnore]
	public string StatusText
	{
		get
		{
			if (!IsOnline)
			{
				return "Offline";
			}
			if (!IsJoinable)
			{
				return "Starting";
			}
			return "Ready";
		}
	}

	[JsonIgnore]
	public string CpuUsageText
	{
		get
		{
			if (!IsOnline)
			{
				return "--";
			}
			return $"{CpuUsage:0.0}%";
		}
	}

	[JsonIgnore]
	public string MemoryUsageText
	{
		get
		{
			if (!IsOnline)
			{
				return "--";
			}
			return $"{MemoryUsageMB:N0} MB";
		}
	}

	[JsonIgnore]
	public string MapImageSource => "pack://application:,,,/Assets/Maps/" + GetMapAssetName(MapName) + ".png";

	public event PropertyChangedEventHandler? PropertyChanged;

	public void UpdateRuntimeStatus(ServerStatus status)
	{
		IsOnline = status.IsOnline;
		IsJoinable = status.IsJoinable;
		CpuUsage = status.CpuUsage;
		MemoryUsageMB = status.MemoryUsageMB;
		PlayerCount = status.PlayerCount;
		Uptime = status.Uptime;
		OnPropertyChanged("StatusText");
		OnPropertyChanged("CpuUsageText");
		OnPropertyChanged("MemoryUsageText");
		OnPropertyChanged("PlayerCount");
	}

	private static string GetMapAssetName(string mapName)
	{
		string normalized = (mapName ?? string.Empty).Trim() switch
		{
			"TheIsland_WP" or "TheIsland" => "TheIsland",
			"ScorchedEarth_WP" or "ScorchedEarth" => "ScorchedEarth",
			"TheCenter_WP" or "TheCenter" => "TheCenter",
			"Aberration_WP" or "Aberration" => "Aberration",
			"Extinction_WP" or "Extinction" => "Extinction",
			"Ragnarok_WP" or "Ragnarok" => "Ragnarok",
			"Valguero_WP" or "Valguero" => "Valguero",
			_ => "TheIsland"
		};
		return normalized;
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
