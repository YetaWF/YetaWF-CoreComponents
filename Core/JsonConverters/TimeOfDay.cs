using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using YetaWF.Core.Components;

namespace YetaWF.Core.JsonConverters {

    public class TimeOfDayNullableJsonConverter : JsonConverter<TimeOfDay?> {

        public override TimeOfDay? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s)) return null;
                if (!DateTime.TryParse(s, out DateTime result))
                    throw new JsonException($"Expected DateTime value: {s}");
                return new TimeOfDay(result.ToUniversalTime());
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, TimeOfDay? value, JsonSerializerOptions options) {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(((TimeOfDay)value).TOD.ToString());
        }
    }

    public class TimeOfDayJsonConverter : JsonConverter<TimeOfDay> {

        public override TimeOfDay Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!DateTime.TryParse(s, out DateTime result))
                    throw new JsonException($"Expected DateTime value: {s}");
                return new TimeOfDay(result.ToUniversalTime());
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, TimeOfDay value, JsonSerializerOptions options) {
            writer.WriteStringValue($"{value.AsDateTime().ToUniversalTime():o}");
        }
    }
}
