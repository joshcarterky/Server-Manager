# Dedicated Server Manager v2.0.6 Release Notes

**Release date:** 2026-06-02

## Overview
This release fixes a UI issue in the Add Existing Server popup and ships the latest Windows x64 installer zip.

## What changed
- Fixed `Add Existing Server` popup button row clipping.

## Package
- Asset: `release/Dedicated-Server-Manager-v2.0.6-win-x64.zip`
- Version: `2.0.6`

## Notes
- The package includes the latest Windows x64 build and release metadata for update distribution.
- For update manifest publishing, keep the ZIP and `update-manifest.json` together in the same public location.

## Recommended release command
```powershell
gh release create v2.0.6 "C:\Users\josh\Desktop\server manager\release\Dedicated-Server-Manager-v2.0.6-win-x64.zip" --repo <owner/repo> --title "v2.0.6" --notes-file "release-notes-v2.0.6.md"
```
