﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YetaWF.Core.JsonConverters {

    public class DecimalJsonConverter : JsonConverter<decimal> {
            public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.Number) {
                return reader.GetDecimal();
            } else if (reader.TokenType == JsonTokenType.String) {
                var s = reader.GetString();
                if (!decimal.TryParse(s, out decimal result))
                    throw new JsonException($"Expected decimal value: {s}");
                return result;
            } else throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Decimal value, JsonSerializerOptions options) {
            writer.WriteNumberValue((decimal)value);
        }
    }
}