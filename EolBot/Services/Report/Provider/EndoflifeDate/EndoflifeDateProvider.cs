using EolBot.Services.Git.Abstract;
using EolBot.Services.Report.Provider.Abstract;
using EolBot.Services.Report.Provider.EndoflifeDate.Api;
using EolBot.Services.Report.Provider.EndoflifeDate.Api.Abstract;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EolBot.Services.Report.Provider.EndoflifeDate
{
    public partial class EndoflifeDateProvider : IReportDataProvider
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _repoUrl;
        private readonly string _repoPath;
        private readonly string _repoData;

        private readonly IGitService _gitService;
        private readonly IEndOfLifeDateClient _endOfLifeDateClient;
        private readonly ILogger<EndoflifeDateProvider>? _logger;

        private IEnumerable<ProductListResponseItem>? _products;

        public EndoflifeDateProvider(
            IOptions<RepositorySettings> options,
            IGitService gitService,
            IEndOfLifeDateClient endOfLifeClient,
            ILogger<EndoflifeDateProvider>? logger = null)
        {
            _repoUrl = options.Value.Url;
            _repoPath = options.Value.LocalPath;
            _repoData = Path.Combine(_repoPath, "releases");

            _gitService = gitService;
            _endOfLifeDateClient = endOfLifeClient;
            _logger = logger;
        }

        public async Task<IEnumerable<ReportItem>> GetAsync(DateTime fromInclusive, DateTime toInclusive,
            CancellationToken cancellationToken = default)
        {
            _ = _gitService.EnsureCloned(_repoUrl, _repoPath);
            _gitService.Pull(_repoPath);

            await LoadProductsAsync(cancellationToken);

            List<ReportItem> items = [];
            foreach (var file in Directory.EnumerateFiles(_repoData, "*.json"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var deserializedContent = JsonSerializer
                    .Deserialize<EndoflifeDateReportItem>(File.ReadAllText(file), _jsonSerializerOptions);
                if (deserializedContent == null)
                {
                    _logger?.LogWarning("{FileName} has no content", Path.GetFileName(file));
                    continue;
                }
                var foundItems = from release in deserializedContent.Releases.Values
                                 where (release.Eol.DateTime >= fromInclusive && release.Eol.DateTime <= toInclusive)
                                    || (release.Eoes >= fromInclusive && release.Eoes <= toInclusive)
                                 let productId = Path.GetFileNameWithoutExtension(file)
                                 let eoesMatched = release.Eoes >= fromInclusive && release.Eoes <= toInclusive
                                 let product = FindProduct(productId)
                                 select new ReportItem
                                 {
                                     ProductName = product?.Label ?? productId,
                                     ProductVersion = release.Name,
                                     ProductUrl = $"https://endoflife.date/{product?.Name ?? productId}",
                                     Eol = eoesMatched ? release.Eoes!.Value : release.Eol.DateTime!.Value
                                 };
                items.AddRange(foundItems);
            }

            return items;
        }

        private async Task LoadProductsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _endOfLifeDateClient.GetProductsAsync(cancellationToken);
                if (response?.Total > 0)
                {
                    _products = response.Result;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Could not fetch products with the End‑of‑Life API");
            }
        }

        private ProductListResponseItem? FindProduct(string id)
        {
            return _products?
                .FirstOrDefault(x =>
                    string.Equals(id, x.Name, StringComparison.OrdinalIgnoreCase)
                        || x.Aliases.Any(y => string.Equals(id, y, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
