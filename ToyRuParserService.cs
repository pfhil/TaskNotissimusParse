using AngleSharp;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TaskNotissimusParse
{
    public class ToyRuParserService
    {
        private readonly IHtmlDownloader _downloader;
        private readonly IParser<Product> _productParser;
        private readonly IParser<string[]> _catalogParser;
        private readonly IBrowsingContext _context;

        public ToyRuParserService(IHtmlDownloader downloader, IParser<Product> productParser, IParser<string[]> catalogParser, IBrowsingContext? context = null)
        {
            _downloader = downloader;
            _productParser = productParser;
            _catalogParser = catalogParser;

            if (context != null)
            {
                _context = context;
            }
            else
            {
                var config = Configuration.Default.WithDefaultLoader().WithCulture("ru-ru");
                _context = BrowsingContext.New(config);
            }
        }

        public async Task<(IAsyncEnumerable<Product> productAsyncEnumerable, Task<Task> taskAsyncEnumerable)> ParseProductsAsync(HttpClient httpClient)
        {
            var channelUrIs = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true,
            });
            var channelProducts = Channel.CreateUnbounded<(string html, string Uri)>(new UnboundedChannelOptions
            {
                SingleWriter = true,
                SingleReader = true,
            });


            var taskGetCatalogUrIs = Task.Factory.StartNew(async () =>
            {
                try
                {
                    foreach (var catalogUrI in await GetCatalogUrIs())
                    {
                        await channelUrIs.Writer.WriteAsync(await _downloader.DownloadAsync(catalogUrI));
                    }

                }
                catch (Exception ex)
                {
                    channelUrIs.Writer.Complete();
                    throw ex;
                }
                channelUrIs.Writer.Complete();
            });
            var productURIs = (await GetProductUrIs(channelUrIs.Reader).ToListAsync()).SelectMany(ie => ie);
            await await taskGetCatalogUrIs; //ожидаем для получения исключения, если оно возникло

            var taskGetProducts = Task.Factory.StartNew(async () =>
            {
                try
                {
                    foreach (var productUrI in productURIs)
                    {
                        await channelProducts.Writer.WriteAsync((await _downloader.DownloadAsync(productUrI), productUrI));
                    }
                }
                catch (Exception ex)
                {
                    channelProducts.Writer.Complete();
                    throw ex;
                }
                channelProducts.Writer.Complete();
            });
            return (GetProducts(channelProducts.Reader), taskGetProducts);
        }

        private async Task<List<string>> GetCatalogUrIs()
        {
            var html = await _downloader.DownloadAsync("https://www.toy.ru/catalog/boy_transport/");
            var document = await _context.OpenAsync(req => req.Header("Content-Type", "text/html; charset=utf-8").Content(html));

            var maxPageSelector = "ul.pagination.justify-content-between li:nth-last-child(2) a";
            var maxPageElement = document.QuerySelector(maxPageSelector);
            var maxPageValue = maxPageElement == null ? 1 : int.Parse(maxPageElement.TextContent);

            var catalogUrIs = new List<string>();
            for (int i = 1; i <= maxPageValue; i++)
            {
                catalogUrIs.Add($"https://www.toy.ru/catalog/boy_transport/?filterseccode%5B0%5D=transport&PAGEN_5={i}");
            }

            return catalogUrIs;
        }

        private async IAsyncEnumerable<IEnumerable<string>> GetProductUrIs(ChannelReader<string> reader)
        {
            await foreach (var item in reader.ReadAllAsync())
            {
                yield return await _catalogParser.ParseAsync(item);
            }
        }

        private async IAsyncEnumerable<Product> GetProducts(ChannelReader<(string html, string Uri)> reader)
        {
            await foreach (var item in reader.ReadAllAsync())
            {
                yield return await _productParser.ParseAsync(item.html, new RequestMetadata { Uri = new Uri(item.Uri) });
            }
        }
    }
}
