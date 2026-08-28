# Pocket Pirate App

A .NET 10 Web API that converts YouTube (and other) URLs to MP3 or MP4 using `yt-dlp` and `ffmpeg`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `yt-dlp.exe` and `ffmpeg.exe` placed in the `tools/` directory relative to the API output (e.g. `backend/PocketPirate/PocketPirate/bin/Debug/net10.0/tools/`)

### Install required tools (Windows)

Run the following commands to download the required binaries into the correct `tools/` directory:

```powershell
# Create the tools directory if it doesn't exist
New-Item -ItemType Directory -Force -Path "backend\PocketPirate\PocketPirate\bin\Debug\net10.0\tools"

# Download yt-dlp
Invoke-WebRequest -Uri "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" `
    -OutFile "backend\PocketPirate\PocketPirate\bin\Debug\net10.0\tools\yt-dlp.exe"

# Download ffmpeg
Invoke-WebRequest -Uri "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip" `
    -OutFile "$env:TEMP\ffmpeg.zip"
Expand-Archive -Path "$env:TEMP\ffmpeg.zip" -DestinationPath "$env:TEMP\ffmpeg-extracted" -Force
Copy-Item (Get-ChildItem "$env:TEMP\ffmpeg-extracted" -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1).FullName `
    -Destination "backend\PocketPirate\PocketPirate\bin\Debug\net10.0\tools\ffmpeg.exe"
```

### Verify tools are in place

```powershell
Get-ChildItem "backend\PocketPirate\PocketPirate\bin\Debug\net10.0\tools"
```

You should see both `ffmpeg.exe` and `yt-dlp.exe` listed.

## Running the API

```powershell
cd backend\PocketPirate\PocketPirate
dotnet run
```

## Frontend

The frontend is an Angular 13 single-page application located in the `frontend/` directory.

### Prerequisites

- [Node.js](https://nodejs.org/) and npm
- Angular CLI: `npm install -g @angular/cli`

### Setup

```powershell
cd frontend
npm install
```

### Running the dev server

```powershell
ng serve
```

Navigate to `http://localhost:4200/`. The app will automatically reload on source file changes.

The UI lets you paste a video URL and download it as MP3 or MP4. It communicates with the backend API running on its configured base URL.

### Build

```powershell
ng build
```

Build artifacts are output to the `dist/` directory.

### Running unit tests

```powershell
ng test
```

## Desktop Installer (Electron)

The `electron-shell/` directory wraps the Angular UI and .NET API into a single Windows installer using Electron. End users install it like any normal app — no Node, .NET, ffmpeg, or yt-dlp required on their machine.

### Prerequisites (build machine only)

- [Node.js](https://nodejs.org/) and npm
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `yt-dlp.exe` and `ffmpeg.exe` in `backend\PocketPirate\PocketPirate\bin\Debug\net10.0\tools\` (see above)

### Build the installer

```powershell
.\build-electron.ps1
```

This script:
1. Builds the Angular frontend (`ng build --configuration production`)
2. Publishes the .NET API as a self-contained `win-x64` executable
3. Copies everything into `electron-shell/resources/`
4. Runs `electron-builder` to produce a Windows NSIS installer

The installer is output to `dist-electron/`.

### What happens at runtime

- The installer places the app in `Program Files`
- On launch, Electron starts the .NET API silently in the background on a free local port
- The UI opens in a native window — no browser or terminal needed
- Closing the app shuts down the API automatically

### Project structure

```
electron-shell/
├── main.js              ← Electron entry point
├── package.json         ← electron-builder config
└── resources/           ← populated by build-electron.ps1
    ├── api/             ← self-contained .NET publish output + tools/
    └── app/             ← Angular production build
```

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/MediaConverter/convert-to-mp3` | Download and return audio as MP3 |
| POST | `/api/MediaConverter/convert-to-mp4` | Download and return video as MP4 |

Both endpoints accept a plain JSON string body containing the URL, e.g.:

```json
"https://www.youtube.com/watch?v=example"
```

