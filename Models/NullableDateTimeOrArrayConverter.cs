using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Denly.Models;

public sealed class NullableDateTimeOrArrayConverter : JsonConverter<DateTime?>
{
    public override DateTime? ReadJson(
        JsonReader reader,
        Type objectType,
        DateTime? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.Date)
        {
            return reader.Value as DateTime?;
        }

        if (reader.TokenType == JsonToken.String)
        {
            var text = reader.Value as string;
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                ? parsed
                : null;
        }

        if (reader.TokenType == JsonToken.StartArray)
        {
            var array = JArray.Load(reader);
            var token = array.Last ?? array.First;
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type == JTokenType.Date)
            {
                return token.ToObject<DateTime?>();
            }

            var tokenText = token.ToString();
            return DateTime.TryParse(
                tokenText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed)
                ? parsed
                : null;
        }

        return null;
    }

    public override void WriteJson(JsonWriter writer, DateTime? value, JsonSerializer serializer)
    {
        if (value.HasValue)
        {
            writer.WriteValue(value.Value);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
