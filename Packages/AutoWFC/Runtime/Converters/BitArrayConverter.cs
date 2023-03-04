using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;

namespace AutoWfc.Runtime.Converters
{
    public class BitArrayConverter : JsonConverter<BitArray>
    {
        private struct PackedBoolArray
        {
            public int Size;
            public byte[] Data;
        }
        public override void WriteJson(JsonWriter writer, BitArray value, JsonSerializer serializer)
        {
            bool[] array = new bool[value.Count];
            value.CopyTo(array, 0);
            var byteArray = PackBoolsInByteArray(array);
            // var nList = array.Select(e => e ? 1 : 0).ToArray();
            serializer.Serialize(writer,new PackedBoolArray
            {
                Size = value.Count,
                Data = byteArray,
            });
        }

        public override BitArray ReadJson(JsonReader reader, Type objectType, BitArray existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var packedBoolArray = serializer.Deserialize<PackedBoolArray>(reader);
            var paddedBitArray = new BitArray(packedBoolArray.Data);
            var boolArray = new bool[paddedBitArray.Count];
            paddedBitArray.CopyTo(boolArray,0);
            return new BitArray(boolArray.Take(packedBoolArray.Size).ToArray());
        }
        
        // Credits: https://stackoverflow.com/questions/713057/convert-bool-to-byte
        public byte[] PackBoolsInByteArray(bool[] bools)
        {
            int len = bools.Length;
            int bytes = len >> 3;
            if ((len & 0x07) != 0) ++bytes;
            byte[] arr2 = new byte[bytes];
            for (int i = 0; i < bools.Length; i++)
            {
                if (bools[i])
                    arr2[i >> 3] |= (byte)(1 << (i & 0x07));
            }
            return arr2;
        }
    }
}