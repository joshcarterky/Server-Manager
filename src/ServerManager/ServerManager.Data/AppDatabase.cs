using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace ServerManager.Data;

public class AppDatabase : IAppDatabase
{
	private static readonly string[] SchemaCommands =
	{
		@"CREATE TABLE IF NOT EXISTS Servers (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    GameId TEXT NOT NULL DEFAULT 'asa',
    AppId INTEGER NOT NULL DEFAULT 2430930,
    MapName TEXT NOT NULL,
    InstallDirectory TEXT NOT NULL DEFAULT '',
    SaveDirectory TEXT NOT NULL DEFAULT '',
    ExecutableName TEXT NOT NULL DEFAULT 'ArkAscendedServer.exe',
    LaunchArguments TEXT NOT NULL DEFAULT '',
    ClusterId TEXT NULL,
    GamePort INTEGER NOT NULL,
    QueryPort INTEGER NOT NULL,
    RconPort INTEGER NOT NULL,
    RconEnabled INTEGER NOT NULL DEFAULT 1,
    AdminPassword TEXT NOT NULL DEFAULT '',
    ServerPassword TEXT NOT NULL DEFAULT '',
    AdminPasswordSecretId TEXT NULL,
    ServerPasswordSecretId TEXT NULL,
    MaxPlayers INTEGER NOT NULL DEFAULT 70,
    AutoUpdateEnabled INTEGER NOT NULL DEFAULT 1,
    AutoRestartOnCrash INTEGER NOT NULL DEFAULT 1,
    IsRemote INTEGER NOT NULL DEFAULT 0,
    RemoteHost TEXT NOT NULL DEFAULT '',
    Status TEXT NOT NULL DEFAULT 'Offline',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);",
		@"CREATE TABLE IF NOT EXISTS Clusters (
    Id TEXT PRIMARY KEY,
    ClusterKey TEXT NOT NULL UNIQUE,
    DisplayName TEXT NOT NULL,
    SharedStoragePath TEXT NOT NULL,
    Enabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);",
		@"CREATE TABLE IF NOT EXISTS Mods (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NULL,
    ProjectId TEXT NOT NULL DEFAULT '',
    CurseForgeId TEXT NOT NULL DEFAULT '',
    FileId TEXT NULL,
    Name TEXT NOT NULL,
    Author TEXT NOT NULL DEFAULT '',
    ThumbnailUrl TEXT NOT NULL DEFAULT '',
    Version TEXT NOT NULL DEFAULT '',
    InstalledVersion TEXT NOT NULL DEFAULT '',
    LatestVersion TEXT NOT NULL DEFAULT '',
    SizeBytes INTEGER NOT NULL DEFAULT 0,
    LastUpdated TEXT NULL,
    LoadOrder INTEGER NOT NULL DEFAULT 0,
    Enabled INTEGER NOT NULL DEFAULT 1,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    IsBroken INTEGER NOT NULL DEFAULT 0,
    DependencyJson TEXT NOT NULL DEFAULT '[]',
    LastCheckedAt TEXT NULL,
    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE CASCADE
);",
		@"CREATE TABLE IF NOT EXISTS ConfigMetadata (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    VariableName TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    DefaultValue TEXT NOT NULL DEFAULT '',
    DataType TEXT NOT NULL,
    MinValue REAL NULL,
    MaxValue REAL NULL,
    FileLocation TEXT NOT NULL,
    IniSection TEXT NOT NULL,
    Category TEXT NOT NULL,
    RequiresRestart INTEGER NOT NULL DEFAULT 0
);",
		@"CREATE TABLE IF NOT EXISTS Backups (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NULL,
    ClusterId TEXT NULL,
    BackupType TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    SizeBytes INTEGER NOT NULL DEFAULT 0,
    ManifestJson TEXT NOT NULL DEFAULT '{}',
    Sha256 TEXT NOT NULL DEFAULT '',
    IsValid INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE SET NULL,
    FOREIGN KEY(ClusterId) REFERENCES Clusters(Id) ON DELETE SET NULL
);",
		@"CREATE TABLE IF NOT EXISTS BackupHistory (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    BackupType TEXT NOT NULL,
    SizeBytes INTEGER NOT NULL DEFAULT 0,
    IsValid INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE CASCADE
);",
		@"CREATE TABLE IF NOT EXISTS Schedules (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NULL,
    ClusterId TEXT NULL,
    Name TEXT NOT NULL,
    TaskType TEXT NOT NULL,
    CronExpression TEXT NOT NULL,
    PayloadJson TEXT NOT NULL DEFAULT '{}',
    Enabled INTEGER NOT NULL DEFAULT 1,
    LastRunAt TEXT NULL,
    NextRunAt TEXT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE CASCADE,
    FOREIGN KEY(ClusterId) REFERENCES Clusters(Id) ON DELETE CASCADE
);",
		@"CREATE TABLE IF NOT EXISTS Users (
    Id TEXT PRIMARY KEY,
    Username TEXT NOT NULL UNIQUE,
    DisplayName TEXT NOT NULL,
    Role TEXT NOT NULL DEFAULT 'Admin',
    PasswordHash TEXT NULL,
    CreatedAt TEXT NOT NULL,
    LastLoginAt TEXT NULL
);",
		@"CREATE TABLE IF NOT EXISTS Logs (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NULL,
    Level TEXT NOT NULL,
    Source TEXT NOT NULL,
    Message TEXT NOT NULL,
    DataJson TEXT NOT NULL DEFAULT '{}',
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE SET NULL
);",
		@"CREATE TABLE IF NOT EXISTS ServerTemplates (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT NOT NULL DEFAULT '',
    TemplateJson TEXT NOT NULL,
    IsBuiltIn INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL
);",
		@"CREATE TABLE IF NOT EXISTS ValidationFindings (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NULL,
    Severity TEXT NOT NULL,
    Category TEXT NOT NULL,
    Message TEXT NOT NULL,
    AutoFixSuggestion TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL,
    ResolvedAt TEXT NULL
);",
		@"CREATE TABLE IF NOT EXISTS AppLogs (
    Id TEXT PRIMARY KEY,
    Level TEXT NOT NULL,
    Source TEXT NOT NULL DEFAULT '',
    Message TEXT NOT NULL,
    CreatedAt TEXT NOT NULL
);",
		@"CREATE TABLE IF NOT EXISTS Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);",
		@"CREATE TABLE IF NOT EXISTS UserSettings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);"
	};

	private static readonly string[] IndexCommands =
	{
		"CREATE INDEX IF NOT EXISTS IX_Mods_ServerId_LoadOrder ON Mods(ServerId, LoadOrder);",
		"CREATE INDEX IF NOT EXISTS IX_Backups_ServerId_CreatedAt ON Backups(ServerId, CreatedAt DESC);",
		"CREATE INDEX IF NOT EXISTS IX_Schedules_NextRunAt ON Schedules(Enabled, NextRunAt);",
		"CREATE INDEX IF NOT EXISTS IX_Logs_ServerId_CreatedAt ON Logs(ServerId, CreatedAt DESC);",
		"CREATE INDEX IF NOT EXISTS IX_AppLogs_CreatedAt ON AppLogs(CreatedAt DESC);"
	};

	private static readonly IReadOnlyDictionary<string, string[]> AdditiveColumns = new Dictionary<string, string[]>
	{
		["Servers"] = new[]
		{
			"GameId TEXT NOT NULL DEFAULT 'asa'",
			"AppId INTEGER NOT NULL DEFAULT 2430930",
			"ExecutableName TEXT NOT NULL DEFAULT 'ArkAscendedServer.exe'",
			"RconEnabled INTEGER NOT NULL DEFAULT 1",
			"AdminPasswordSecretId TEXT NULL",
			"ServerPasswordSecretId TEXT NULL",
			"MaxPlayers INTEGER NOT NULL DEFAULT 70",
			"AutoUpdateEnabled INTEGER NOT NULL DEFAULT 1",
			"AutoRestartOnCrash INTEGER NOT NULL DEFAULT 1",
			"Status TEXT NOT NULL DEFAULT 'Offline'"
		},
		["Mods"] = new[]
		{
			"ProjectId TEXT NOT NULL DEFAULT ''",
			"FileId TEXT NULL",
			"Version TEXT NOT NULL DEFAULT ''",
			"Enabled INTEGER NOT NULL DEFAULT 1",
			"DependencyJson TEXT NOT NULL DEFAULT '[]'",
			"LastCheckedAt TEXT NULL"
		},
		["AppLogs"] = new[]
		{
			"Source TEXT NOT NULL DEFAULT ''"
		}
	};

	public string DatabasePath { get; }

	static AppDatabase()
	{
		Batteries_V2.Init();
	}

	public AppDatabase()
	{
		string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
		Directory.CreateDirectory(dataDirectory);
		DatabasePath = Path.Combine(dataDirectory, "Server-Manager.db");
	}

	public async Task InitializeAsync()
	{
		await using SqliteConnection connection = new SqliteConnection("Data Source=" + DatabasePath);
		await connection.OpenAsync().ConfigureAwait(false);

		await ExecuteNonQueryAsync(connection, "PRAGMA foreign_keys = ON;").ConfigureAwait(false);
		await ExecuteNonQueryAsync(connection, "PRAGMA journal_mode = WAL;").ConfigureAwait(false);

		foreach (string commandText in SchemaCommands)
		{
			await ExecuteNonQueryAsync(connection, commandText).ConfigureAwait(false);
		}

		foreach (KeyValuePair<string, string[]> tableColumns in AdditiveColumns)
		{
			foreach (string columnDefinition in tableColumns.Value)
			{
				await EnsureColumnAsync(connection, tableColumns.Key, columnDefinition).ConfigureAwait(false);
			}
		}

		foreach (string commandText in IndexCommands)
		{
			await ExecuteNonQueryAsync(connection, commandText).ConfigureAwait(false);
		}
	}

	private static async Task EnsureColumnAsync(SqliteConnection connection, string tableName, string columnDefinition)
	{
		string columnName = columnDefinition.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
		if (await ColumnExistsAsync(connection, tableName, columnName).ConfigureAwait(false))
		{
			return;
		}

		await ExecuteNonQueryAsync(connection, $"ALTER TABLE {tableName} ADD COLUMN {columnDefinition};").ConfigureAwait(false);
	}

	private static async Task<bool> ColumnExistsAsync(SqliteConnection connection, string tableName, string columnName)
	{
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = $"PRAGMA table_info({tableName});";
		await using SqliteDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
		while (await reader.ReadAsync().ConfigureAwait(false))
		{
			if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string commandText)
	{
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = commandText;
		await command.ExecuteNonQueryAsync().ConfigureAwait(false);
	}
}
