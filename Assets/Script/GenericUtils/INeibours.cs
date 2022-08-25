using System.Collections.Generic;

namespace Script.GenericUtils
{
    public interface INeibours<T>
    {
        int Length { get; }
        IEnumerable<int[]> Neighbours { get; }

        bool TrySet(T[] array, int[] index, int[] offset, T element);
        bool TryGet(T[] array, int[] index, int[] offset, out T element);
    }
}