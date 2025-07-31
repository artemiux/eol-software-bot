namespace EolBot.Services.Report
{
    public class ReportSettings
    {
        public required int DaysToCover { get; set; }

        public required int MaxConcurrentMessages { get; set; }
    }
}
