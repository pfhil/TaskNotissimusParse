using AngleSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;

namespace TaskNotissimusParse
{
    public class ProductParser : IParser<IEnumerable<Product>>
    {
        private readonly IHtmlDownloader _downloader;
        private readonly IBrowsingContext _context;

        public ProductParser(IHtmlDownloader downloader, IBrowsingContext context)
        {
            _downloader = downloader;
            _context = context;
        }

        public async Task<IEnumerable<Product>> ParseAsync(params string[] urls)
        {
            //Уже загруженные страницы начинают парсится пока загружаются другие
            //Локер в HtmlDownloader гарантирует одновременную загрузку только одной страницы
            var tasks = urls.Select(async url =>
            {
                var html = _downloader.DownloadAsync(url);
                return await ParseHtmlAsync(html, url);
            });

            var results = await Task.WhenAll(tasks);

            return results;
        }
        private async Task<Product> ParseHtmlAsync(string html, string url)
        {
            var document = await _context.OpenAsync(req => req.Header("Content-Type", "text/html; charset=utf-8").Content(html));

            var nameSelector = "h1.detail-name";
            var priceSelector = "span.price";
            var oldPriceSelector = "span.old-price";
            var breadcrumbsSelector = "nav.breadcrumb span > span, a.breadcrumb-item + span";
            var availableOkSelector = "span.ok";
            var availableNotSelector = "div.net-v-nalichii";
            var imageSelector = ".detail-image img";
            var regionSelector = "div.select-city-link [data-src=\"#region\"]";


            var name = document.QuerySelector(nameSelector)?.TextContent.Trim();

            var priceElement = document.QuerySelector(priceSelector);
            decimal? price = priceElement == null ? null : Convert.ToDecimal(new string(priceElement.TextContent.Where(char.IsDigit).ToArray()));

            var oldPriceElement = document.QuerySelector(oldPriceSelector);
            decimal? oldPrice = oldPriceElement == null ? null : oldPriceElement.TextContent.Trim() == "" ? null : Convert.ToDecimal(new string(oldPriceElement.TextContent.Where(char.IsDigit).ToArray()));

            var breadcrumbsElement = document.QuerySelectorAll(breadcrumbsSelector);
            var breadcrumbs = breadcrumbsElement == null ? null : breadcrumbsElement.Select(el => el.TextContent.Trim());

            var availableOkElement = document.QuerySelector(availableOkSelector);
            var availableNotElement = document.QuerySelector(availableNotSelector);

            //Если оба элемента равны null, то available равен null
            bool? available = availableOkElement != null ? true : availableNotElement != null ? false : null;

            string? imageLink = document.QuerySelector(imageSelector)?.GetAttribute("src");

            var region = document.QuerySelector(regionSelector)?.TextContent.Trim();

            return new Product
            {
                Name = name,
                Price = price,
                OldPrice = oldPrice,
                Breadcrumbs = breadcrumbs,
                Available = available,
                LinkOnImage = imageLink,
                Region = region,
                Link = url
            };
        }
    }
}
