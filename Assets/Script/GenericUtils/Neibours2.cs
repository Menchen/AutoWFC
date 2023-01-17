using System;
using System.Collections.Generic;
using System.Linq;

namespace Script.GenericUtils
{
    public class Neibours2: INeibours
    {
        public int Length => _offset.Length;

        public IList<int[]> Neighbours => _offset;
        
        private const int Rank = 2;
        private readonly int[][] _offset =  {
            new []{-1,0},
            new []{1,0},
            new []{0,-1},
            new []{0,1},
        };

    }
}