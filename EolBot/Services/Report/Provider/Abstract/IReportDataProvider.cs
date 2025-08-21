namespace EolBot.Services.Report.Provider.Abstract
{
    public interface IReportDataProvider
    {
        Task<IEnumerable<ReportItem>> GetAsync(DateTime fromInclusive, DateTime toInclusive,
            CancellationToken cancellationToken = default);
    }
}
