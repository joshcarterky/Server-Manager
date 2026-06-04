## Dedicated Server Manager v2.0.24

- Reloads MaxPlayers from `GameUserSettings.ini` before starting or restarting from the Servers tab.
- Stops passing managed `-MaxPlayers` launch arguments so the INI value is not overridden back to a stale default.
