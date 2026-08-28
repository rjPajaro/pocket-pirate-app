using Microsoft.AspNetCore.Mvc;
using PocketPirate.Application.Interfaces;

namespace PocketPirate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaConverterController : ControllerBase
    {
        private readonly IMediaConverterService _service;

        public MediaConverterController(IMediaConverterService service)
        {
            _service = service;
        }

        // Legacy synchronous endpoints (kept for compatibility)
        [HttpPost("convert-to-mp3")]
        public async Task<IActionResult> ConvertToMp3([FromBody] string url)
        {
            var filePath = await _service.ConvertToMp3(url);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(stream, "audio/mpeg", Path.GetFileName(filePath));
        }

        [HttpPost("convert-to-mp4")]
        public async Task<IActionResult> ConvertToMp4([FromBody] string url)
        {
            var filePath = await _service.ConvertToMp4(url);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(stream, "video/mp4", Path.GetFileName(filePath));
        }

        // Progress-based endpoints
        [HttpPost("start-mp3")]
        public IActionResult StartMp3([FromBody] string url)
        {
            var jobId = _service.StartJob(url, "mp3");
            return Ok(new { jobId });
        }

        [HttpPost("start-mp4")]
        public IActionResult StartMp4([FromBody] string url)
        {
            var jobId = _service.StartJob(url, "mp4");
            return Ok(new { jobId });
        }

        [HttpGet("progress/{jobId}")]
        public async Task StreamProgress(string jobId, CancellationToken ct)
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var job = _service.GetJob(jobId);
                    if (job == null)
                    {
                        await Response.WriteAsync("data: {\"status\":\"error\",\"error\":\"Job not found\"}\n\n", ct);
                        break;
                    }

                    var errPart = job.Error != null ? $",\"error\":\"{job.Error.Replace("\"", "\\\"")}\"" : "";
                    var filePart = job.FileName != null ? $",\"fileName\":\"{job.FileName.Replace("\"", "\\\"")}\"" : "";
                    var payload = $"{{\"progress\":{job.Progress},\"status\":\"{job.Status}\"{errPart}{filePart}}}";
                    await Response.WriteAsync($"data: {payload}\n\n", ct);
                    await Response.Body.FlushAsync(ct);

                    if (job.Status is "done" or "error")
                    {
                        // Give the browser time to receive the final event before closing
                        await Task.Delay(500, CancellationToken.None);
                        break;
                    }

                    await Task.Delay(400, ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        [HttpGet("download/{jobId}")]
        public IActionResult DownloadFile(string jobId)
        {
            var job = _service.GetJob(jobId);
            if (job?.Status != "done" || job.FilePath == null)
                return NotFound();

            var ext = Path.GetExtension(job.FilePath).TrimStart('.');
            var contentType = ext == "mp3" ? "audio/mpeg" : "video/mp4";
            var stream = new FileStream(job.FilePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            _service.RemoveJob(jobId);
            return File(stream, contentType, Path.GetFileName(job.FilePath));
        }
    }
}
