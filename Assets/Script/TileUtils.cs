using UnityEngine;

namespace Script
{
    public static class TileUtils
    {
        public static int MapIndex(Vector2Int index, int width)
        {
            return index.x + index.y * width;
        }
        public static Vector2Int MapIndex(int index,int width)
        {
            return new Vector2Int(index % width, index / width);
        }
        
        public static int MapIndex(int x, int y, int width)
        {
            return x + y * width;
        }
        
        public static T[] GetNeighbors<T>(T[]array ,int x,int y,int maxX, int maxY, int width)
        {
            var result = new T[4];
            if (y+1<maxY)
            {
                result[Up] = (array[MapIndex(x,y+1,width)]);
            }
            if (x+1<maxX)
            {
                result[Right] = (array[MapIndex(x+1,y,width)]);
            }
            if (y-1>=0)
            {
                result[Down] = (array[MapIndex(x,y-1,width)]);
            }
            if (x-1>=0)
            {
                result[Left] = (array[MapIndex(x-1,y,width)]);
            }

            return result;
        }

        public static readonly int[] Dirs = {Up, Right, Down, Left};
        public const int Up = 0;
        public const int Right = 1;
        public const int Down = 2;
        public const int Left = 3;
    }
}