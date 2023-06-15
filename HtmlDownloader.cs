using System.Net;

namespace TaskNotissimusParse
{
    public class HtmlDownloader : IHtmlDownloader
    {
        public HttpClient HttpClient { get; set; }

        public int RequestDelayMilliseconds { get; }
        public bool WorkIfException { get; }
        public event Action<string>? ExceptionNotificationHandler;
        public Func<HttpClient>? GetNewHttpClient;

        public HtmlDownloader(HttpClient httpClient, bool workIfException, int requestDelayMilliseconds, Func<HttpClient>? getNewHttpClient, Action<string>? exceptionNotificationHandler = null)
        {
            HttpClient = httpClient;
            RequestDelayMilliseconds = requestDelayMilliseconds;
            GetNewHttpClient = getNewHttpClient;
            WorkIfException = workIfException;
            ExceptionNotificationHandler = exceptionNotificationHandler;
        }

        public async Task<string> DownloadAsync(string url)
        {
            while (true)
            {
                try
                {
                    await Task.Delay(RequestDelayMilliseconds);
                    var response = await HttpClient.GetStringAsync(url);
                    if (!string.IsNullOrEmpty(response.Trim()))
                    {
                        return response;
                    }
                    else
                    {
                        throw new Exception("При осуществлении запроса получена пустая строка");
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (WorkIfException)
                    {
                        ExceptionNotificationHandler?.Invoke($"Произошла ошибка загрузки {DateTime.Now} - {ex.Message}");
                        if (ex.StatusCode == HttpStatusCode.InternalServerError && GetNewHttpClient != null)
                        {
                            HttpClient = GetNewHttpClient();
                        }
                        await Task.Delay(30000);
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    if (WorkIfException)
                    {
                        ExceptionNotificationHandler?.Invoke($"Произошла ошибка загрузки {DateTime.Now} - {ex.Message}");
                        await Task.Delay(30000);
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
