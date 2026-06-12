# ARK: Survival Ascended Server Manager Technical Design

Date: 2026-06-12

This document defines the technical baseline for a professional Windows ARK: Survival Ascended (ASA) dedicated server manager built with C#, .NET 8, WPF, MVVM, SQLite, dependency injection, async services, and background workers.

## Source Notes

Studio Wildcard does not publish a complete formal ASA dedicated-server operations manual. This design therefore separates:

- High-confidence vendor/platform facts: SteamCMD behavior, Steam app metadata, .NET architecture guidance, CurseForge API behavior.
- High-confidence operational facts: ASA filesystem paths, launch arguments, INI locations, RCON behavior, and clustering conventions validated by ARK admin documentation and server-hosting practice.
- Compatibility assumptions: ASA inherits many server config names from ASE, but every setting should be validated against actual server output and logs before being exposed as "known good."

Primary references:

- Valve SteamCMD documentation: https://developer.valvesoftware.com/wiki/SteamCMD
- SteamDB ASA Dedicated Server app 2430930 metadata: https://steamdb.info/app/2430930/info/
- ARK Official Community Wiki server configuration: https://ark.wiki.gg/wiki/Server_configuration
- CurseForge for Studios API documentation: https://docs.curseforge.com/rest-api/
- Microsoft dependency injection guidance: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview
- Microsoft Worker Service guidance: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- Microsoft EF/DbContext configuration guidance: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
- Community ASA RCON reference implementation: https://github.com/malkamius/ASA_RCon
- ARK cluster setup conventions: https://survivetheark.com/index.php?/forums/topic/87419-guide-cluster-setup/

## 1. ASA Dedicated Server Operations

### 1.1 Server Distribution

ASA dedicated server is distributed through Steam as app `2430930`, "ARK: Survival Ascended Dedicated Server." SteamDB currently reports it as a Windows tool using Unreal Engine, BattlEye, and Epic Online Services technology. SteamCMD should install/update it with anonymous login unless a future Steam change requires authentication.

Canonical install/update command:

```powershell
steamcmd.exe +force_install_dir "D:\ASAServers\TheIsland" +login anonymous +app_update 2430930 validate +quit
```

Valve recommends `force_install_dir` before login. `app_update <appid> validate` installs or verifies the app files.

### 1.2 Expected Folder Structure

Typical Windows install root:

```text
ServerRoot/
  Engine/
  ShooterGame/
    Binaries/
      Win64/
        ArkAscendedServer.exe
    Content/
    Saved/
      Config/
        WindowsServer/
          GameUserSettings.ini
          Game.ini
          Engine.ini
      Logs/
      SavedArks/
      SavedArksLocal/
```

Important paths:

- Executable: `ShooterGame/Binaries/Win64/ArkAscendedServer.exe`
- Config: `ShooterGame/Saved/Config/WindowsServer/`
- Saves: `ShooterGame/Saved/SavedArks/` or `ShooterGame/Saved/SavedArksLocal/`
- Logs: `ShooterGame/Saved/Logs/`
- Cluster storage: operator-defined shared folder passed with `-ClusterDirOverride=...`
- Mods: ASA uses CurseForge-hosted mods. Server-side resolved/downloaded mod content is managed by the game runtime and launcher flow, while the manager should persist mod IDs, metadata, desired load order, and validation state.

### 1.3 Startup Process

ASA is started by launching `ArkAscendedServer.exe` with:

- First argument: map URL with `?` options.
- Subsequent arguments: command switches beginning with `-`.

Representative launch:

```powershell
ArkAscendedServer.exe "TheIsland_WP?SessionName=My Server?ServerAdminPassword=secret?RCONEnabled=True?RCONPort=32330" -server -NoLogWindow -port=7777 -queryport=27015 -clusterid=mycluster -ClusterDirOverride="D:\ASAClusters\mycluster" -NoTransferFromFiltering -mods=123456,987654
```

Startup phases the manager should model:

1. Process creation and stdout/stderr capture.
2. Config discovery and INI merge.
3. Map package load.
4. Mod discovery/download/resolve/load in declared order.
5. Save load or world creation.
6. Cluster transfer storage initialization.
7. Network bind for game/query/RCON ports.
8. Server browser/EOS/Steam visibility initialization.
9. Runtime ready detection from logs and query/RCON availability.

### 1.4 Common Launch Parameters

Map URL options:

| Parameter | Purpose |
| --- | --- |
| `SessionName` | Public server name |
| `ServerAdminPassword` | Admin/RCON auth password |
| `ServerPassword` | Optional join password |
| `RCONEnabled` | Enables RCON |
| `RCONPort` | RCON TCP port |

Command switches:

| Switch | Purpose |
| --- | --- |
| `-server` | Dedicated server mode |
| `-log` / `-NoLogWindow` | Logging/window behavior |
| `-port=` | Game port, usually UDP |
| `-queryport=` | Server query port, usually UDP |
| `-RCONPort=` | RCON TCP port |
| `-MaxPlayers=` | Player slot cap |
| `-clusterid=` | Logical cluster identity |
| `-ClusterDirOverride=` | Shared transfer-data folder |
| `-NoTransferFromFiltering` | Allows transfer matching behavior used in clusters |
| `-mods=` | Comma-separated CurseForge/project mod IDs in load order |
| `-NoBattlEye` | Disable BattlEye |
| `-Crossplay=true/false` | Crossplay behavior where supported |

The manager must treat launch arguments as a generated artifact. User-provided custom arguments should be preserved but deduplicated against managed arguments.

### 1.5 Configuration Files

`GameUserSettings.ini`:

- Main runtime server settings.
- Common sections include `[ServerSettings]` and `[/Script/ShooterGame.ShooterGameUserSettings]`.
- Stores ports, passwords, rates, RCON, transfer flags, and many gameplay toggles.

`Game.ini`:

- Gameplay rules and mode-level settings.
- Common section: `[/script/shootergame.shootergamemode]`.
- Used for per-level stat multipliers, breeding rules, engrams, item stack overrides, spawn entries, etc.

`Engine.ini`:

- Lower-level Unreal/network/logging/platform settings.
- Should be advanced-editor only unless a setting is explicitly cataloged.

Design rules:

- Use a real INI parser that preserves comments, duplicate keys where allowed, section order, and array-style repeated keys.
- Maintain a setting catalog with key, file, section, type, default, min/max, restart requirement, validation rules, and description.
- Support advanced raw editing with parse validation before save.
- Apply managed settings through a typed model and write only owned values unless the user chooses "normalize file."

### 1.6 Save Data

Primary save files:

| File type | Meaning |
| --- | --- |
| `.ark` | Map/world save |
| `.arkprofile` | Survivor/player profiles |
| `.arktribe` | Tribe data |
| `.arktributetribe` | Tribute/transfer related tribe data |
| Cluster files | Character, dino, and item transfer payloads in cluster storage |

Backup policy:

- Stop or RCON `SaveWorld` before backup.
- Prefer quiesced copy: `SaveWorld`, wait for log confirmation or delay, then snapshot.
- Back up world save, profiles, tribe files, config files, launch manifest, mod manifest, and cluster folder if selected.
- Use zip archives with manifest JSON and checksums.
- Rotation should support count, age, and storage-size limits.
- Restore should verify server stopped, create a pre-restore safety backup, then replace target files.

### 1.7 Mod Management

ASA mods are CurseForge-based. The manager should integrate the CurseForge API for search and metadata where an API key is available, while still supporting manual mod/project IDs.

Required capabilities:

- Search by game/category/text.
- Add by project ID.
- Fetch latest file metadata and dependencies.
- Persist desired load order.
- Validate duplicates and missing dependencies.
- Generate `-mods=id1,id2,id3`.
- Detect mod updates and schedule restart-required updates.
- Warn that mod installation/update generally requires the server to be stopped or restarted to fully apply.

CurseForge integration model:

```text
CurseForgeService
  SearchModsAsync(query)
  GetModAsync(projectId)
  GetLatestFilesAsync(projectIds)
  GetDependenciesAsync(fileId)
```

### 1.8 Networking

Ports:

- Game port: usually UDP, default commonly `7777`.
- Query port: usually UDP, default commonly `27015`.
- RCON port: TCP, configurable, examples commonly `32330` or `27020`.

The manager should:

- Detect port conflicts across managed servers.
- Detect local bind conflicts using active TCP/UDP tables.
- Offer Windows Firewall rule creation for executable and explicit ports.
- Warn about NAT/router port forwarding for public hosting.
- Support per-server network profile: LAN-only, public, VPN, hosted remote.
- Verify externally only when the user opts in, because external checks require network services and may expose information.

### 1.9 SteamCMD

Lifecycle:

1. Detect existing SteamCMD.
2. Download/install SteamCMD if missing.
3. Update SteamCMD itself by running it once.
4. Install/update ASA with `app_update 2430930`.
5. Validate files on demand.
6. Parse SteamCMD output into progress events and structured failures.

Error handling:

- Network failure.
- Steam login failure.
- Disk full.
- Locked files because the server is running.
- Missing executable after "success."
- Partial install.

### 1.10 Monitoring

Monitoring layers:

- Process: PID, start time, exit code, CPU, RAM, handles.
- Log: rolling tail, ready/error pattern detection.
- Query: server online, map, players if query protocol responds.
- RCON: authenticated status, player list, command success.
- Filesystem: save timestamp, log activity, mod/config changes.

Crash detection:

- Unexpected process exit.
- Fatal/error log patterns.
- Watchdog restart policy with cooldown and max attempts.
- Crash bundle: last log slice, generated launch command, current config snapshot.

Network usage:

- Per-process network counters are limited on Windows without ETW/performance APIs. Initial release should show process CPU/RAM and optional host-level network totals; advanced release can add ETW.

### 1.11 Clustering

ASA clusters require every server in the group to use:

- Same cluster ID.
- Shared cluster transfer directory, usually `-ClusterDirOverride=...`.
- Compatible transfer rules.
- Unique game/query/RCON ports.
- Distinct install/save directories per map.

Cluster validation:

- All servers in cluster stopped before changing ID/path.
- Shared path exists and is writable by the manager/server process.
- No duplicate map unless intentionally allowed.
- No duplicate ports.
- Transfer flags consistent.
- Backups include cluster data or clearly exclude it.

### 1.12 RCON

RCON is enabled through `RCONEnabled=True`, `RCONPort`, and admin password. Common commands include:

- `ListPlayers`
- `ServerChat <message>`
- `Broadcast <message>` where supported
- `SaveWorld`
- `DoExit`
- `KickPlayer <id>`
- `BanPlayer <id>`

The manager should:

- Keep connection state per server.
- Serialize commands.
- Apply timeouts.
- Redact passwords in logs.
- Support scheduled RCON commands.
- Parse player lists into structured records when possible.

## 2. Software Architecture

### 2.1 Target Stack

- C# 12 where possible, project currently configured for C# 11.
- .NET 8 `net8.0-windows`.
- WPF.
- MVVM with CommunityToolkit.Mvvm.
- SQLite with Microsoft.Data.Sqlite or EF Core SQLite.
- Microsoft.Extensions.Hosting and dependency injection.
- Async/await for all process, IO, network, and database work.
- Background services for monitoring, scheduling, updates, and notifications.

### 2.2 Solution Layout

Recommended final structure:

```text
src/
  ServerManager.App/
    App.xaml
    MainWindow.xaml
    Resources/
    Assets/
  ServerManager.Core/
    Domain/
    ValueObjects/
    Validation/
    Events/
  ServerManager.Data/
    AppDbContext.cs
    Migrations/
    Repositories/
  ServerManager.Services/
    SteamCmd/
    Servers/
    Config/
    Mods/
    Backups/
    Rcon/
    Monitoring/
    Scheduling/
    Firewall/
    Logging/
  ServerManager.UI/
    Views/
    ViewModels/
    Controls/
    Converters/
  ServerManager.Tests/
```

Current repo can evolve incrementally from the existing single WPF project. Do not big-bang split until the data/service boundaries are stable.

### 2.3 Service Architecture

```text
WPF Views
  -> ViewModels
    -> Application Services
      -> Domain Services / Validators
      -> Repositories / DbContext
      -> Infrastructure Adapters
        -> SteamCMD
        -> ASA process
        -> File system
        -> RCON
        -> CurseForge
        -> Windows Firewall
```

Core services:

| Service | Responsibility |
| --- | --- |
| `IServerCatalogService` | CRUD, duplicate, import/export servers |
| `ISteamCmdService` | SteamCMD install/update/validate |
| `IServerProcessService` | Start/stop/restart/kill and console capture |
| `ILaunchProfileBuilder` | Generate launch args safely |
| `IIniConfigService` | Read/write/validate INI files |
| `IBackupService` | Backup/restore/rotation |
| `IModService` | Mod metadata, load order, dependency validation |
| `ICurseForgeClient` | CurseForge API adapter |
| `IClusterService` | Cluster CRUD and validation |
| `IRconService` | RCON connect/command/player operations |
| `IMonitoringService` | Runtime metrics and status |
| `ISchedulerService` | Cron/interval task execution |
| `IFirewallService` | Windows Firewall rule checks/creation |
| `IActivityLogService` | User-visible activity history |

### 2.4 Background Workers

| Worker | Cadence | Purpose |
| --- | --- | --- |
| `ServerMonitorWorker` | 1-5 sec | CPU/RAM/status/log tail |
| `SchedulerWorker` | 15-60 sec | Trigger due tasks |
| `UpdateCheckWorker` | Configurable | Steam/app/mod update checks |
| `BackupRotationWorker` | Hourly/daily | Enforce retention |
| `NotificationWorker` | Event-driven | Discord/toast/log alerts |

### 2.5 Class Diagram

```text
+------------------+        +------------------+
| ServerInstance   | 1    * | ServerMod        |
+------------------+--------+------------------+
| Id               |        | Id               |
| Name             |        | ServerId         |
| GameId           |        | ProjectId        |
| InstallPath      |        | Name             |
| MapName          |        | LoadOrder        |
| Ports            |        | Version          |
| ClusterId        |        | Enabled          |
| ConfigSnapshot   |        +------------------+
+------------------+
        |
        | *    1
+------------------+        +------------------+
| Cluster          | 1    * | BackupEntry      |
+------------------+--------+------------------+
| Id               |        | Id               |
| Name             |        | ServerId         |
| ClusterKey       |        | FilePath         |
| StoragePath      |        | CreatedAt        |
| Enabled          |        | ManifestJson     |
+------------------+        +------------------+

+------------------+        +------------------+
| ScheduleTask     |        | ActivityLogEntry |
+------------------+        +------------------+
| Id               |        | Id               |
| ServerId         |        | Level            |
| TaskType         |        | Source           |
| Cron             |        | Message          |
| PayloadJson      |        | CreatedAt        |
+------------------+        +------------------+
```

## 3. SQLite Database Design

Use SQLite foreign keys and WAL mode. Store complex user-tuned settings as normalized rows where they are queried, and JSON snapshots where preserving the exact generated state matters.

```sql
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

CREATE TABLE Servers (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    GameId TEXT NOT NULL DEFAULT 'asa',
    AppId INTEGER NOT NULL DEFAULT 2430930,
    InstallDirectory TEXT NOT NULL,
    ExecutableName TEXT NOT NULL DEFAULT 'ArkAscendedServer.exe',
    MapName TEXT NOT NULL,
    LaunchArguments TEXT NOT NULL DEFAULT '',
    ClusterId TEXT NULL,
    GamePort INTEGER NOT NULL,
    QueryPort INTEGER NOT NULL,
    RconPort INTEGER NOT NULL,
    RconEnabled INTEGER NOT NULL DEFAULT 1,
    AdminPasswordSecretId TEXT NULL,
    ServerPasswordSecretId TEXT NULL,
    MaxPlayers INTEGER NOT NULL DEFAULT 70,
    AutoUpdateEnabled INTEGER NOT NULL DEFAULT 1,
    AutoRestartOnCrash INTEGER NOT NULL DEFAULT 1,
    Status TEXT NOT NULL DEFAULT 'Offline',
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE Clusters (
    Id TEXT PRIMARY KEY,
    ClusterKey TEXT NOT NULL UNIQUE,
    DisplayName TEXT NOT NULL,
    SharedStoragePath TEXT NOT NULL,
    Enabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE Mods (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NOT NULL,
    ProjectId TEXT NOT NULL,
    FileId TEXT NULL,
    Name TEXT NOT NULL,
    Author TEXT NOT NULL DEFAULT '',
    Version TEXT NOT NULL DEFAULT '',
    LatestVersion TEXT NOT NULL DEFAULT '',
    LoadOrder INTEGER NOT NULL,
    Enabled INTEGER NOT NULL DEFAULT 1,
    DependencyJson TEXT NOT NULL DEFAULT '[]',
    LastCheckedAt TEXT NULL,
    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE CASCADE
);

CREATE TABLE Backups (
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
);

CREATE TABLE Schedules (
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
);

CREATE TABLE Users (
    Id TEXT PRIMARY KEY,
    Username TEXT NOT NULL UNIQUE,
    DisplayName TEXT NOT NULL,
    Role TEXT NOT NULL DEFAULT 'Admin',
    PasswordHash TEXT NULL,
    CreatedAt TEXT NOT NULL,
    LastLoginAt TEXT NULL
);

CREATE TABLE Logs (
    Id TEXT PRIMARY KEY,
    ServerId TEXT NULL,
    Level TEXT NOT NULL,
    Source TEXT NOT NULL,
    Message TEXT NOT NULL,
    DataJson TEXT NOT NULL DEFAULT '{}',
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY(ServerId) REFERENCES Servers(Id) ON DELETE SET NULL
);

CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE INDEX IX_Mods_ServerId_LoadOrder ON Mods(ServerId, LoadOrder);
CREATE INDEX IX_Backups_ServerId_CreatedAt ON Backups(ServerId, CreatedAt DESC);
CREATE INDEX IX_Schedules_NextRunAt ON Schedules(Enabled, NextRunAt);
CREATE INDEX IX_Logs_ServerId_CreatedAt ON Logs(ServerId, CreatedAt DESC);
```

Secrets should not be stored directly in SQLite in plaintext. Use Windows DPAPI or Windows Credential Manager and store only secret IDs in the database.

## 4. UI Design

### 4.1 Visual Direction

Modern dark UI inspired by Discord, Steam, and game launchers:

- Deep neutral background with restrained accent colors.
- Sidebar navigation.
- Dense operational dashboard.
- Status chips: Offline gray, Starting amber, Online green, Error red.
- Cards only for repeated resource/status items and focused panels.
- Rounded corners, but keep operational tables compact.
- Icons on primary actions and navigation.
- Progress bars for installs, updates, backups, and validation.

### 4.2 Main Shell Wireframe

```text
+--------------------------------------------------------------------------------+
| Top Bar: App Name | Active Profile | Global Search | Alerts | Settings          |
+-------------+------------------------------------------------------------------+
| Sidebar     | Page Content                                                     |
| Dashboard   |                                                                  |
| Servers     |                                                                  |
| Clusters    |                                                                  |
| Mods        |                                                                  |
| Backups     |                                                                  |
| Console     |                                                                  |
| Logs        |                                                                  |
| Settings    |                                                                  |
+-------------+------------------------------------------------------------------+
```

### 4.3 Dashboard

```text
+------------------------------------------------------------------------------+
| Fleet Status: 3 Online | 1 Starting | 0 Crashed | Update Available            |
+----------------------+----------------------+-------------------------------+
| Server Card          | Server Card          | Server Card                   |
| The Island           | Scorched Earth       | The Center                    |
| Online / 12 players  | Offline              | Starting                      |
| CPU RAM Uptime       | Last backup          | Progress                      |
+----------------------+----------------------+-------------------------------+
| Activity Feed                         | Scheduled Tasks Due              |
+---------------------------------------+----------------------------------+
```

### 4.4 Servers Page

- List/table of servers with map, status, ports, cluster, players.
- Detail panel for selected server.
- Actions: Create, Import, Duplicate, Start, Stop, Restart, Kill, Update, Validate, Export.
- Tabs: Overview, Configuration, Mods, Backups, Schedules, Logs.

### 4.5 Clusters Page

- Cluster list.
- Shared path editor.
- Member server list.
- Validation panel.
- Transfer policy comparison.
- Cluster backup options.

### 4.6 Mods Page

- Server selector.
- Installed mods ordered list with drag/drop reorder.
- CurseForge search/add-by-ID.
- Update status and dependency warnings.
- Generated `-mods=` preview.

### 4.7 Console Page

- Server selector.
- Live stdout/log stream.
- RCON command input.
- Quick commands: SaveWorld, Broadcast, ListPlayers, DoExit.
- Player management panel.

### 4.8 Backups Page

- Backup timeline.
- Manual backup.
- Restore workflow with pre-restore safety backup.
- Retention policy editor.
- Integrity check.

### 4.9 Settings Page

- SteamCMD path.
- Default install root.
- Default cluster root.
- CurseForge API key.
- Firewall automation.
- Notification channels.
- Theme/accessibility.

## 5. Feature Implementation Plan

### Phase 1: Foundation

- Formalize domain models.
- Move persistence to SQLite-backed repositories.
- Add schema migration layer.
- Add settings/secrets service.
- Add structured activity log.
- Add test project.

### Phase 2: Server Lifecycle

- Implement launch profile builder.
- Add robust process runner with stdout/stderr streams.
- Add start/stop/restart/kill.
- Add RCON-assisted graceful stop: broadcast, save, DoExit, timeout, kill fallback.
- Add port conflict validator.

### Phase 3: SteamCMD

- SteamCMD install/detect/update.
- ASA install/update/validate.
- Progress parser.
- Lock detection when server process is running.

### Phase 4: Configuration Editor

- INI parser/preserver.
- Setting catalog.
- Visual editor with type validation.
- Advanced editor.
- Import/export config bundle.

### Phase 5: Monitoring

- Process metrics.
- Log tail and error detection.
- Query/RCON status probes.
- Player count.
- Crash watchdog and restart policy.

### Phase 6: Backups

- Manual backup.
- Scheduled backup.
- Rotation policy.
- Restore workflow.
- Backup manifest and checksum validation.

### Phase 7: Mods

- CurseForge API client.
- Add/remove/update/reorder mods.
- Dependency resolver.
- Generated launch argument integration.
- Mod update scheduling.

### Phase 8: Clustering

- Cluster CRUD.
- Add/remove server to cluster.
- Shared directory creation/validation.
- Transfer setting consistency checks.
- Cluster-aware backup/restore.

### Phase 9: Scheduler

- Cron task model.
- Restart/update/backup/RCON command tasks.
- Missed-run policy.
- Pre-task and post-task notifications.

### Phase 10: Polish

- Dark UI refinement.
- Accessibility pass.
- Error reports.
- Import from AMP/ArkASM/ASA Dedicated Manager layouts.
- Release packaging and updater.

## 6. Module Code Plan

### 6.1 Domain Models

- `ServerInstance`
- `Cluster`
- `ServerMod`
- `BackupEntry`
- `ScheduleTask`
- `ServerRuntimeStatus`
- `PortBinding`
- `LaunchProfile`
- `IniDocument`
- `ConfigSettingDefinition`

### 6.2 Services

Implementation order:

1. `AppDbContext` or `SqliteConnectionFactory`
2. `SettingsService`
3. `ActivityLogService`
4. `LaunchProfileBuilder`
5. `ServerProcessService`
6. `SteamCmdService`
7. `IniConfigService`
8. `RconService`
9. `BackupService`
10. `MonitoringService`
11. `CurseForgeClient`
12. `ModService`
13. `ClusterService`
14. `SchedulerService`
15. `FirewallService`

### 6.3 Validation

Validators should be standalone and testable:

- `ServerPathValidator`
- `PortValidator`
- `ClusterValidator`
- `IniSettingValidator`
- `ModDependencyValidator`
- `BackupManifestValidator`
- `SteamCmdInstallValidator`

### 6.4 Importers

Import adapters:

- Generic ASA folder importer.
- AMP instance importer.
- ArkASM importer where config layout can be detected.
- ASA Dedicated Manager importer where config layout can be detected.

Importer output should be a `ServerImportPreview` that the user can review before committing.

## 7. Recommended NuGet Packages

Required:

- `CommunityToolkit.Mvvm`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Logging`
- `Microsoft.Data.Sqlite`
- `SQLitePCLRaw.bundle_e_sqlite3`
- `Serilog`
- `Serilog.Extensions.Hosting`
- `Serilog.Sinks.File`
- `Serilog.Sinks.Async`
- `NCrontab.Signed`

Recommended additions:

- `Microsoft.EntityFrameworkCore.Sqlite` if moving to EF Core.
- `Microsoft.EntityFrameworkCore.Design` for migrations.
- `System.IO.Compression` built-in for zip backups.
- `Polly` for HTTP retry policies.
- `FluentValidation` for validation pipeline.
- `RconSharp` or an internal Source RCON implementation after compatibility testing with ASA.
- `H.NotifyIcon.Wpf` for tray icon if the current implementation needs replacement.
- `Hardcodet.NotifyIcon.Wpf` as an alternative tray icon library.
- `Microsoft.Windows.CsWin32` if using Windows APIs for firewall/ETW.

## 8. Feature Checklist

Server management:

- [ ] Create server
- [ ] Edit server
- [ ] Delete server
- [ ] Duplicate server
- [ ] Import existing server
- [ ] Import AMP server
- [ ] Export configuration
- [ ] Validate server paths

Installation:

- [ ] Install SteamCMD
- [ ] Update SteamCMD
- [ ] Install ASA server
- [ ] Update ASA server
- [ ] Validate ASA server files
- [ ] Detect install corruption

Startup:

- [ ] Start server
- [ ] Stop server gracefully
- [ ] Restart server
- [ ] Force kill server
- [ ] Scheduled restart
- [ ] Launch argument preview
- [ ] Managed/custom argument merge

Monitoring:

- [ ] Live console
- [ ] CPU usage
- [ ] RAM usage
- [ ] Network usage
- [ ] Uptime
- [ ] Online status
- [ ] Player count
- [ ] Crash detection
- [ ] Watchdog restart

Backups:

- [ ] Manual backup
- [ ] Scheduled backup
- [ ] Backup rotation
- [ ] Restore backup
- [ ] Pre-restore safety backup
- [ ] Backup manifest
- [ ] Integrity validation

Mods:

- [ ] CurseForge search
- [ ] Add by project ID
- [ ] Install/enable mod
- [ ] Remove/disable mod
- [ ] Update mod
- [ ] Reorder mods
- [ ] Dependency validation
- [ ] Duplicate detection

Clusters:

- [ ] Create cluster
- [ ] Edit cluster
- [ ] Add server to cluster
- [ ] Remove server from cluster
- [ ] Shared storage path validation
- [ ] Transfer setting validation
- [ ] Cluster backup

Configuration:

- [ ] Visual editor
- [ ] Advanced editor
- [ ] INI parser
- [ ] Preserve comments
- [ ] Parameter search
- [ ] Type validation
- [ ] Restart-required flags

RCON:

- [ ] Connect/disconnect
- [ ] Broadcast messages
- [ ] List players
- [ ] Kick players
- [ ] Ban players
- [ ] Save world
- [ ] Custom commands
- [ ] Scheduled commands

Logs:

- [ ] Live logs
- [ ] Error detection
- [ ] Search logs
- [ ] Export logs
- [ ] Crash bundle

Scheduler:

- [ ] Restarts
- [ ] Backups
- [ ] Updates
- [ ] RCON commands
- [ ] Notifications
- [ ] Missed-run policy

## 9. Engineering Rules

- Never edit config while the server is running unless the setting is known hot-reload safe.
- Never restore backups while the server is running.
- Always redact secrets from logs, UI diagnostics, crash bundles, and exported configs unless the user explicitly includes secrets.
- Always create directories before launch and verify write access.
- Always validate ports before start.
- Always persist generated launch profile for troubleshooting.
- Treat ASA logs as the source of truth for ready/error state.
- Treat RCON as best-effort; a process may be alive even if RCON is not ready.
- Every destructive operation needs a confirmation or a reversible safety backup.

## 10. Immediate Next Modules

The next implementation modules should be:

1. SQLite schema migration and repository layer.
2. Launch profile builder with tests.
3. Server import preview for ASA/AMP folders.
4. Port and cluster validation services.
5. Graceful stop pipeline using RCON `SaveWorld` and `DoExit`.

This order gives the manager a reliable operational core before expanding UI polish and advanced automation.
