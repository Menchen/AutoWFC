using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Script
{
    public struct TileData<T> where T:TileBase
    {
        public Vector2Int Position;
        public Sprite Sprite
        {
            set
            {
                var hash128 = new Hash128();
                var x = Mathf.FloorToInt(value.rect.x);
                var y = Mathf.FloorToInt(value.rect.y);
                var w = Mathf.FloorToInt(value.rect.width);
                var h = Mathf.FloorToInt(value.rect.height);
                hash128.Append(value.texture.GetPixels(x, y, w, h));
                Hash = hash128.ToString();
            }
        }

        public string Hash
        {
            get;
            private set;
        }

        public T Tile { get; set; }
        
        public static TileData<Tile>[] PopulateHashArray(TileChangeData[] tiles)
        {
            var dataArray = new TileData<Tile>[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                var data = new TileData<Tile>()
                {
                    Position = new Vector2Int(tiles[i].position.x,tiles[i].position.y),
                    Tile = (Tile) tiles[i].tile,
                    Sprite = ((Tile) tiles[i].tile).sprite,
                };
                dataArray[i] = data;

            }
            return dataArray;
        }
        
        public static Dictionary<string, TileData<T>> ToHashLookUp(TileData<T>[] array)
        {
            return array.DistinctBy(e=>e.Hash).ToDictionary(e => e.Hash, e => e);
        }
        
    }
}