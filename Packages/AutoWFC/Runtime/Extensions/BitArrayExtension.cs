using System;
using System.Collections;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace AutoWfc.Runtime.Extensions
{
    public static class BitArrayExtensions
    {
        public static Int32 GetCardinality(this BitArray bitArray)
        {

            Int32[] ints = new Int32[(bitArray.Count >> 5) + 1];

            bitArray.CopyTo(ints, 0);

            Int32 count = 0;

            // fix for not truncated bits in last integer that may have been set to true with SetAll()
            ints[ints.Length - 1] &= ~(-1 << (bitArray.Count % 32));

            for (Int32 i = 0; i < ints.Length; i++)
            {

                Int32 c = ints[i];

                // magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
                unchecked
                {
                    c = c - ((c >> 1) & 0x55555555);
                    c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
                    c = ((c + (c >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
                }

                count += c;

            }

            return count;

        }
    
        public static int FindFirstIndex(this BitArray bitArray, bool value = true)
        {
            for (int i = 0; i < bitArray.Count; i++)
            {
                if (bitArray[i] == value)
                {
                    return i;
                }
            }

            throw new IndexOutOfRangeException("Couldn't find first index");
        }
        /**
         * Efficient random index with value
         */
        public static int FindRandomIndex(this BitArray bitArray, int count, Random random, bool value = true)
        {
            int? lastVar = null;
            var index = random.Next(count);
            for (var i = 0; i < bitArray.Count; i++)
            {
                if (bitArray[i] == value)
                {
                    lastVar = i;
                    index--;
                }

                if (index < 0)
                {
                    break;
                }
            }


            if (lastVar is null || bitArray[lastVar.Value] == value)
            {
                throw new IndexOutOfRangeException($"Failed to find random index with value {value}");
            }

            return lastVar!.Value;
        }

        public static IEnumerable<bool> Iterate(this BitArray bitArray)
        {
            var enumerator = bitArray.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return (bool)enumerator.Current!;
            }
        }
        public static IEnumerable<(bool,int)> IterateWithIndex(this BitArray bitArray)
        {
            var enumerator = bitArray.GetEnumerator();
            var i = 0;
            while (enumerator.MoveNext())
            {
                yield return ((bool)enumerator.Current!,i);
                i++;
            }
        }
    }
}
