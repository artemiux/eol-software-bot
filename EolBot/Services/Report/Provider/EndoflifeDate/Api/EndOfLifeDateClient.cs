using EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract;
using System.Net.Http.Json;

namespace EolBot.Services.Report.Provider.EndoflifeDate.Api
{
    public class EndOfLifeDateClient(HttpClient httpClient) : IEndOfLifeDateClient
    {
        public async Task<ProductListResponse?> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<ProductListResponse>("products", cancellationToken);
        }
    }
}
