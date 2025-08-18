using EolBot.Services.Git.Abstract;
using EolBot.Services.Report.Provider.Abstract;
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
        private readonly ILogger<EndoflifeDateProvider>? _logger;

        private ProductDetail[]? _productDetails;

        public EndoflifeDateProvider(
            IOptions<RepositorySettings> options,
            IGitService gitService,
            ILogger<EndoflifeDateProvider>? logger = null)
        {
            _repoUrl = options.Value.Url;
            _repoPath = options.Value.LocalPath;
            _repoData = Path.Combine(_repoPath, "releases");

            _gitService = gitService;
            _logger = logger;

            LoadProductDetails();
        }

        public IEnumerable<ReportItem> Get(DateTime fromInclusive, DateTime toInclusive)
        {
            _ = _gitService.EnsureCloned(_repoUrl, _repoPath);
            _gitService.Pull(_repoPath);

            List<ReportItem> items = [];
            foreach (var file in Directory.EnumerateFiles(_repoData, "*.json"))
            {
                try
                {
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
                                     select new ReportItem
                                     {
                                         ProductName = GetProductNameOrDefault(productId),
                                         ProductVersion = release.Name,
                                         ProductUrl = $"https://endoflife.date/{productId}",
                                         Eol = eoesMatched ? release.Eoes!.Value : release.Eol.DateTime!.Value
                                     };
                    items.AddRange(foundItems);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("An error occured while reading {File}: {Message}", Path.GetFileName(file), ex.Message);
                }
            }

            return items;
        }

        /*
            Loads optional `products.json` file that provides full product names.
            It should be periodically updated, for example, by running the following code in the browser console
            while staying on https://endoflife.date/:

            let jsonArray = [];
            document.querySelectorAll('.nav-list-item').forEach(item => {
            let anchor = item.querySelector('a');
            if(anchor) {
                let obj = {
                "id": anchor.getAttribute('href').slice(1),
                "name": anchor.textContent.trim()
                };
                jsonArray.push(obj);
            }
            });
            console.log(JSON.stringify(jsonArray, null, "\t"));
        */
        private void LoadProductDetails()
        {
            if (File.Exists("products.json"))
            {
                try
                {
                    _productDetails = JsonSerializer.Deserialize<ProductDetail[]>(
                        File.ReadAllText("products.json"));
                }
                catch { }
            }
        }

        private string GetProductNameOrDefault(string productId)
        {
            return _productDetails?.FirstOrDefault(x => x.Id == productId)?.Name ?? productId;
        }
    }
}
