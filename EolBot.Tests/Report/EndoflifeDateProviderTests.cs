using EolBot.Services.Git.Abstract;
using EolBot.Services.Report.Provider.EndoflifeDate;
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
        public void Get_FindsExactlyTwoItems()
        {
            Directory.CreateDirectory(_repoData);

            var productAName = "product-a";
            var productBName = "product-b";
            var fileAPath = Path.Combine(_repoData, $"{productAName}.json");
            var fileBPath = Path.Combine(_repoData, $"{productBName}.json");
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

            File.WriteAllText(fileAPath, JsonSerializer.Serialize(itemA));
            File.WriteAllText(fileBPath, JsonSerializer.Serialize(itemB));

            var provider = new EndoflifeDateProvider(_options, _gitService);
            var actual = provider.Get(
                fromInclusive: new(2199, 1, 1, 0, 0, 0),
                toInclusive: new(2199, 1, 6, 23, 59, 59));

            File.Delete(fileAPath);
            File.Delete(fileBPath);

            Assert.Collection(actual,
                item =>
                {
                    Assert.Equal(productAName, item.ProductName);
                    Assert.Equal(releaseA.Name, item.ProductVersion);
                    Assert.Equal($"https://endoflife.date/{productAName}", item.ProductUrl);
                    Assert.Equal(releaseA.Eol.DateTime, item.Eol);
                },
                item =>
                {
                    Assert.Equal(productBName, item.ProductName);
                    Assert.Equal(releaseB.Name, item.ProductVersion);
                    Assert.Equal($"https://endoflife.date/{productBName}", item.ProductUrl);
                    Assert.Equal(releaseB.Eol.DateTime, item.Eol);
                });
        }
    }
}
