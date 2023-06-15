using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;

namespace TaskNotissimusParse
{
    public class ToyRuParserBuilder : IDisposable
    {
        private readonly bool _useDefaultSettings;
        private readonly UserChatter _userChatter;

        private WebProxy? _proxy;
        private CookieContainer _cookieContainer;
        private int _requestDelay;
        private int _requestTimeOut;
        private bool _workIfException;
        private Action<string>? _exceptionNotificationHandler;
        private Func<HttpClient>? _createNewHttpClientHandler;
        private HttpClientHandler _httpClientHandler;
        private IHtmlDownloader _htmlDownloader;
        private IBrowsingContext _context;

        public HttpClient HttpClient { get; private set; }

        public ToyRuParserBuilder(bool useDefaultSettings, UserChatter userChatter)
        {
            _useDefaultSettings = useDefaultSettings;
            _userChatter = userChatter;
        }

        public ToyRuParserService Build()
        {
            return new ToyRuParserService(
                _htmlDownloader,
                new ProductParser(_context),
                new CatalogParser(_context),
                _context
            );
        }

        public ToyRuParserBuilder SetBrowsingContext()
        {
            var config = Configuration.Default.WithDefaultLoader().WithCulture("ru-ru");
            _context = BrowsingContext.New(config);

            return this;
        }

        public ToyRuParserBuilder SetHtmlDownloader()
        {
            _htmlDownloader = new HtmlDownloader(HttpClient, _workIfException, _requestDelay, _createNewHttpClientHandler, _exceptionNotificationHandler);

            return this;
        }

        public ToyRuParserBuilder SetWorkIfException()
        {
            if (!_useDefaultSettings)
            {
                _workIfException = _userChatter.GetAnswerFromUser(
                    "Повторять загрузку страниц, если их загрузка не произошла? Иначе программа будет экстрено завершена, если загрузка любой страницы не удалась. (y/n): ",
                    str => str is "y" or "n") == "y";

                _exceptionNotificationHandler =
                    _workIfException &&
                    _userChatter.GetAnswerFromUser("Включить уведомление об ошибках, возникших при загрузке страницы? (y/n): ",
                        str => str is "y" or "n") == "y"
                        ? _userChatter.NotifyUserHandler
                        : null;

                _createNewHttpClientHandler =
                    _workIfException &&
                    _userChatter.GetAnswerFromUser("Позволить создавать новые экземпляры HttpClient при ошибках загрузки страниц? (Внимание, в случае включения данной опции, это может за собой повлечь появление SocketException) (y/n): ",
                        str => str is "y" or "n") == "y"
                        ? () =>
                        {
                            HttpClient?.Dispose();
                            SetHttpClient();
                            return HttpClient!;
                        }
                        : null;
            }
            else
            {
                _workIfException = true;
                _exceptionNotificationHandler = Console.WriteLine;
                _createNewHttpClientHandler = null;
            }

            return this;
        }

        public ToyRuParserBuilder SetRequestTimeOut()
        {
            if (!_useDefaultSettings)
            {
                _requestTimeOut = Convert.ToInt32(_userChatter.GetAnswerFromUser("Укажите максимальное время ожидания запроса (значение указывается в секундах): ",
                    str => int.TryParse(str, out var result) && result > 0));
            }
            else
            {
                _requestTimeOut = 50;
            }

            return this;
        }

        public ToyRuParserBuilder SetRequestDelay()
        {
            if (!_useDefaultSettings)
            {
                _requestDelay = Convert.ToInt32(_userChatter.GetAnswerFromUser("Укажите задержу между запросами (значение указывается в миллисекундах): ",
                    str => int.TryParse(str, out var result) && result >= 0));
            }
            else
            {
                _requestDelay = 800;
            }

            return this;
        }

        public ToyRuParserBuilder SetProxy()
        {
            if (!_useDefaultSettings)
            {
                var useProxy = _userChatter.GetAnswerFromUser("Использовать прокси? (y/n): ", str => str is "y" or "n") == "y";

                if (useProxy)
                {
                    var validIpAddressPortRegex = @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]):[\d]+$";
                    var uriForProxy = new Uri("http://" + _userChatter.GetAnswerFromUser("Введите Uri прокси ({Ip Address}:{Port}): ", str => Regex.IsMatch(str, validIpAddressPortRegex)));
                    var proxy = new WebProxy
                    {
                        Address = uriForProxy,
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false,
                    };

                    var useAuthenticatedProxy = _userChatter.GetAnswerFromUser("Использовать аутентифицированный прокси? (y/n): ", str => str is "y" or "n") == "y";

                    if (useAuthenticatedProxy)
                    {
                        var proxyUserName = _userChatter.GetAnswerFromUser("Введите имя пользователя прокси: ", str => !string.IsNullOrEmpty(str));
                        var proxyPassword = _userChatter.GetAnswerFromUser("Введите пароль пользователя прокси: ", str => !string.IsNullOrEmpty(str));

                        proxy.Credentials = new NetworkCredential(proxyUserName, proxyPassword);
                    }

                    _proxy = proxy;
                }
                else
                {
                    _proxy = null;
                }
            }
            else
            {
                _proxy = null;
            }

            return this;
        }

        public ToyRuParserBuilder ChangeParsingRegion(AvailableParsingRegionsEnum region)
        {
            SetParsingRegion(region);
            SetHttpClientHandler();
            SetHttpClient();

            _htmlDownloader.HttpClient = HttpClient;

            return this;
        }

        public ToyRuParserBuilder SetParsingRegion(AvailableParsingRegionsEnum region)
        {
            _cookieContainer = new CookieContainer();
            switch (region)
            {
                case AvailableParsingRegionsEnum.SaintPetersburg:
                    _cookieContainer.Add(new Uri("https://www.toy.ru"), new Cookie("BITRIX_SM_city", "78000000000"));
                    break;

                case AvailableParsingRegionsEnum.Rostov:
                    _cookieContainer.Add(new Uri("https://www.toy.ru"), new Cookie("BITRIX_SM_city", "61000001000"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(region));
            }

            return this;
        }

        public ToyRuParserBuilder SetHttpClient()
        {
            HttpClient = new HttpClient(handler: _httpClientHandler, disposeHandler: false);
            HttpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            HttpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
            HttpClient.Timeout = TimeSpan.FromSeconds(_requestTimeOut);

            return this;
        }

        public ToyRuParserBuilder SetHttpClientHandler()
        {
            if (_proxy != null)
            {
                _httpClientHandler = new HttpClientHandler
                {
                    Proxy = _proxy,
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    CookieContainer = _cookieContainer,
                };
            }
            else
            {
                _httpClientHandler = new HttpClientHandler()
                {
                    CookieContainer = _cookieContainer
                };
            }

            return this;
        }

        public void Dispose()
        {
            _httpClientHandler.Dispose();
            _context.Dispose();
            HttpClient.Dispose();
        }
    }
}
