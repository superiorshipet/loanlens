using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanLens.Application.Common;

public class FlexibleEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return default;
            }

            if (Enum.TryParse<T>(stringValue, ignoreCase: true, out var parsedEnum))
            {
                return parsedEnum;
            }

            if (int.TryParse(stringValue, out var intFromString))
            {
                return (T)Enum.ToObject(typeof(T), intFromString);
            }

            var allowedNames = string.Join(", ", Enum.GetNames<T>());
            throw new JsonException($"Invalid value '{stringValue}' for enum {typeof(T).Name}. Allowed values: {allowedNames}");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var intVal))
            {
                return (T)Enum.ToObject(typeof(T), intVal);
            }

            if (reader.TryGetInt64(out var longVal))
            {
                return (T)Enum.ToObject(typeof(T), longVal);
            }
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} for enum {typeof(T).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
