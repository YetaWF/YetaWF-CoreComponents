using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class DateTimeNullableJsonConverter : JsonConverter<DateTime?> {

        public override bool HandleNull => true;

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return null;
                if (!DateTime.TryParse(s, out DateTime result))
                    throw new JsonException($"Expected DateTime value: {s}");
                return result.ToUniversalTime();
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(((DateTime)value).ToString("o"));
        }
    }

    public class DateTimeJsonConverter : JsonConverter<DateTime> {

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!DateTime.TryParse(s, out DateTime result))
                    throw new JsonException($"Expected DateTime value: {s}");
                return result.ToUniversalTime();
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString("o"));
        }
    }
}
