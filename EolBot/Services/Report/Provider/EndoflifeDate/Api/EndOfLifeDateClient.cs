using EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract;
using System.Net.Http.Json;

namespace EolBot.Services.Report.Provider.EndoflifeDate.Api
{
    public class EndOfLifeDateClient(HttpClient httpClient) : IEndOfLifeDateClient
    {
        public async Task<FullProductListResponse?> GetFullProductsAsync(CancellationToken cancellationToken = default)
        {
            return await httpClient.GetFromJsonAsync<FullProductListResponse>("products/full", cancellationToken);
        }
    }
}
