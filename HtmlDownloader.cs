namespace TaskNotissimusParse
{
    public class HtmlDownloader : IHtmlDownloader
    {
        private readonly object _lock = new();
        private readonly HttpClient _httpClient;

        public bool WorkIfException { get; }

        public HtmlDownloader(HttpClient httpClient, bool workIfException)
        {
            _httpClient = httpClient;
            WorkIfException = workIfException;
        }

        public async Task<string> DownloadAsync(string url)
        {
            while (true)
            {
                try
                {
                    lock (_lock)
                    {
                        Console.WriteLine($"Начал загрузку {url}");
                        var result = _httpClient.GetStringAsync(url).Result;
                        Console.WriteLine($"Закончил загрузку {url}");
                        return result;
                    }
                }
                catch (Exception)
                {
                    if (WorkIfException)
                    {
                        await Task.Delay(10000);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
