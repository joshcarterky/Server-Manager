# Dedicated Server Manager v2.0.19 Release Notes

This release fixes GameUserSettings.ini values being ignored for Max Players and mods.

## Changes

- Server start now reloads `GameUserSettings.ini` first, so manual `MaxPlayers` edits are preserved.
- Configure Server Mods now builds the Server Load Order from the `ActiveMods` value in `GameUserSettings.ini`.
- MaxPlayers and ActiveMods are found even when they are saved under a different INI section than the editor catalog expects.

## Update asset

- Asset: `release/Dedicated-Server-Manager-v2.0.19-win-x64.zip`
- Version: `2.0.19`

## Update manifest URL

`https://github.com/joshcarterky/Server-Manager/releases/download/v2.0.19/update-manifest.json`
