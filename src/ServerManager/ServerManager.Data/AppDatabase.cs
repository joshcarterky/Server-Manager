using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace ServerManager.Data;

public class AppDatabase : IAppDatabase
{
	private static readonly string[] SchemaCommands = new string[8] { "CREATE TABLE IF NOT EXISTS Servers (\n    Id TEXT PRIMARY KEY,\n    Name TEXT NOT NULL,\n    MapName TEXT NOT NULL,\n    InstallDirectory TEXT NOT NULL DEFAULT '',\n    SaveDirectory TEXT NOT NULL DEFAULT '',\n    LaunchArguments TEXT NOT NULL DEFAULT '',\n    ClusterId TEXT NOT NULL DEFAULT '',\n    GamePort INTEGER NOT NULL,\n    QueryPort INTEGER NOT NULL,\n    RconPort INTEGER NOT NULL,\n    AdminPassword TEXT NOT NULL DEFAULT '',\n    ServerPassword TEXT NOT NULL DEFAULT '',\n    IsRemote INTEGER NOT NULL DEFAULT 0,\n    RemoteHost TEXT NOT NULL DEFAULT '',\n    CreatedAt TEXT NOT NULL,\n    UpdatedAt TEXT NOT NULL\n);", "CREATE TABLE IF NOT EXISTS Mods (\n    Id TEXT PRIMARY KEY,\n    ServerId TEXT NULL,\n    CurseForgeId TEXT NOT NULL DEFAULT '',\n    Name TEXT NOT NULL,\n    Author TEXT NOT NULL DEFAULT '',\n    ThumbnailUrl TEXT NOT NULL DEFAULT '',\n    InstalledVersion TEXT NOT NULL DEFAULT '',\n    LatestVersion TEXT NOT NULL DEFAULT '',\n    SizeBytes INTEGER NOT NULL DEFAULT 0,\n    LastUpdated TEXT NULL,\n    LoadOrder INTEGER NOT NULL DEFAULT 0,\n    IsEnabled INTEGER NOT NULL DEFAULT 1,\n    IsBroken INTEGER NOT NULL DEFAULT 0,\n    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE CASCADE\n);", "CREATE TABLE IF NOT EXISTS ConfigMetadata (\n    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n    VariableName TEXT NOT NULL,\n    Description TEXT NOT NULL DEFAULT '',\n    DefaultValue TEXT NOT NULL DEFAULT '',\n    DataType TEXT NOT NULL,\n    MinValue REAL NULL,\n    MaxValue REAL NULL,\n    FileLocation TEXT NOT NULL,\n    IniSection TEXT NOT NULL,\n    Category TEXT NOT NULL,\n    RequiresRestart INTEGER NOT NULL DEFAULT 0\n);", "CREATE TABLE IF NOT EXISTS BackupHistory (\n    Id TEXT PRIMARY KEY,\n    ServerId TEXT NOT NULL,\n    FilePath TEXT NOT NULL,\n    BackupType TEXT NOT NULL,\n    SizeBytes INTEGER NOT NULL DEFAULT 0,\n    IsValid INTEGER NOT NULL DEFAULT 0,\n    CreatedAt TEXT NOT NULL,\n    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE CASCADE\n);", "CREATE TABLE IF NOT EXISTS ServerTemplates (\n    Id TEXT PRIMARY KEY,\n    Name TEXT NOT NULL,\n    Description TEXT NOT NULL DEFAULT '',\n    TemplateJson TEXT NOT NULL,\n    IsBuiltIn INTEGER NOT NULL DEFAULT 0,\n    CreatedAt TEXT NOT NULL\n);", "CREATE TABLE IF NOT EXISTS ValidationFindings (\n    Id TEXT PRIMARY KEY,\n    ServerId TEXT NULL,\n    Severity TEXT NOT NULL,\n    Category TEXT NOT NULL,\n    Message TEXT NOT NULL,\n    AutoFixSuggestion TEXT NOT NULL DEFAULT '',\n    CreatedAt TEXT NOT NULL,\n    ResolvedAt TEXT NULL\n);", "CREATE TABLE IF NOT EXISTS AppLogs (\n    Id TEXT PRIMARY KEY,\n    Level TEXT NOT NULL,\n    Source TEXT NOT NULL DEFAULT '',\n    Message TEXT NOT NULL,\n    CreatedAt TEXT NOT NULL\n);", "CREATE TABLE IF NOT EXISTS UserSettings (\n    Key TEXT PRIMARY KEY,\n    Value TEXT NOT NULL\n);" };

	public string DatabasePath { get; }

	static AppDatabase()
	{
		Batteries_V2.Init();
	}

	public AppDatabase()
	{
		string text = Path.Combine(AppContext.BaseDirectory, "Data");
		Directory.CreateDirectory(text);
		DatabasePath = Path.Combine(text, "Server-Manager.db");
	}

	public async Task InitializeAsync()
	{
		SqliteConnection connection = new SqliteConnection("Data Source=" + DatabasePath);
		try
		{
			await connection.OpenAsync().ConfigureAwait(continueOnCapturedContext: false);
			string[] schemaCommands = SchemaCommands;
			foreach (string commandText in schemaCommands)
			{
				SqliteCommand command = connection.CreateCommand();
				try
				{
					command.CommandText = commandText;
					await command.ExecuteNonQueryAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				finally
				{
					if (command != null)
					{
						await command.DisposeAsync();
					}
				}
			}
		}
		finally
		{
			if (connection != null)
			{
				await connection.DisposeAsync();
			}
		}
	}
}
