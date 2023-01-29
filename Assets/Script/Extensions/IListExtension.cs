using System;
using System.Collections.Generic;

namespace Script.Extensions
{
    public static class IListExtension
    {
        private static Random _random = new Random();  

        // Source https://stackoverflow.com/questions/273313/randomize-a-listt
        // Fisher-Yates shuffle O(N)
        public static void ShuffleInPlace<T>(this IList<T> list, Random random = null)  
        {  
            int n = list.Count;
            var rng = random ?? _random;
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                (list[k], list[n]) = (list[n], list[k]);
            }  
        }
    }
}