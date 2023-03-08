using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class EnumJsonConverter : JsonConverter<Enum> {

        public override bool CanConvert(Type type) {
            return type.IsEnum;
        }

        public override Enum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Number) {
                var val = reader.GetInt32();
                return (Enum)Enum.ToObject(typeToConvert, val);
                //return (Enum)Convert.ChangeType(val, typeToConvert);
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!Enum.TryParse(typeToConvert, s, out object? result)) throw new JsonException($"Can't convert {s} to {typeToConvert.FullName}");
                return (Enum)result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Enum value, JsonSerializerOptions options) {
            int v = (int)Convert.ChangeType(value, typeof(int));
            writer.WriteNumberValue(v);
        }
    }
}
