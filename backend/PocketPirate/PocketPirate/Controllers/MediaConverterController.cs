using Microsoft.AspNetCore.Mvc;
using PocketPirate.Application.Interfaces;

namespace PocketPirate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MediaConverterController : ControllerBase
    {
        private readonly IMediaConverterService _mediaConverterService;

        public MediaConverterController(IMediaConverterService mediaConverterService)
        {
            _mediaConverterService = mediaConverterService;
        }

        [HttpPost("convert-to-mp3")]
        public async Task<IActionResult> ConvertToMp3([FromBody] string url)
        {
            var filePath = await _mediaConverterService.ConvertToMp3(url);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(stream, "audio/mpeg", Path.GetFileName(filePath));
        }

        [HttpPost("convert-to-mp4")]
        public async Task<IActionResult> ConvertToMp4([FromBody] string url)
        {
            var filePath = await _mediaConverterService.ConvertToMp4(url);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(stream, "video/mp4", Path.GetFileName(filePath));
        }
    }
}
