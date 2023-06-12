using System.Net;
using System.Text.RegularExpressions;
using AngleSharp;
using TaskNotissimusParse;

var cookieContainer = new CookieContainer();
cookieContainer.Add(new Uri("https://www.toy.ru"), new Cookie("BITRIX_SM_city", "61000001000"));

using var httpClient = GetHttpClient(cookieContainer);
httpClient.Timeout = TimeSpan.FromSeconds(50);

var productsFromSaintPetersburg = await ParseProducts(httpClient);

foreach (var product in productsFromSaintPetersburg)
{
    Console.WriteLine($"{product.Name}, {product.Price}, {product.OldPrice}, {product.Available}, {product.Region} {string.Join(">", product.Breadcrumbs)}");
}

static async Task<IEnumerable<Product>> ParseProducts(HttpClient httpClient)
{
    var config = Configuration.Default.WithDefaultLoader().WithCulture("ru-ru");
    var context = BrowsingContext.New(config);
    var html = await httpClient.GetStringAsync("https://www.toy.ru/catalog/boy_transport/");
    var document = await context.OpenAsync(req => req.Header("Content-Type", "text/html; charset=utf-8").Content(html));

    var maxPageSelector = "ul.pagination.justify-content-between li:nth-last-child(2) a";
    var maxPageElement = document.QuerySelector(maxPageSelector);
    var maxPageValue = maxPageElement == null ? 1 : int.Parse(maxPageElement.TextContent);

    var catalogURIs = new List<string>();
    for (int i = 1; i <= maxPageValue; i++)
    {
        catalogURIs.Add($"https://www.toy.ru/catalog/boy_transport/?filterseccode%5B0%5D=transport&PAGEN_5={i}");
    }

    var downloader = new HtmlDownloader(httpClient);

    var catalogParser = new CatalogParser(downloader, context);
    var productURIs = await catalogParser.ParseAsync(catalogURIs.ToArray());

    var productParser = new ProductParser(downloader, context);
    return await productParser.ParseAsync(productURIs.ToArray());
}

static HttpClient GetHttpClient(CookieContainer cookieContainer)
{
    var useProxy = GetAnswerFromUser("Использовать прокси? (y/n): ", Console.ReadKey, info => info.KeyChar is 'y' or 'n').KeyChar == 'y';

    if (useProxy)
    {
        var validIpAddressPortRegex = @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]):[\d]+$";
        var uriForProxy = new Uri("http://" + GetAnswerFromUser("Введите Uri прокси ({Ip Address}:{Port}): ", Console.ReadLine, str => Regex.IsMatch(str, validIpAddressPortRegex)));
        var proxy = new WebProxy
        {
            Address = uriForProxy,
            BypassProxyOnLocal = false,
            UseDefaultCredentials = false,
        };

        var useAuthenticatedProxy = GetAnswerFromUser("Использовать аутентифицированный прокси? (y/n): ", Console.ReadKey, info => info.KeyChar is 'y' or 'n').KeyChar == 'y';

        if (useAuthenticatedProxy)
        {
            var proxyUserName = GetAnswerFromUser("Введите имя пользователя прокси: ", Console.ReadLine, str => !string.IsNullOrEmpty(str));
            var proxyPassword = GetAnswerFromUser("Введите пароль пользователя прокси: ", Console.ReadLine, str => !string.IsNullOrEmpty(str));

            proxy.Credentials = new NetworkCredential(proxyUserName, proxyPassword);
        }

        var httpClientHandler = new HttpClientHandler
        {
            Proxy = proxy,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            CookieContainer = cookieContainer,
        };

        return new HttpClient(handler: httpClientHandler, disposeHandler: true);
    }
    else
    {
        var httpClientHandler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
        };

        return new HttpClient(handler: httpClientHandler, disposeHandler: true);
    }
}

static T GetAnswerFromUser<T>(string question, Func<T> answerHandler, Predicate<T>? checkAnswerPredicate = null)
{
    while (true)
    {
        Console.WriteLine(question);
        var answer = answerHandler();
        Console.WriteLine();

        if (checkAnswerPredicate != null && checkAnswerPredicate(answer))
        {
            return answer;
        }
        else
        {
            Console.WriteLine("Ошибка ввода. Повторите ввод.");
        }
    }
}
