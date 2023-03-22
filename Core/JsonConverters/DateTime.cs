﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

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