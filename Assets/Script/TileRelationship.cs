using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Script
{
    
    [Serializable]
    public class TileRelationship 
    {
        public const int Up = 0;
        public const int Right = 1;
        public const int Down = 2;
        public const int Left = 3;

        public string Hash { get; set; }

        public Dictionary<string,int>[] Rules { get; }

        public TileRelationship()
        {
            Rules = new[] {new Dictionary<string, int>(),new Dictionary<string,int>(),new Dictionary<string,int>(),new Dictionary<string,int>()};
        }

        public static Dictionary<string, TileRelationship> PopulateRelationshipMap(string[] hashArray,
            int maxX, int maxY, int pixelPerUnit)
        {
            var result = new Dictionary<string, TileRelationship>();

            for (int x = 0; x < maxX; x++)
            {
                for (int y = 0; y < maxY; y++)
                {
                    var hash = hashArray[TileUtils.MapIndex(x, y,pixelPerUnit)];
                    if (!result.TryGetValue(hash, out var relationship))
                    {
                        relationship = new TileRelationship(){ Hash = hash};
                    }

                    var neighbors = TileUtils.GetNeighbors(hashArray, x, y,maxX,maxY);

                    for (int i = 0; i < neighbors.Length; i++)
                    {
                        if (string.IsNullOrEmpty(neighbors[i]))
                        {
                            continue;
                        }

                        var weight = relationship.Rules[i].GetValueOrDefault(neighbors[i]);
                        relationship.Rules[i].TryAdd(neighbors[i],weight+1);
                    }

                    result[hash] = relationship;
                }
            }

            return result;
        }

    }
}