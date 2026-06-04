# Changelog

## 2.0.23 - 2026-06-04

- Refreshed the Templates tab with header stats, accented template cards, rate chips, and feature chips.
- Reworked the Template Preview panel into summary, rate, launch settings, and cluster sections.

## 2.0.22 - 2026-06-04

- Preserved SteamCMD's real exit code when the update terminal reports a Windows interruption code.
- Added specific guidance for SteamCMD app state 0x6 failures on ARK: Survival Ascended updates.

## 2.0.21 - 2026-06-04

- Retried SteamCMD update failures with exit code 8 before showing an error.
- Added clearer SteamCMD failure messages with next steps for exit codes 7 and 8.

## 2.0.20 - 2026-06-04

- Stopped raw ASA stdout and stderr log spam from appearing in the RCON console.
- Kept server process output in the application logs for troubleshooting.

## 2.0.19 - 2026-06-04

- Reloaded `GameUserSettings.ini` immediately before server start so manual MaxPlayers edits are not overwritten by stale app values.
- Loaded `ActiveMods` from `GameUserSettings.ini` into the Configure Server Mods load-order list.
- Used INI MaxPlayers and ActiveMods values even when they appear outside the built-in catalog section.

## 2.0.18 - 2026-06-04

- Fixed the Configure Server Mods tab so the bottom Server Load Order section can be reached on shorter windows.
- Added internal scrolling to the inline Configure Server panel.

## 2.0.17 - 2026-06-04

- Fixed selected servers so INI-backed settings reload from `Game.ini` and `GameUserSettings.ini` when the server is selected.
- Prevented the Configure Server INI panel and editor from opening with stale saved app settings when newer INI file values exist.

## 2.0.16 - 2026-06-03

- Improved the Logs tab with parsed log columns, summary counts, selected-entry details, and copy/export controls.
- Added the missing BattleEye launch setting to the INI editor and wired it to `-NoBattlEye`.
- Fixed the RCON console layout so command controls stay visible when output fills the console.
- Fixed managed INI values such as Max Players drifting back to stale template values when starting a server.

## 2.0.15 - 2026-06-03

- Added the new Server Manager icon to the EXE, main window, taskbar, and tray icon.
- Changed Add Existing Server so imported installs are copied into the app's default servers folder.
- The manager now points imported servers at the copied default-location folder instead of the original source folder.

## 2.0.14 - 2026-06-03

- Fixed ARK session names showing `%20` instead of spaces in the in-game server browser.
- Quoted the generated map/options launch argument when the session name contains spaces.

## 2.0.13 - 2026-06-03

- Fixed Game.ini and GameUserSettings.ini imports so they persist to the selected server config folder.
- Reloaded imported INI values into the editor and Configure Server settings immediately after import.
- Saved the app/server configuration after INI import so imported values remain available when reopening the editor.
- Improved the import confirmation message when an imported file has no keys that match the built-in editor catalog.

## 2.0.12 - 2026-06-03

- Added visible Reconnect and Clear buttons to the Configure Server RCON console header.
- Renamed the RCON refresh action to Reconnect for clearer wording.
- Organized the Templates tab by game with section headers and template counts.
- Cleaned up template cards so game context is shown once per section instead of repeated on every card.

## 2.0.11 - 2026-06-03

- Added an in-app Download & Install update flow in Settings.
- The updater downloads the release zip, stages it, closes the app, replaces files, and relaunches the app.

## 2.0.10 - 2026-06-03

- Added a Refresh button to the RCON console to reconnect the selected server.
- Kept Clear available in the RCON console controls for quickly clearing output.

## 2.0.9 - 2026-06-03

- Added an automatic one-time SteamCMD retry for first-run exit code 7 setup failures.

## 2.0.8 - 2026-06-03

- Fixed update checks failing with "The calling thread cannot access this object because a different thread owns it."

## 2.0.7 - 2026-06-03

- Fixed the INI Settings Editor crash caused by missing WPF resources.
- Restyled the INI Settings Editor so it matches the app's dark theme.
- Added an explicit Save button to the INI Settings Editor.
- Fixed INI editor saves so edited values refresh Configure Server settings and persist to INI files.
- Fixed Configure Server name edits being overwritten by stale INI values.

## 2.0.6 - 2026-06-02

- Fixed Add Existing Server popup button row clipping.

## 2.0.5 - 2026-06-02

- Fixed disabled Settings action buttons so they keep the dark app theme.
- Fixed Add Existing Server and New Server game selectors so the closed combo box remains dark.

## 2.0.4 - 2026-06-02

- Added extra spacing below the Update Manifest URL row.
- Fixed disabled Settings buttons turning bright white.

## 2.0.3 - 2026-06-02

- Fixed Settings update buttons overlapping the Update Manifest URL field.

## 2.0.2 - 2026-06-02

- Replaced the Configure Server Console placeholder with the full RCON console surface.
- Added automatic RCON console connection after starting or restarting a server from dashboard and servers workflows.
- Extended auto-connect retries so the console waits longer while ASA finishes booting.

## 2.0.1 - 2026-06-02

- Added an update manifest URL setting so deployed copies can check for newer downloadable releases.
- Added Settings controls for current version, latest checked version, Check Updates, and Open Download.
- Added release distribution docs and an update manifest template for publishing future versions.

## 2.0.0 - 2026-06-02

- Added live application color theming with expanded dark color choices.
- Fixed settings color changes so they apply to the main application shell.
- Darkened WPF scrollbars, tab surfaces, and Windows title-bar chrome.
- Added a Custom Map option in Configure Server so custom map package names can be entered and saved.
- Fixed the settings color selector startup crash.

## 0.1.17 - 2026-05-29

- Restyled the Configure Server Mods tab with modern cards, clearer search/results sections, and a cleaner server load-order table.

## 0.1.16 - 2026-05-29

- Enriched installed mod metadata from known CurseForge Project IDs before opening the installed mods window.
- Future browser-added mods now fill missing thumbnails, author, link, and download metadata when available in the local catalog.

## 0.1.15 - 2026-05-29

- Added a View Installed Mods window with mod cards, thumbnails when available, metadata, and CurseForge links.

## 0.1.14 - 2026-05-29

- Labeled the Mods tab lists as Search Results and Server Load Order so their separate purposes are clear.

## 0.1.13 - 2026-05-29

- Fixed the in-app CurseForge browser release build by copying WebView2Loader.dll beside the app.

## 0.1.12 - 2026-05-29

- Improved the in-app CurseForge browser startup by using a clean app-owned WebView2 data folder.
- Removed the forced browser user agent and changed the default page to the normal ASA mods page.
- Added retry handling when CurseForge navigation fails inside WebView2.

## 0.1.11 - 2026-05-29

- Restored the in-app CurseForge browser so the current mod page can be added directly.
- Added a loading timeout/content detection so the browser overlay does not stay stuck forever.
- Strengthened the Mods tab dark list styling so headers, rows, and selected items remain readable.

## 0.1.10 - 2026-05-29

- Changed CurseForge Open Website actions to launch the normal browser instead of the embedded WebView.
- Darkened the Mods tab list headers and rows so search results and server mod lists are readable.

## 0.1.9 - 2026-05-29

- Restored the Mods tab under Configure Server.
- Added mod search, Add ID, Open Website, load-order, remove, and move controls to the inline server configuration panel.

## 0.1.8 - 2026-05-29

- Fixed Servers tab Configure buttons so they open the server settings panel directly.
- Removed Console buttons from server cards.

## 0.1.7 - 2026-05-29

- Fixed the New Server game selector so it shows the game name instead of the internal model type.

## 0.1.6 - 2026-05-29

- Added a New Server dialog that lets you choose the game profile and enter the server name.
- New servers now use a default install folder based on the chosen server name.

## 0.1.5 - 2026-05-29

- Removed the top-right New Server and Add Existing Server buttons from the Servers tab.
- Moved Add Existing Server into the empty server panel under New Server.

## 0.1.4 - 2026-05-29

- Removed the top-level Remove Server button from the Servers tab.
- Kept deletion on each server card so server removal is explicit per server.

## 0.1.3 - 2026-05-29

- Added a Delete button to each server card so the exact server can be selected for removal.
- Added a confirmation dialog that names the server and clarifies that install folders and saves are not deleted.
- Kept the dashboard server list in sync after deleting from the Servers page.

## 0.1.2 - 2026-05-27

- Replaced the placeholder Templates tab with working server preset cards.
- Added built-in templates for Official PvE, Official PvP, Solo Boosted, FiberCraft, Hardcore, Casual PvE, and Breeding Server.
- Added template actions to create a server, save the selected dashboard server as a custom template, and refresh templates.

## 0.1.1 - 2026-05-27

- Added smart import for existing ASA server installs and save folders.
- Import now reads saved map files and `GameUserSettings.ini` values such as session name, ports, passwords, max players, rates, and cluster settings.
- Existing saves stay in their original folder instead of being moved or deleted.

## 0.1.0 - 2026-05-27

- Added visible app versioning.
- Normalized ASA map selections to current `_WP` server map names.
- Fixed ASA launch map names so local join works.
- Added dashboard readiness, CPU, memory, IP, and server card updates.
