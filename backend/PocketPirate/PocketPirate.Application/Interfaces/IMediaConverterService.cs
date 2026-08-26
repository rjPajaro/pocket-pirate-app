namespace PocketPirate.Application.Interfaces
{
    public interface IMediaConverterService
    {
        Task<string> ConvertToMp3(string url);
        Task<string> ConvertToMp4(string url);
    }
}
