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
            var nList = array.Select(e => e ? 1 : 0).ToArray();
            serializer.Serialize(writer,nList);
        }

        public override BitArray ReadJson(JsonReader reader, Type objectType, BitArray existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var nArray = serializer.Deserialize<int[]>(reader);
            var boolArray = nArray?.Select(e => e != 0).ToArray();
            // var boolArray = JsonConvert.DeserializeObject<bool[]>(reader.ReadAsString());
            return boolArray is null ? null : new BitArray(boolArray);
        }
    }
}