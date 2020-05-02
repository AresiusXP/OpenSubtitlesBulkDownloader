# Opensubtitles.org Bulk Downloader
This is a test app to interact with REST endpoint with Open Subtitles. It uses files hashes to look for the most accurate subtitles, and downloads the first result. It only searches for English subtitles. It looks for subtitles in a path recursively.

## Requirements
- Windows 10
- .NET 4.7.2

## Command line app
1. Open `cmd.exe`
2. Run `OpenSubtitlesBulkDownload.exe [Path]`

    _Path is optional. If not set, it will use current location._
    ![Example with no path](https://i.imgur.com/o3gFitN.png)

## Graphic interface
1. Run `OSBDgui.exe`
2. Write path or select it with the folder icon.
3. Click `Search subtitles`

    ![Search completed](https://i.imgur.com/l5ILv9u.png)
