using ManuHub.Ytdlp.NET;
using PocketPirate.Application.Interfaces;

namespace PocketPirate.Application.Services
{
    public class MediaConverterService : IMediaConverterService
    {
        public async Task<string> ConvertToMp3(string url)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "pocket-pirate-downloads");
            Directory.CreateDirectory(tempFolder);

            var toolsPath = Path.Combine(AppContext.BaseDirectory, "tools");
            var ytdlp = new Ytdlp(Path.Combine(toolsPath, "yt-dlp.exe"))
                .WithOutputFolder(tempFolder)
                .WithFFmpegLocation(toolsPath)
                .WithFormat("bestaudio/best")
                .WithExtractAudio(AudioFormat.Mp3);

            await ytdlp.DownloadAsync(url);

            var files = Directory.GetFiles(tempFolder, "*.mp3");
            if (files.Length == 0)
                throw new Exception("Download failed - no file created");

            return files[0];
        }

        public async Task<string> ConvertToMp4(string url)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "pocket-pirate-downloads");
            Directory.CreateDirectory(tempFolder);

            var toolsPath = Path.Combine(AppContext.BaseDirectory, "tools");
            var ytdlp = new Ytdlp(Path.Combine(toolsPath, "yt-dlp.exe"))
                .WithOutputFolder(tempFolder)
                .WithFFmpegLocation(toolsPath)
                .WithFormat("bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best")
                .WithMergeOutputFormat("mp4");

            await ytdlp.DownloadAsync(url);

            var files = Directory.GetFiles(tempFolder, "*.mp4");
            if (files.Length == 0)
                throw new Exception("Download failed - no file created");

            return files[0];
        }
    }
}
