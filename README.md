# ArchivePlayer

A self-hosted media library manager for Archive.org content. Browse, organize, and watch public domain and user-uploaded media from the Internet Archive with a modern, Netflix-style interface.

All data is stored locally on your machine. No accounts, no telemetry, no cloud dependency.

## Features

- **Live search** of Archive.org with video filtering and pagination
- **Direct URL/identifier** addition for items you already know
- **Custom categories** with autocomplete from your existing library
- **Continue Watching** section with visual progress bars
- **Episode selector** for TV shows with season grouping
- **Playback controls**: speed adjustment, picture-in-picture, keyboard shortcuts, and mobile gestures
- **Optional TMDB integration** for posters, episode titles, descriptions, and smart category suggestions
- **Filename-based episode fallback** when TMDB is unavailable
- **Privacy controls**: clear watch history or remove items individually
- **Local SQLite database** — your library lives entirely on your machine
- **User authentication** for multi-user setups on a single machine

## Attribution

If you fork, modify, or redistribute this project, please provide attribution by:
- Crediting the original author
- Linking to the original repository: https://github.com/JeppeKjeldal/ArchivePlayer
- Noting that your work is derived from this project

This helps support the original development and allows others to find the source project.

## Requirements

- Windows 10 or 11 (64-bit)
- No .NET installation required — the runtime is bundled
- An internet connection for streaming from Archive.org

## Installation

1. Download the latest release from the [Releases page](../../releases)
2. Extract the archive to a folder you have write access to (for example, `C:\Apps\ArchivePlayer`)
3. Run `ArchivePlayer.exe`
4. Your browser will open to `http://localhost:5000` automatically
5. Register a local account and start adding content

On first run, the application creates:

- `archiveplayer.db` — your local database
- `settings.json` — your user preferences (including optional TMDB key)

Keep the application in a writable location so these files can be saved. To stop the server, close the console window.

## Usage

### Adding Content

There are two ways to add content:

**Live Search** — Type a query (for example, "night of the living dead") and browse results filtered to video content. Click "Add to Library" on any result.

**Direct URL** — If you already know the Archive.org identifier or URL, paste it into the Direct URL tab and add it immediately.

### Organizing Your Library

When adding content, you can assign it to a category. Categories are free-form — create whatever structure makes sense for your collection. Existing categories appear as autocomplete suggestions.

### Watching

Click any item in your library to open the player. For TV shows, use the episode selector to choose which episode to watch. Progress is saved automatically, and the item appears in "Continue Watching" on the home page.

## TMDB Integration (Optional)

ArchivePlayer works without any API key. When TMDB is not configured, the app uses Archive.org thumbnails and parses episode information from filenames.

To enable posters, episode metadata, and category suggestions:

1. Create a free account at [themoviedb.org](https://www.themoviedb.org/signup)
2. Request an API key under Settings → API (the free Developer tier is sufficient)
3. Paste your v3 API key into the Settings page in ArchivePlayer

Your key is stored locally in `settings.json` and is only used to make requests to TMDB from your machine.

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A TMDB API key (optional, for development)

### Running in Development

```bash
git clone https://github.com/YOUR_USERNAME/ArchivePlayer.git
cd ArchivePlayer
cd ArchivePlayer
dotnet run
```

Your TMDB API key should be stored in User Secrets during development:

```bash
dotnet user-secrets set "TMDB:ApiKey" "your-key-here"
```

### Publishing a Release

To build a self-contained Windows executable:

```bash
dotnet publish .\ArchivePlayer\ArchivePlayer.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:PublishTrimmed=false `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish-release
```

The output in `publish-release` can be zipped and distributed. Users do not need to install .NET.

## Tech Stack

- **Backend**: ASP.NET Core 10, Entity Framework Core
- **Database**: SQLite (via EF Core)
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Razor views, Tailwind CSS, vanilla JavaScript
- **External APIs**: Archive.org Metadata API, TMDB API (optional)

## Known Limitations

- **Windows SmartScreen**: Unsigned builds may trigger a SmartScreen warning on first launch. Select "More info" → "Run anyway".
- **Playback buffering**: Streams are served directly from Archive.org. Startup delay and occasional buffering depend on their infrastructure.
- **Single-machine focus**: The app is designed for personal use on one machine. Running it as a shared server for multiple users over a network is possible but not the primary use case.

## Attribution

This product uses the TMDB API but is not endorsed or certified by TMDB.

All media content is streamed from the [Internet Archive](https://archive.org). Users are responsible for ensuring their use of any material complies with applicable law.

## License

This project is provided as-is for personal and educational use.
