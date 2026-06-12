## Dedicated Server Manager v2.0.28

- Added shared ASA server install detection for `ArkAscendedServer.exe` and `ShooterGameServer.exe`.
- Detects `ShooterGame/Saved`, `Config/WindowsServer`, INI files, `SavedArks`, logs, and mods folders.
- Stores discovered save, config, log, and mod paths on server profiles.
- Added a reusable ASA launch profile builder for safer startup command generation.
- Improved AMP/existing-server imports for launch, cluster, and player settings.
- Added a full ASA server manager technical design document.
- Expanded SQLite schema initialization for future server, cluster, mod, backup, schedule, user, log, and settings features.

Asset: `release/Dedicated-Server-Manager-v2.0.28-win-x64.zip`
