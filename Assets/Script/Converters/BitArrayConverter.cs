using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Script.Converters
{
    public class BitArrayConverter : JsonConverter<BitArray>
    {
        public override void WriteJson(JsonWriter writer, BitArray value, JsonSerializer serializer)
        {
            bool[] array = new bool[value.Count];
            value.CopyTo(array, 0);
            serializer.Serialize(writer,array);
        }

        public override BitArray ReadJson(JsonReader reader, Type objectType, BitArray existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var boolArray = serializer.Deserialize<bool[]>(reader);
            // var boolArray = JsonConvert.DeserializeObject<bool[]>(reader.ReadAsString());
            return boolArray is null ? null : new BitArray(boolArray);
        }
    }
}