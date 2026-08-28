using ManuHub.Ytdlp.NET;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using PocketPirate.Application.Interfaces;
using PocketPirate.Application.Models;

namespace PocketPirate.Application.Services
{
    public class MediaConverterService : IMediaConverterService
    {
        private static readonly ConcurrentDictionary<string, DownloadJob> _jobs = new();

        public string StartJob(string url, string format)
        {
            var jobId = Guid.NewGuid().ToString("N");
            var job = new DownloadJob();
            _jobs[jobId] = job;
            _ = Task.Run(() => RunJobAsync(jobId, url, format, job));
            return jobId;
        }

        public DownloadJob? GetJob(string jobId)
            => _jobs.TryGetValue(jobId, out var job) ? job : null;

        public void RemoveJob(string jobId) => _jobs.TryRemove(jobId, out _);

        public Task<string> ConvertToMp3(string url) => RunConversionAsync(url, "mp3", _ => { });
        public Task<string> ConvertToMp4(string url) => RunConversionAsync(url, "mp4", _ => { });

        private async Task RunJobAsync(string jobId, string url, string format, DownloadJob job)
        {
            try
            {
                job.Status = "downloading";
                var filePath = await RunConversionAsync(url, format, pct =>
                {
                    job.Progress = pct;
                    job.Status = pct >= 95 ? "converting" : "downloading";
                });
                job.FilePath = filePath;
                job.FileName = Path.GetFileName(filePath);
                job.Progress = 100;
                job.Status = "done";
            }
            catch (Exception ex)
            {
                job.Error = ex.Message;
                job.Status = "error";
            }
        }

        private async Task<string> RunConversionAsync(string url, string format, Action<int> onProgress)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "pocket-pirate-downloads");
            Directory.CreateDirectory(tempFolder);
            var toolsPath = Path.Combine(AppContext.BaseDirectory, "tools");
            var ytdlpPath = Path.Combine(toolsPath, "yt-dlp.exe");

            var args = format == "mp3"
                ? $"--ffmpeg-location \"{toolsPath}\" -x --audio-format mp3 -o \"{tempFolder}/%(title)s.%(ext)s\" \"{url}\""
                : $"--ffmpeg-location \"{toolsPath}\" -f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best\" --merge-output-format mp4 -o \"{tempFolder}/%(title)s.%(ext)s\" \"{url}\"";

            var psi = new ProcessStartInfo
            {
                FileName = ytdlpPath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var progressRegex = new Regex(@"\[download\]\s+(\d+(?:\.\d+)?)%");

            using var process = new Process { StartInfo = psi };
            process.Start();

            await Task.WhenAll(
                Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                    {
                        var match = progressRegex.Match(line);
                        if (match.Success && double.TryParse(match.Groups[1].Value,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var pct))
                        {
                            onProgress((int)Math.Clamp(pct, 0, 99));
                        }
                    }
                }),
                process.WaitForExitAsync()
            );

            if (process.ExitCode != 0)
                throw new Exception($"yt-dlp exited with code {process.ExitCode}");

            var ext = format == "mp3" ? "mp3" : "mp4";
            var files = Directory.GetFiles(tempFolder, $"*.{ext}");
            if (files.Length == 0)
                throw new Exception($"Download failed — no {ext} file produced");

            return files[0];
        }
    }
}
