namespace PocketPirate.Application.Models
{
    public class DownloadJob
    {
        public int Progress { get; set; }
        public string Status { get; set; } = "starting"; // starting | downloading | converting | done | error
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public string? Error { get; set; }
    }
}
