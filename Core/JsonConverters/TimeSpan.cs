using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class TimeSpanNullableJsonConverter : JsonConverter<TimeSpan?> {

        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return null;
                if (!TimeSpan.TryParse(s, out TimeSpan result))
                    throw new JsonException($"Expected TimeSpan value: {s}");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options) {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.ToString());
        }
    }

    public class TimeSpanJsonConverter : JsonConverter<TimeSpan> {

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!TimeSpan.TryParse(s, out TimeSpan result))
                    throw new JsonException($"Expected TimeSpan value: {s}");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }
}
