using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using YetaWF.Core.Models;

namespace YetaWF.Core.JsonConverters {

    public class MultiStringJsonConverter : JsonConverter<MultiString> {

        //public override bool HandleNull => true;
         
        public override MultiString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var dictionary = new MultiString();
            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return dictionary;
                if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
                while (reader.Read()) {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;
                    // Get the key.
                    if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                    string key = reader.GetString() ?? throw new JsonException();
                    // Get the value.
                    if (!reader.Read()) throw new JsonException();
                    string? value = reader.GetString() ?? throw new JsonException();
                    // Add to dictionary.
                    dictionary.Remove(key);// en-us is already installed by default, so remove all to avoid dups
                    dictionary.Add(key, value);
                }
            }
            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, MultiString ms, JsonSerializerOptions options) {
            writer.WriteStartArray();
            foreach ((string key, string value) in ms) {
                writer.WriteStartObject();
                writer.WritePropertyName(key);
                writer.WriteStringValue(value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }

}
