/* Copyright © 2021 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

// https://stackoverflow.com/questions/31325866/newtonsoft-json-cannot-convert-model-with-typeconverter-attribute
// Used to ignore a custom type converter for JSON serialization/deserialization

namespace YetaWF.Core.Serializers {

    public class NoTypeConverterJsonConverter<T> : JsonConverter {

        static readonly IContractResolver resolver = new NoTypeConverterContractResolver();

        class NoTypeConverterContractResolver : DefaultContractResolver {
            protected override JsonContract CreateContract(Type objectType) {
                if (typeof(T).IsAssignableFrom(objectType)) {
                    var contract = this.CreateObjectContract(objectType);
                    contract.Converter = null; // Also null out the converter to prevent infinite recursion.
                    return contract;
                }
                return base.CreateContract(objectType);
            }
        }

        public override bool CanConvert(Type objectType) {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
            return JsonSerializer.CreateDefault(new JsonSerializerSettings { ContractResolver = resolver }).Deserialize(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
            JsonSerializer.CreateDefault(new JsonSerializerSettings { ContractResolver = resolver }).Serialize(writer, value);
        }
    }
}
