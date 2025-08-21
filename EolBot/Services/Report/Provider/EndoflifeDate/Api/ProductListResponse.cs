namespace EolBot.Services.Report.Provider.EndoflifeDate.Api
{
    public class ProductListResponse
    {
        public int Total { get; set; }

        public IEnumerable<ProductListResponseItem> Result { get; set; } = [];
    }

    public class ProductListResponseItem
    {
        public required string Name { get; set; }

        public required string Label { get; set; }

        public IEnumerable<string> Aliases { get; set; } = [];

        public required string Uri { get; set; }
    }
}
