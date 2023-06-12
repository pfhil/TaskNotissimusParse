namespace TaskNotissimusParse
{
    public class Product
    {
        public string? Name { get; set; }
        public string? Region { get; set; }
        public decimal? Price { get; set; }
        public decimal? OldPrice { get; set; }
        public bool? Available { get; set; }
        public IEnumerable<string>? Breadcrumbs { get; set; }
        public string? Link { get; set; }
        public string? LinkOnImage { get; set; }
    }
}
