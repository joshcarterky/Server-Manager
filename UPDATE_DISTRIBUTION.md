# Sending App Updates

Use the update manifest to tell installed copies where the newest download is.

1. Publish the app and create a zip.
2. Upload the zip somewhere public, such as GitHub Releases, a web server, Dropbox, Google Drive direct link, or another file host.
3. Edit `release\update-manifest.json` and replace `downloadUrl` with the public link to the zip.
4. Upload `update-manifest.json` somewhere public too.
5. Give users the manifest URL once. In the app they open Settings, paste it into `Update Manifest URL`, click `Save Settings`, then click `Check Updates`.

For the next release, update the version, upload the new zip, and update only the manifest file. Everyone using that manifest URL will see the new version.

Manifest format:

```json
{
  "version": "2.0.6",
  "downloadUrl": "https://example.com/Dedicated-Server-Manager-v2.0.6-win-x64.zip",
  "releaseNotesUrl": "https://example.com/release-notes",
  "publishedAt": "2026-06-02"
}
```
