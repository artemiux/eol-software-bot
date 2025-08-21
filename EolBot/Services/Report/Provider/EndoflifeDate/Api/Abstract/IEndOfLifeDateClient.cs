namespace EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract
{
    public interface IEndOfLifeDateClient
    {
        Task<FullProductListResponse?> GetFullProductsAsync(CancellationToken cancellationToken = default);
    }
}
