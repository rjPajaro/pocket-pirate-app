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

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/MediaConverter/convert-to-mp3` | Download and return audio as MP3 |
| POST | `/api/MediaConverter/convert-to-mp4` | Download and return video as MP4 |

Both endpoints accept a plain JSON string body containing the URL, e.g.:

```json
"https://www.youtube.com/watch?v=example"
```

