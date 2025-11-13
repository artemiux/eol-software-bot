using EolBot.Services.Report.Provider.EndoflifeDate.Api;

namespace EolBot.Tests.Report
{
    public class EndOfLifeDateClientTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly EndOfLifeDateClient _client;

        public EndOfLifeDateClientTests()
        {
            _httpClient = new()
            {
                BaseAddress = new Uri("https://endoflife.date/api/v1/")
            };
            _client = new EndOfLifeDateClient(_httpClient);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetFullProductsAsync_ShouldReturnData()
        {
            var actual = await _client.GetFullProductsAsync();
            Assert.True(actual?.Total > 0);
            Assert.Equal(actual?.Total, actual?.Result.Count());
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
