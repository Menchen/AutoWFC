using System.Linq;
using JetBrains.Annotations;

public static class ArrayUtils
{
    public static int GetVolume(int[] size)
    {
        return size.Aggregate((a, b) => a * b);
    }
    public static bool InBounds(int[] size, int pos)
    {
        return pos >=0 && pos < size.Aggregate((a, b) => a * b) ;
    }
    
    public static bool InBounds(int[] size, int[] pos)
    {
        return pos.All(e=>e>=0) && size.Zip(pos, (a, b) => a - b).All(e => e > 0);
    }
    
    [CanBeNull]
    public static int[] UnRavelIndex(int[] size, int index)
    {
        var prefix = new int[size.Length + 1];
        prefix[0] = 1;

        for (int i = 0; i < size.Length; i++)
        {
            prefix[i+1] = size[i] * prefix[i];
        }

        if (index < 0 || index > prefix[^1])
        {
            return null;
        }
        
        var result = new int[size.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = (index % prefix[i + 1]) / prefix[i];
        }

        return result;
    }
    
    public static int? RavelIndex(int[] size, int[] index)
    {
        var prefix = new int[size.Length + 1];
        prefix[0] = 1;

        for (int i = 0; i < size.Length; i++)
        {
            prefix[i+1] = size[i] * prefix[i];
        }

        var result = 0;
        for (int i = 0; i < index.Length; i++)
        {
            result += prefix[i] * index[i];
        }

        return result;
    }
}

public struct TypedArray<T>
{
    public T[] Value;

    public int Length => Value.Length;

    private TypedArray(T[] value)
    {
        Value = value;
    }

    public static implicit operator T[](TypedArray<T> typedArray)
    {
        return typedArray.Value;
    }
    
    public static implicit operator TypedArray<T>(T[] arr)
    {
        return new TypedArray<T>(arr);
    }

    public T this[int index]
    {
        get => Value[index];
        set => Value[index] = value;
    }
}
