using System.Text.Json.Serialization;

namespace EolBot.Services.Report.Provider.EndoflifeDate
{
    public partial class EndoflifeDateProvider
    {
        public class ProductDetail
        {
            [JsonPropertyName("id")]
            public required string Id { get; init; }

            [JsonPropertyName("name")]
            public required string Name { get; init; }
        }
    }
}
