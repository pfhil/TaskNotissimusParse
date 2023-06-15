using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TaskNotissimusParse;


try
{
    var userChatter = new UserChatter(Console.WriteLine, Console.ReadLine);
    var useDefaultSettings =
        userChatter.GetAnswerFromUser("Использовать стандартные настройки для парсинга? (y/n): ",
            str => str is "y" or "n") == "y";
    var parseRostov =
        userChatter.GetAnswerFromUser(
            "Парсить данные по Ростову? Внимание, в случае утвердительного ответа, парсинг займет 1-2 часа времени. (y/n): ",
            str => str is "y" or "n") == "y";

    using var toyRuParserBuilder = new ToyRuParserBuilder(useDefaultSettings, userChatter);
    var toyRuParserBuilderDirector = new ToyRuParserBuilderDirector(toyRuParserBuilder);
    toyRuParserBuilderDirector.Build(AvailableParsingRegionsEnum.SaintPetersburg);

    var toyRuParserService = toyRuParserBuilder.Build();

    {
        using var streamWriter = new StreamWriter(new FileStream("data.csv", FileMode.Create), Encoding.UTF8);
        using var csvWriter = new CsvWriter(streamWriter, CultureInfo.CurrentCulture);

        var productsSpb = await toyRuParserService.ParseProductsAsync(toyRuParserBuilder.HttpClient);
        await csvWriter.WriteRecordsAsync(productsSpb.productAsyncEnumerable);
        await await productsSpb.taskAsyncEnumerable;
    }

    if (parseRostov)
    {
        toyRuParserBuilder.ChangeParsingRegion(AvailableParsingRegionsEnum.Rostov);
        toyRuParserService = toyRuParserBuilder.Build();

        using var streamWriter = new StreamWriter(new FileStream("data.csv", FileMode.Append), Encoding.UTF8);
        using var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = false });

        var productsRostov = await toyRuParserService.ParseProductsAsync(toyRuParserBuilder.HttpClient);
        await csvWriter.WriteRecordsAsync(productsRostov.productAsyncEnumerable);
        await await productsRostov.taskAsyncEnumerable;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Ошибка - {ex.Message}");
}
