using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Converters;

public sealed class DateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd";

    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("DateTime value is null or empty");

        return DateTime.ParseExact(
            value,
            Format,
            CultureInfo.InvariantCulture
        );
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}
