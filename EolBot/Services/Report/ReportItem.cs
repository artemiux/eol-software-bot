namespace EolBot.Services.Report
{
    public class ReportItem
    {
        public required string ProductName { get; init; }

        public required string ProductVersion { get; init; }

        public string? ProductUrl { get; init; }

        public DateTime Eol { get; init; }
    }
}
