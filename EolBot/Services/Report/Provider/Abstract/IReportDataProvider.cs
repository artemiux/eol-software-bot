namespace EolBot.Services.Report.Provider.Abstract
{
    public interface IReportDataProvider
    {
        IEnumerable<ReportItem> Get(DateTime fromInclusive, DateTime toInclusive);
    }
}
