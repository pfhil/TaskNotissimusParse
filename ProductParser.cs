using AngleSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;

namespace TaskNotissimusParse
{
    public class ProductParser : IParser<Product>
    {
        private readonly IBrowsingContext _context;

        public ProductParser(IBrowsingContext context)
        {
            _context = context;
        }

        public async Task<Product> ParseAsync(string html, RequestMetadata? metadata)
        {
            var document = await _context.OpenAsync(req => req.Header("Content-Type", "text/html; charset=utf-8").Content(html));

            //Сомнительное решение (асинхронно запускать сбор информации по картрочке товара), но это требование есть в пунке задания под №3
            //Это имело бы смысл, если бы программа парсила страницы, когда все они были бы загружены и тогда парсинг можно было бы проводить в нескольких потоках одновременно
            
            //Но в моей реализации парсинг происходит по мере загрузки страниц, в этом есть несколько плюсов:
            // 1)Можно уже начинать парсинг загруженных страниц, когда когда программа ждет загрузки других страниц
            // 2)Не требуется держать в памяти всю коллекцию html страниц перед их парсингом 
            return await Task.Run(() =>
            {
                var nameSelector = "h1.detail-name";
                var priceSelector = "span.price";
                var oldPriceSelector = "span.old-price";
                var breadcrumbsSelector = "nav.breadcrumb span > span, a.breadcrumb-item + span";
                var availableOkSelector = "span.ok";
                var availableNotSelector = "div.net-v-nalichii";
                var imagesSelector = "div.detail-image img";
                var regionSelector = "div.select-city-link [data-src=\"#region\"]";


                var name = document.QuerySelector(nameSelector)?.TextContent.Trim();

                var priceElement = document.QuerySelector(priceSelector);
                decimal? price = priceElement == null ? null : Convert.ToDecimal(new string(priceElement.TextContent.Trim().SkipLast(5).ToArray()));

                var oldPriceElement = document.QuerySelector(oldPriceSelector);
                decimal? oldPrice = oldPriceElement == null ? null : oldPriceElement.TextContent.Trim() == "" ? null : Convert.ToDecimal(new string(oldPriceElement.TextContent.Trim().SkipLast(5).ToArray()));

                var breadcrumbsElement = document.QuerySelectorAll(breadcrumbsSelector);
                var breadcrumbs = breadcrumbsElement == null ? null : string.Join('>', breadcrumbsElement.Select(el => el.TextContent.Trim()));

                var availableOkElement = document.QuerySelector(availableOkSelector);
                var availableNotElement = document.QuerySelector(availableNotSelector);

                //Если оба элемента равны null, то available равен null
                bool? available = availableOkElement != null ? true : availableNotElement != null ? false : null;

                var imageLinksElement = document.QuerySelectorAll(imagesSelector);
                string? imageLink = imageLinksElement == null ? null : string.Join(' ', imageLinksElement.Select(img => img.GetAttribute("src")));

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
                    Link = metadata?.Uri.ToString(),
                };
            });
        }
    }
}
