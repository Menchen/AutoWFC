using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;


namespace Script
{
    public class TilesetSlicer : MonoBehaviour
    {

        public Texture2D tileSet;

        public string tileSetPath = "generatedTileset";
        public GameObject tilePallet;
        public int pixelPerUnit;




        public void SliceCurrentTileSet()
        {
            if (tileSet is null)
            {
                return;
            }

            var palletTilemap = tilePallet.GetComponentInChildren<Tilemap>();
            tileSet.filterMode = FilterMode.Point;
            // var size = new Vector2(tileWidth, tileHeight);

            var changeDataList = new List<TileChangeData>();

            var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            var tilemap = GetComponent<Tilemap>();
            tilemap.ClearAllTiles();
            tilemap.ClearAllEditorPreviewTiles();
            palletTilemap.ClearAllTiles();
            palletTilemap.ClearAllEditorPreviewTiles();
            AssetDatabase.DeleteAsset($"Assets/{tileSetPath}");
            AssetDatabase.CreateFolder("Assets", tileSetPath);

            int x = 0;
            int y = tileSet.height / pixelPerUnit;
            foreach (var sprite in sprites)
            {

                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;

                TileChangeData data = new TileChangeData()
                {
                    tile = tile,
                    position = new Vector3Int(x, y),
                };

                changeDataList.Add(data);

                x++;
                if (x >= tileSet.width / pixelPerUnit)
                {
                    x = 0;
                    y--;
                }
            }

            var hashArray = TileData<Tile>.PopulateHashArray(changeDataList.ToArray());
            var hashMap = TileData<Tile>.ToHashLookUp(hashArray);
            var tileLookUp = hashMap.ToDictionary(e => e.Key, e => (Tile) e.Value.Tile);

            var relations =
                TileRelationship.PopulateRelationshipMap(hashArray.Select(e=>e.Hash).ToArray(), MaxX, MaxY, pixelPerUnit);

            using (FileStream fs = new FileStream("Assets/Relationship.json",FileMode.Create))
            {
                using (StreamWriter streamWriter = new StreamWriter(fs))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(relations,new JsonSerializerSettings(){ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.Indented}));
                }
                
            }
            

            foreach (var data in hashMap.Values)
            {
                AssetDatabase.CreateAsset(data.Tile, $"Assets/{tileSetPath}/{data.Position.x}_{data.Position.y}.asset");
                var position = new Vector3Int(data.Position.x,data.Position.y,0);
                tilemap.SetEditorPreviewTile(position, data.Tile);
                tilemap.SetTile(position, data.Tile);
                palletTilemap.SetTile(position, data.Tile);
            }
        }

        private int MaxY => tileSet.height / pixelPerUnit;
        private int MaxX => tileSet.width / pixelPerUnit;

    }
    

}
