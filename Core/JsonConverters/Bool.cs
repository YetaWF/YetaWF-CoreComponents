using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class BoolJsonConverter : JsonConverter<bool> {

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.False) {
                return false;
            } else if (reader.TokenType == JsonTokenType.True) {
                return true;
            } else if (reader.TokenType == JsonTokenType.Number) {
                return reader.GetInt32() != 0;
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) throw new JsonException($"Expected bool value: {s}");
                s = s.ToLower();
                if (string.CompareOrdinal(s, "true") == 0) return true;
                if (string.CompareOrdinal(s, "false") == 0) return false;
                throw new JsonException($"Expected bool value: {s}");
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) {
            writer.WriteBooleanValue((bool)value);
        }
    }
}
