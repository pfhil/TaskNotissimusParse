using AngleSharp;

namespace TaskNotissimusParse
{
    public class CatalogParser : IParser<string[]>
    {
        private readonly IBrowsingContext _context;

        public CatalogParser(IBrowsingContext context)
        {
            _context = context;
        }

        public async Task<string[]> ParseAsync(string html, RequestMetadata? _ = null)
        {
            using var document = await _context.OpenAsync(req => req.Content(html));
            var productUrlsSelector = "div.product-card div.row div:first-child a";
            var productUrlsElements = document.QuerySelectorAll(productUrlsSelector);

            return productUrlsElements.Select(el => "https://www.toy.ru" + el.GetAttribute("href")).ToArray();
        }
    }
}
