using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Script.Converters;
using Script.GenericUtils;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using WFC;


namespace Script
{
    [RequireComponent(typeof(Tilemap))]
    public class TilesetSlicer : MonoBehaviour
    {

        public BoundsInt? CurrentSelection;
        public Texture2D tileSet;
        [FormerlySerializedAs("OutputSize")] public Vector2Int outputSize;

        [FormerlySerializedAs("ColorScheme")] [SerializeField]
        private Style colorScheme;

        
        [TextArea(15,20)]
        public string serializedJson;

        public TextAsset JsonFile;
        
        [Serializable]
        private class Style
        {
            [FormerlySerializedAs("HoverColor")] public Color hoverColor = Color.white;
            [FormerlySerializedAs("ClickColor")] public Color clickColor = Color.yellow;
            [FormerlySerializedAs("SelectColor")] public Color selectColor = new Color(255/255f, 165/255f, 0f);
        }

        public void WfcWithJson()
        {
            
            var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            var lookup = sprites.GroupBy(HashSprite).ToDictionary(e => e.Key, e => e.First());

            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            var outputVec = new[] {outputSize.x, outputSize.y};
            if (JsonFile)
            {
                var json = JsonConvert.SerializeObject(wfc);
                File.WriteAllText(AssetDatabase.GetAssetPath(JsonFile), json);
                // TODO Move to Editor
                EditorUtility.SetDirty(JsonFile);
                
            }
            var colaped = wfc.Collapse(outputVec,out var output);

            var tilemap = GetComponent<Tilemap>();
            tilemap.ClearAllTiles();
            tilemap.ClearAllEditorPreviewTiles();

            for (int i = 0; i < output.Length; i++)
            {
                var pos = ArrayUtils.UnRavelIndex(outputVec, i);
                var tile = ScriptableObject.CreateInstance<Tile>();
                
                tile.sprite = output[i] is null ? null : lookup[output[i]];
                var unityIndex = ToUnityIndex(pos[0], pos[1], outputVec[0], outputVec[1]);
                tilemap.SetEditorPreviewTile(new Vector3Int(unityIndex.x, unityIndex.y), tile);
                tilemap.SetTile(new Vector3Int(unityIndex.x, unityIndex.y), tile);
            }
        }

        public void WfcThis()
        {
            if (tileSet is null || outputSize == Vector2Int.zero)
            {
                return;
            }
            
            tileSet.filterMode = FilterMode.Point;
            
            var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            var pixelPerUnit = sprites[0].pixelsPerUnit;
            
            
            int maxY = Mathf.FloorToInt(tileSet.height / pixelPerUnit);
            int maxX = Mathf.FloorToInt(tileSet.width / pixelPerUnit);
            
            var lookup = sprites.GroupBy(HashSprite).ToDictionary(e => e.Key, e => e.First());
            var hashedSpriteInput = new string[sprites.Length];
            // var hashedSpriteInput = sprites.Select(HashSprite).ToArray();
            
            var sizeInput = new[] {maxX, sprites.Length/maxX};
            int x = 0;
            int y = maxY-1;

            hashedSpriteInput = sprites.Select(HashSprite).ToArray();
            // string t = null;
            // foreach (var sprite in sprites)
            // {
            //     var index = ArrayUtils.RavelIndex(sizeInput, new[] {x, y}).Value;
            //     hashedSpriteInput[index] = HashSprite(sprite);
            //     if (x==9 && y == 19)
            //     {
            //         t = HashSprite(sprite);
            //     }
            //     
            //     x++;
            //     if (x >= MaxX)
            //     {
            //         x = 0;
            //         y--;
            //     }
            // }
            var preset = new string[sprites.Length];
            // preset[sprites.Length / 2] = t;
            var ba = new BitArray(32,false);

//var q = (bool[]) ba;
            var jsonx = JsonConvert.SerializeObject(ba, new BitArrayConverter());
            Console.WriteLine(jsonx);
            var obj = JsonConvert.DeserializeObject<BitArray>(jsonx,new BitArrayConverter());
            Console.WriteLine(obj);

            var outputVec = new[] {outputSize.x, outputSize.y};
            var wfc = new WfcUtils<string>(2,3,sprites.Length,sizeInput,hashedSpriteInput,BorderBehavior.Wrap,new System.Random(DateTime.Now.Millisecond),0,new Neibours2(),null,WfcUtils<string>.NextCell.NextCellEnum.MinEntropy,WfcUtils<string>.SelectPattern.SelectPatternEnum.PatternUniform);
            serializedJson = wfc.SerializeToJson();
            if (JsonFile)
            {
                var json = JsonConvert.SerializeObject(wfc);
                File.WriteAllText(AssetDatabase.GetAssetPath(JsonFile), json);
                // TODO Move to Editor
                EditorUtility.SetDirty(JsonFile);
                
            }
            var colaped = wfc.Collapse(outputVec,out var output);

            var tilemap = GetComponent<Tilemap>();
            tilemap.ClearAllTiles();
            tilemap.ClearAllEditorPreviewTiles();

            for (int i = 0; i < output.Length; i++)
            {
                var pos = ArrayUtils.UnRavelIndex(outputVec, i);
                var tile = ScriptableObject.CreateInstance<Tile>();
                
                tile.sprite = output[i] is null ? null : lookup[output[i]];
                var unityIndex = ToUnityIndex(pos[0], pos[1], outputVec[0], outputVec[1]);
                tilemap.SetEditorPreviewTile(new Vector3Int(unityIndex.x, unityIndex.y), tile);
                tilemap.SetTile(new Vector3Int(unityIndex.x, unityIndex.y), tile);
            }
        }

        private Vector2Int ToUnityIndex(int x, int y, int w, int h)
        {
            // return new Vector2Int(x, y);
            return new Vector2Int(x, h - y-1);
        }

        private string HashSprite(Sprite value)
        {
            var hash128 = new Hash128();
            var x = Mathf.FloorToInt(value.rect.x);
            var y = Mathf.FloorToInt(value.rect.y);
            var w = Mathf.FloorToInt(value.rect.width);
            var h = Mathf.FloorToInt(value.rect.height);
            hash128.Append(value.texture.GetPixels(x, y, w, h));
            return hash128.ToString();
        }

        // public void SliceCurrentTileSet()
        // {
        //     if (tileSet is null)
        //     {
        //         return;
        //     }
        //
        //     var palletTilemap = tilePallet.GetComponentInChildren<Tilemap>();
        //     tileSet.filterMode = FilterMode.Point;
        //     // var size = new Vector2(tileWidth, tileHeight);
        //
        //     var changeDataList = new List<TileChangeData>();
        //
        //     var sprites = Resources.LoadAll<Sprite>(tileSet.name);
        //     var tilemap = GetComponent<Tilemap>();
        //     tilemap.ClearAllTiles();
        //     tilemap.ClearAllEditorPreviewTiles();
        //     palletTilemap.ClearAllTiles();
        //     palletTilemap.ClearAllEditorPreviewTiles();
        //     AssetDatabase.DeleteAsset($"Assets/{tileSetPath}");
        //     AssetDatabase.CreateFolder("Assets", tileSetPath);
        //
        //     int x = 0;
        //     int y = MaxY-1;
        //     foreach (var sprite in sprites)
        //     {
        //
        //         Tile tile = ScriptableObject.CreateInstance<Tile>();
        //         tile.sprite = sprite;
        //
        //         TileChangeData data = new TileChangeData()
        //         {
        //             tile = tile,
        //             position = new Vector3Int(x, y),
        //         };
        //
        //         changeDataList.Add(data);
        //
        //         x++;
        //         if (x >= MaxX)
        //         {
        //             x = 0;
        //             y--;
        //         }
        //     }
        //
        //     var hashArray = TileData<Tile>.PopulateHashArray(changeDataList.ToArray());
        //     var hashMap = TileData<Tile>.ToHashLookUp(hashArray);
        //     var tileLookUp = hashMap.ToDictionary(e => e.Key, e => (Tile) e.Value.Tile);
        //
        //     var relations =
        //         TileRelationship.PopulateRelationshipMap(hashArray.Select(e=>e.Hash).ToArray(), MaxX, MaxY, pixelPerUnit);
        //
        //     using (FileStream fs = new FileStream("Assets/Relationship.json",FileMode.Create))
        //     {
        //         using (StreamWriter streamWriter = new StreamWriter(fs))
        //         {
        //             streamWriter.Write(JsonConvert.SerializeObject(relations,new JsonSerializerSettings(){ReferenceLoopHandling = ReferenceLoopHandling.Ignore, Formatting = Formatting.Indented}));
        //         }
        //         
        //     }
        //     
        //
        //     foreach (var data in hashMap.Values)
        //     {
        //         AssetDatabase.CreateAsset(data.Tile, $"Assets/{tileSetPath}/{data.Position.x}_{data.Position.y}.asset");
        //         var position = new Vector3Int(data.Position.x,data.Position.y,0);
        //         tilemap.SetEditorPreviewTile(position, data.Tile);
        //         tilemap.SetTile(position, data.Tile);
        //         palletTilemap.SetTile(position, data.Tile);
        //     }
        //
        //
        //     WFC wfc;
        //     for (int i = 0; i < 5000; i++)
        //     {
        //         
        //         try
        //         {
        //             wfc = new WFC(relations,new Vector2Int(MaxX,MaxY), hashArray);
        //             while (wfc.CollapseNextState())
        //             {
        //                 
        //             }
        //             
        //             wfc.WriteToFile(out var wfcMap);
        //
        //             for (int xx = 0; xx < wfcMap.GetLength(0); xx++)
        //             {
        //                 for (int yy = 0; yy < wfcMap.GetLength(1); yy++)
        //                 {
        //                     if (wfcMap[xx,yy] is not null && tileLookUp.TryGetValue(wfcMap[xx,yy], out var tile))
        //                     {
        //                         tilemap.SetEditorPreviewTile(new Vector3Int(xx,yy),tile);
        //                     }
        //                 }
        //             }
        //             break;
        //
        //         }
        //         catch (InvalidOperationException e)
        //         {
        //             Console.WriteLine(e);
        //         }
        //
        //     }
        //     
        // }

        // private int MaxY => tileSet.height / pixelPerUnit;
        // private int MaxX => tileSet.width / pixelPerUnit;

    }
    

}
