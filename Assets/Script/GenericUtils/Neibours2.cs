using System;
using System.Collections.Generic;
using System.Linq;

namespace Script.GenericUtils
{
    public class Neibours2: INeibours
    {
        
        public override IList<int[]> Neighbours => new int[][]
        {
            new []{-1,0},
            new []{1,0},
            new []{0,-1},
            new []{0,1},
        };
        private const int Rank = 2;
    }
}