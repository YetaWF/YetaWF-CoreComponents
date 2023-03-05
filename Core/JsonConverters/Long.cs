using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class LongNullableJsonConverter : JsonConverter<long?> {

        public override bool HandleNull => true;

        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            } else if (reader.TokenType == JsonTokenType.Number) {
                return reader.GetInt64();
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return null;
                if (!long.TryParse(s, out long result))
                    throw new JsonException($"Expected long value: {s}");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options) {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteNumberValue((long)value);
        }
    }
    public class LongJsonConverter : JsonConverter<long> {

        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Number) {
                return reader.GetInt64();
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!long.TryParse(s, out long result))
                    throw new JsonException($"Expected long value: {s}");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) {
            writer.WriteNumberValue((long)value);
        }
    }
}
