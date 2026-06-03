# Dedicated Server Manager v2.0.9 Release Notes

**Release date:** 2026-06-03

## Overview
This release smooths out SteamCMD first-run installs.

## What changed
- Added an automatic one-time retry when SteamCMD exits with code 7 during install/update.
- This handles the common first-run case where SteamCMD finishes setup, reports a missing app configuration once, then succeeds on the next run.

## Package
- Asset: `release/Dedicated-Server-Manager-v2.0.9-win-x64.zip`
- Version: `2.0.9`

## Update Manifest
Use this manifest URL in Settings to receive this and future updates:

`https://github.com/joshcarterky/Server-Manager/releases/download/v2.0.9/update-manifest.json`
