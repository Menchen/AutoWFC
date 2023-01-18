using System.Collections.Generic;
using Newtonsoft.Json;

namespace Script.GenericUtils
{
    public class INeibours
    {
        public int Length => Neighbours.Count;
        [JsonProperty]
        public virtual IList<int[]> Neighbours { get; set; }


        // bool TrySet(T[] array, int[] index, int[] offset, T element);
        // bool TryGet(T[] array, int[] index, int[] offset, out T element);
    }
}