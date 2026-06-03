# Dedicated Server Manager v2.0.14 Release Notes

**Release date:** 2026-06-03

## Overview
This release fixes ARK session names with spaces so the in-game browser shows spaces instead of `%20`.

## What changed
- Session names are no longer URL-escaped when generating the ARK map/options launch argument.
- The map/options launch argument is quoted when needed so names with spaces remain one argument.

## Package
- Asset: `release/Dedicated-Server-Manager-v2.0.14-win-x64.zip`
- Version: `2.0.14`

## Update Manifest
Use this manifest URL in Settings to receive this and future updates:

`https://github.com/joshcarterky/Server-Manager/releases/download/v2.0.14/update-manifest.json`
