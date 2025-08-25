namespace EolBot.Services.Report.Abstract
{
    public interface IReport
    {
        string Create(DateTime fromInclusive, DateTime toInclusive, IEnumerable<ReportItem> items, string? lang = null);
    }
}
