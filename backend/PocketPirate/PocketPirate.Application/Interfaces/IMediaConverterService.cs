using PocketPirate.Application.Models;

namespace PocketPirate.Application.Interfaces
{
    public interface IMediaConverterService
    {
        Task<string> ConvertToMp3(string url);
        Task<string> ConvertToMp4(string url);
        string StartJob(string url, string format);
        DownloadJob? GetJob(string jobId);
        void RemoveJob(string jobId);
    }
}
