using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EolBot.Services.Report.Provider.EndoflifeDate
{
    public class EndoflifeDateReportItem
    {
        [JsonPropertyName("releases")]
        public required Dictionary<string, Release> Releases { get; set; }
    }

    public class Release
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonConverter(typeof(EolConverter))]
        [JsonPropertyName("eol")]
        public Eol Eol { get; set; }

        [JsonPropertyName("eoes")]
        public DateTime? Eoes { get; set; }
    }

    public struct Eol
    {
        public bool? Bool;
        public DateTime? DateTime;
    }

    public class EolConverter : JsonConverter<Eol>
    {
        public override bool CanConvert(Type t) => t == typeof(Eol);

        public override Eol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    if (DateTime.TryParse(
                            reader.GetString(),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out DateTime dt))
                    {
                        return new Eol { DateTime = dt };
                    }
                    break;
                case JsonTokenType.True:
                    return new Eol { Bool = true };
                case JsonTokenType.False:
                    return new Eol { Bool = false };
            }
            throw new JsonException($"Cannot unmarshal type {nameof(Eol)}.");
        }

        public override void Write(Utf8JsonWriter writer, Eol value, JsonSerializerOptions options)
        {
            if (value.Bool != null)
            {
                JsonSerializer.Serialize(writer, value.Bool, options);
                return;
            }
            if (value.DateTime != null)
            {
                JsonSerializer.Serialize(writer,
                    value.DateTime.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    options);
                return;
            }
            throw new JsonException($"Cannot marshal type {nameof(Eol)}.");
        }
    }
}