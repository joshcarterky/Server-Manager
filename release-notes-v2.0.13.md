# Dedicated Server Manager v2.0.13 Release Notes

**Release date:** 2026-06-03

## Overview
This release fixes INI imports so imported Game.ini and GameUserSettings.ini files are saved and immediately reflected in the editor.

## What changed
- Imported Game.ini and GameUserSettings.ini files now persist to the selected server config folder.
- Imported values reload into the INI editor and Configure Server settings right away.
- The app/server configuration is saved after import so the imported state remains available after reopening the editor.
- Import confirmations now clarify when the full file was saved but none of its keys matched the built-in editor catalog.

## Package
- Asset: `release/Dedicated-Server-Manager-v2.0.13-win-x64.zip`
- Version: `2.0.13`

## Update Manifest
Use this manifest URL in Settings to receive this and future updates:

`https://github.com/joshcarterky/Server-Manager/releases/download/v2.0.13/update-manifest.json`
