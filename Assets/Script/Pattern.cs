using System.Collections;
using System.Collections.Generic;
using System.Linq;
using V = TypedArray<int>;

namespace WFC
{
    public partial class WfcUtils<T>
    {
        public class Pattern
        {
            public Pattern(T[] data, int neighbourLength,int bitsetSize)
            {
                this.Data = data;
                // Value = ;
                Valid = Enumerable.Range(0, neighbourLength).Select(_ => new BitArray(bitsetSize)).ToArray();
            }

            public string Id { get; private set; }
            public float Frequency { get; set; }
            public T Value { get; set; }
            public T[] Data { get; set; }
            public BitArray[] Valid {get; set; }

            private int? _cachedHash;
            public int Hash => _cachedHash ??= ((IStructuralEquatable) this.Data).GetHashCode(EqualityComparer<T>.Default);

            public static bool operator ==(Pattern a, Pattern b)
            {
                return a?.Hash == b?.Hash;
            }
            public static bool operator !=(Pattern a, Pattern b)
            {
                return a?.Hash != b?.Hash;
            }
        }
    }
}
