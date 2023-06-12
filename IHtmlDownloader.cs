namespace TaskNotissimusParse
{
    public interface IHtmlDownloader
    {
        Task<string> DownloadAsync(string urls);
    }
}
