using System;
using System.Collections.Generic;
using System.Linq;

namespace Script.GenericUtils
{
    public class Neibours2<T>: INeibours<T>
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

        public bool TrySet(T[] array, int[] index, int[] offset, T element)
        {
            if (index.Length != offset.Length)
            {
                throw new ArgumentException("Invalid index or offset");
            }
            if (array.Rank != Rank)
            {
                throw new ArgumentException("Not 2D Array");
            }
            if (offset.Length != Rank)
            {
                throw new ArgumentException("Offset length mismatch");
            }

            var vec = index.Zip(offset, (x, y) => x + y).ToArray();
            for (int i = 0; i < Rank; i++)
            {
                if (vec[i] < array.GetLowerBound(i) || vec[i] > array.GetUpperBound(i))
                {
                    return false;
                }
            }

            array.SetValue(element,vec);
            return true;
        }

        public bool TryGet(T[] array, int[] index, int[] offset,out T element)
        {
            if (index.Length != offset.Length)
            {
                throw new ArgumentException("Invalid index or offset");
            }
            if (array.Rank != Rank)
            {
                throw new ArgumentException("Not 2D Array");
            }
            if (offset.Length != Rank)
            {
                throw new ArgumentException("Offset length mismatch");
            }
            
            var vec = index.Zip(offset, (x, y) => x + y).ToArray();
            for (int i = 0; i < Rank; i++)
            {
                if (vec[i] < array.GetLowerBound(i) || vec[i] > array.GetUpperBound(i))
                {
                    element = default;
                    return false;
                }
            }

            element = (T) array.GetValue(vec);
            return true;
        }
    }
}