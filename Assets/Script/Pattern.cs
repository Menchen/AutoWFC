using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Script.Converters;
using V = TypedArray<int>;

namespace WFC
{
    public partial class WfcUtils<T>
    {
        public class Pattern
        {
            public Pattern()
            {
                
            }
            public Pattern(T[] data, int neighbourLength,int bitsetSize,int vol)
            {
                this.Data = data ?? throw new ArgumentNullException();
                // Get center
                Value = data[vol / 2];
                    
                Valid = Enumerable.Range(0, neighbourLength).Select(_ => new BitArray(bitsetSize)).ToArray();
            }

            public int Id { get; set; }
            public float Frequency { get; set; }
            public T Value { get; set; }
            public T[] Data { get; set; }
            
            [JsonProperty(ItemConverterType = typeof(BitArrayConverter))]
            public BitArray[] Valid {get; set; }

             
            [JsonIgnore]
            private int? _cachedHash = null;
            
            [JsonIgnore]
            public int Hash => _cachedHash ??= ((IStructuralEquatable) this.Data).GetHashCode(EqualityComparer<T>.Default);
            // public int Hash => Value;

            public static bool operator ==(Pattern a, Pattern b)
            {
                return a?.Hash == b?.Hash;
            }
            public static bool operator !=(Pattern a, Pattern b)
            {
                return a?.Hash != b?.Hash;
            }

            

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Hash == ((Pattern) obj).Hash;
            }

            public override int GetHashCode()
            {
                return Hash;
            }
        }
    }
}
