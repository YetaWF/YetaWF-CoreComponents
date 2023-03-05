using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class GuidNullableJsonConverter : JsonConverter<Guid?> {
        
        public override bool HandleNull => true;

        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return default;
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return default;
                if (!Guid.TryParse(s, out Guid result)) throw new JsonException($"Can't convert {s} to Guid");
                return (Guid?)result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options) {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.ToString());
        }
    }

    public class GuidJsonConverter : JsonConverter<Guid> {

        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!Guid.TryParse(s, out Guid result)) throw new JsonException($"Can't convert {s} to Guid");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.ToString());
        }
    }
}
