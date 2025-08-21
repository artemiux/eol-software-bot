namespace EolBot.Services.Report.Provider.EndoflifeDate.Api
{
    public class FullProductListResponse
    {
        public required int Total { get; set; }

        public IEnumerable<FullProduct> Result { get; set; } = [];
    }

    public class FullProduct
    {
        public required string Name { get; set; }

        public required string Label { get; set; }

        public required Link Links { get; set; }

        public IEnumerable<Release> Releases { get; set; } = [];
    }

    public class Link
    {
        public required string Html { get; set; }
    }

    public class Release
    {
        public required string Name { get; set; }

        public required string Label { get; set; }

        public required bool IsMaintained { get; set; }

        public required bool IsEol { get; set; }

        public DateTime? EolFrom { get; set; }

        public bool? IsEoas { get; set; }

        // Active support date.
        public DateTime? EoasFrom { get; set; }

        public bool? IsEoes { get; set; }

        // Extended support date.
        public DateTime? EoesFrom { get; set; }
    }
}
