using AngleSharp;

namespace TaskNotissimusParse
{
    public class CatalogParser : IParser<IEnumerable<string>>
    {
        private readonly IHtmlDownloader _downloader;
        private readonly IBrowsingContext _context;

        public CatalogParser(IHtmlDownloader downloader, IBrowsingContext context)
        {
            _downloader = downloader;
            _context = context;
        }

        public async Task<IEnumerable<string>> ParseAsync(params string[] urls)
        {
            //Уже загруженные страницы начинают парсится пока загружаются другие
            //Локер в HtmlDownloader гарантирует одновременную загрузку только одной страницы
            var tasks = urls.Select(async url =>
            {
                var html = _downloader.DownloadAsync(url);
                return await ParseHtmlAsync(html);
            });

            var results = await Task.WhenAll(tasks);

            return results.SelectMany(iEnumerableString => iEnumerableString);
        }

        private async Task<IEnumerable<string>> ParseHtmlAsync(string html)
        {
            var document = await _context.OpenAsync(req => req.Content(html));
            var productUrlsSelector = "div.product-card div.row div:first-child a";
            var productUrlsElements = document.QuerySelectorAll(productUrlsSelector);

            return productUrlsElements.Select(el => "https://www.toy.ru" + el.GetAttribute("href"));
        }
    }
}
