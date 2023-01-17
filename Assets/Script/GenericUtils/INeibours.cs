using System.Collections.Generic;

namespace Script.GenericUtils
{
    public interface INeibours
    {
        int Length { get; }
        IList<int[]> Neighbours { get; }

        // bool TrySet(T[] array, int[] index, int[] offset, T element);
        // bool TryGet(T[] array, int[] index, int[] offset, out T element);
    }
}