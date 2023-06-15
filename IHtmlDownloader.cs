namespace TaskNotissimusParse
{
    public interface IHtmlDownloader
    {
        public HttpClient HttpClient { get; set; }
        Task<string> DownloadAsync(string url);
    }
}
