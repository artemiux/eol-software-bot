namespace EolBot.Services.Report
{
    public class ReportSettings
    {
        public required int DaysToCover { get; init; }

        public required int MaxConcurrentMessages { get; init; }
    }
}
