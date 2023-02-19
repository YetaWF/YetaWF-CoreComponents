using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class IntNullableJsonConverter : JsonConverter<int?> {

        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            } else if (reader.TokenType == JsonTokenType.Number) {
                return reader.GetInt32();
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return null;
                if (!int.TryParse(s, out int result))
                    throw new JsonException($"Expected int value: {s}");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options) {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteNumberValue((int)value);
        }
    }

    public class IntJsonConverter : JsonConverter<int> {

        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Number) {
                return reader.GetInt32();
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!int.TryParse(s, out int result))
                    throw new JsonException($"Expected int value: {s}");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) {
            writer.WriteNumberValue((int)value);
        }
    }
}
