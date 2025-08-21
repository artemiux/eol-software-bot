namespace EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract
{
    public interface IEndOfLifeDateClient
    {
        Task<ProductListResponse?> GetProductsAsync(CancellationToken cancellationToken = default);
    }
}
