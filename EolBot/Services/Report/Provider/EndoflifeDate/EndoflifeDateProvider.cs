using EolBot.Services.Report.Provider.Abstract;
using EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract;

namespace EolBot.Services.Report.Provider.EndoflifeDate
{
    public partial class EndofLifeDateProvider(
        IEndOfLifeDateClient endOfLifeClient) : IReportDataProvider
    {
        public async Task<IEnumerable<ReportItem>> GetAsync(DateTime fromInclusive, DateTime toInclusive,
            CancellationToken cancellationToken = default)
        {
            var response = await endOfLifeClient.GetFullProductsAsync(cancellationToken)
                ?? throw new InvalidOperationException("Empty value received from the server.");

            List<ReportItem> items = [];
            foreach (var product in response.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var release in product.Releases.Where(x => x.IsMaintained))
                {
                    DateTime? eol = null;
                    if (!release.IsEol
                        && release.EolFrom >= fromInclusive && release.EolFrom <= toInclusive)
                    {
                        eol = release.EolFrom;
                    }
                    else if (release.IsEoas.HasValue && release.IsEoas == false
                        && release.EoasFrom >= fromInclusive && release.EoasFrom <= toInclusive)
                    {
                        eol = release.EoasFrom;
                    }
                    else if (release.IsEoes.HasValue && release.IsEoes == false
                        && release.EoesFrom >= fromInclusive && release.EoesFrom <= toInclusive)
                    {
                        eol = release.EoesFrom;
                    }

                    if (eol.HasValue)
                    {
                        items.Add(new ReportItem
                        {
                            ProductName = product.Label,
                            ProductVersion = release.Label,
                            ProductUrl = $"{product.Links.Html}#:~:text={Uri.EscapeDataString(release.Label)}",
                            Eol = eol.Value
                        });
                    }
                }
            }

            return items;
        }
    }
}
