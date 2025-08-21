using EolBot.Services.Git.Abstract;
using EolBot.Services.Report.Provider.EndoflifeDate;
using EolBot.Services.Report.Provider.EndoflifeDate.Api;
using EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace EolBot.Tests.Report
{
    public class EndoflifeDateProviderTests
    {
        private readonly string _repoData;
        private readonly IOptions<RepositorySettings> _options;
        private readonly IGitService _gitService;

        public EndoflifeDateProviderTests()
        {
            var repoPath = Path.Combine(Path.GetTempPath(), "test-release-data");
            _repoData = Path.Combine(repoPath, "releases");

            var mockOptions = new Mock<IOptions<RepositorySettings>>();
            mockOptions.Setup(x => x.Value).Returns(new RepositorySettings
            {
                Url = It.IsAny<string>(),
                LocalPath = repoPath
            });
            _options = mockOptions.Object;

            var mockGitService = new Mock<IGitService>();
            // Do nothing with the repo.
            mockGitService.Setup(x => x.EnsureCloned(It.IsAny<string>(), It.IsAny<string>()));
            mockGitService.Setup(x => x.Pull(It.IsAny<string>()));
            _gitService = mockGitService.Object;
        }

        [Fact]
        public async Task Get_FindsExactlyTwoItems()
        {
            var productAName = "product-a";
            var productBName = "product-b";
            var releaseA = new Release
            {
                Name = "1.0",
                Eol = new Eol { DateTime = new DateTime(2199, 1, 1, 0, 0, 0) }
            };
            var releaseB = new Release
            {
                Name = "2.0",
                Eol = new Eol { DateTime = new DateTime(2199, 1, 6, 0, 0, 0) }
            };
            var itemA = new EndoflifeDateReportItem
            {
                Releases = new Dictionary<string, Release>
                {
                    ["1.0"] = releaseA
                }
            };
            var itemB = new EndoflifeDateReportItem
            {
                Releases = new Dictionary<string, Release>
                {
                    ["2.0"] = releaseB
                }
            };

            Directory.CreateDirectory(_repoData);
            var fileAPath = Path.Combine(_repoData, $"{productAName}.json");
            var fileBPath = Path.Combine(_repoData, $"{productBName}.json");
            File.WriteAllText(fileAPath, JsonSerializer.Serialize(itemA));
            File.WriteAllText(fileBPath, JsonSerializer.Serialize(itemB));

            ProductListResponseItem productInfoA = new()
            {
                Name = productAName,
                Label = "Product A",
                Uri = $"https://endoflife.date/{productAName}"
            };
            ProductListResponseItem productInfoB = new()
            {
                Name = "productb",
                Aliases = [productBName], // Search by aliases
                Label = "Product B",
                Uri = "https://endoflife.date/productb"
            };
            var mockEndoflifeDateClient = new Mock<IEndOfLifeDateClient>();
            mockEndoflifeDateClient
                .Setup(x => x.GetProductsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProductListResponse
                {
                    Total = 2,
                    Result = [productInfoA, productInfoB]
                });

            var provider = new EndoflifeDateProvider(_options, _gitService, mockEndoflifeDateClient.Object);
            var actual = await provider.GetAsync(
                fromInclusive: new(2199, 1, 1, 0, 0, 0),
                toInclusive: new(2199, 1, 6, 23, 59, 59));

            File.Delete(fileAPath);
            File.Delete(fileBPath);

            Assert.Collection(actual,
                item =>
                {
                    Assert.Equal(productInfoA.Label, item.ProductName);
                    Assert.Equal(releaseA.Name, item.ProductVersion);
                    Assert.Equal(productInfoA.Uri, item.ProductUrl);
                    Assert.Equal(releaseA.Eol.DateTime, item.Eol);
                },
                item =>
                {
                    Assert.Equal(productInfoB.Label, item.ProductName);
                    Assert.Equal(releaseB.Name, item.ProductVersion);
                    Assert.Equal(productInfoB.Uri, item.ProductUrl);
                    Assert.Equal(releaseB.Eol.DateTime, item.Eol);
                });
        }
    }
}
