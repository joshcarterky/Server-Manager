Server Manager
Version: 2.0.12

How to run:
1. Extract this zip to a folder such as C:\Server Manager.
2. Run ServerManager.exe.
3. Create or configure a server.
4. Use Install / Update to download ASA dedicated server files.
5. Select an ASA map ending in _WP, such as TheIsland_WP, or choose Custom Map and type the map package name.
6. Start the server and wait for the dashboard badge to show Ready.
7. The Configure Server Console tab will auto-connect to RCON after the server starts.
8. To receive future app updates, open Settings, paste the update-manifest.json URL, save settings, and use Check Updates.

Local join example:
open 127.0.0.1:7777

LAN join example:
open <server local IP>:7777

Notes:
- Do not run the app directly from inside the zip.
- Keep the folder together. The app creates Data, logs, servers, and config files beside the exe.
- For internet players, forward UDP 7777, UDP 7778, and UDP 27015 to the host PC.
- For update distribution, upload the release zip and update-manifest.json to the same public place.
