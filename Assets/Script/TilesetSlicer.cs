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

        public FolderReference folderReference;
        public BoundsInt? CurrentSelection;
        public Texture2D tileSet;

        [FormerlySerializedAs("ColorScheme")] [SerializeField]
        private Style colorScheme;

        
        [TextArea(15,20)]
        public string serializedJson;

        [Serializable]
        private class Style
        {
            [FormerlySerializedAs("HoverColor")] public Color hoverColor = Color.white;
            [FormerlySerializedAs("ClickColor")] public Color clickColor = Color.yellow;
            [FormerlySerializedAs("SelectColor")] public Color selectColor = new Color(255/255f, 165/255f, 0f);
        }

        public void WfcWithJson(BoundsInt bounds)
        {
            // var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            // var lookup = sprites.GroupBy(HashSprite).ToDictionary(e => e.Key, e => e.First());
            var tileLookup = new Dictionary<string, TileBase>();

            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            var outputVec = new[] { bounds.size.x, bounds.size.y };
            var offset = bounds.position;
            
            
            
            var tilemap = GetComponent<Tilemap>();
            var inputTiles = GetTilesFromTilemap(bounds, tilemap, out var inputVec);
            var retry = 5;
            string[] output = new string[] { };
            while (retry > 0)
            {
                var colaped = wfc.Collapse(outputVec,out output,inputTiles);
                if (colaped)
                {
                    break;
                }

                retry--;
            }

            if (retry <= 0)
            {
                Debug.Log($"Failed to WFC");
                return;
            }

            // tilemap.ClearAllTiles();
            tilemap.ClearAllEditorPreviewTiles();

            for (int i = 0; i < output.Length; i++)
            {
                var pos = ArrayUtils.UnRavelIndex(outputVec, i);
                if (!tileLookup.ContainsKey(output[i]))
                {
#if UNITY_EDITOR
                    tileLookup[output[i]] =
                        AssetDatabase.LoadAssetAtPath<TileBase>(folderReference.Path + $"/{output[i]}.asset");
#else
                    // Tiles must be inside of Resources folder
                    tileLookup[output[i]] = Resources.Load<Tile>(output[i]);
#endif
                }
                var tile = tileLookup[output[i]];
                
                // tile.sprite = output[i] is null ? null : lookup[output[i]];
                var unityIndex = ToUnityIndex(pos[0], pos[1], outputVec[0], outputVec[1]);
                // tilemap.SetEditorPreviewTile(offset+new Vector3Int(unityIndex.x, unityIndex.y), tile);
                
                // Use SetTiles ? Seems more efficient.
                tilemap.SetTile(offset+new Vector3Int(unityIndex.x, unityIndex.y), tile);
            }
            EditorUtility.SetDirty(tilemap);
        }

        public void LearnPatternFromRegion(BoundsInt bounds)
        {
            
            var tilemap = GetComponent<Tilemap>();
            var inputTiles = GetTilesFromTilemap(bounds, tilemap, out var inputVec);

            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            wfc.LearnNewPattern(inputVec,inputTiles);
            serializedJson = wfc.SerializeToJson();
        }
        
        public void UnLearnPatternFromRegion(BoundsInt bounds)
        {
            var tilemap = GetComponent<Tilemap>();
            var inputTiles = GetTilesFromTilemap(bounds, tilemap, out var inputVec);

            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            wfc.UnLearnPattern(inputVec,inputTiles);
            serializedJson = wfc.SerializeToJson();
        }

        public string[] GetTilesFromTilemap(BoundsInt bounds, Tilemap tilemap, out int[] inputVec)
        {
            bool saveToFolder = folderReference != null;
            inputVec = new[] { bounds.size.x, bounds.size.y };
            var inputLength = inputVec.Aggregate((acc, e) => acc * e);
            var inputTiles = new string[inputLength];
            for (int i = 0; i < inputLength; i++)
            {
                var pos = ArrayUtils.UnRavelIndex(inputVec, i);
                var unityIndex = ToUnityIndex(pos[0], pos[1], inputVec[0], inputVec[1]);

                var offset = bounds.position;
                try
                {
                    var tile = tilemap.GetTile<Tile>(offset + new Vector3Int(unityIndex.x, unityIndex.y));
                    if (tile == null)
                    {
                        // Empty
                        continue;
                    }
                    
                    inputTiles[i] = HashSprite(tile.sprite);

                    if (saveToFolder)
                    {
                        var fileExist = File.Exists(folderReference.Path + $"/{inputTiles[i]}.asset");
                        if (!fileExist)
                        {
                            AssetDatabase.CreateAsset(tile,folderReference.Path+$"/{inputTiles[i]}.asset");
                        }
                    }


                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"Failed to get Tile from {offset + new Vector3Int(unityIndex.x, unityIndex.y)}, SKIPPED!\n{e}\n{e.StackTrace}");
                }
            }

            return inputTiles;
        }

        public void CreateWfcFromTileSet()
        {
            bool saveToFolder = folderReference != null;
            tileSet.filterMode = FilterMode.Point;
            
            var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            var pixelPerUnit = sprites[0].pixelsPerUnit;
            
            int maxY = Mathf.FloorToInt(tileSet.height / pixelPerUnit);
            int maxX = Mathf.FloorToInt(tileSet.width / pixelPerUnit);

            var sizeInput = new[] {maxX, sprites.Length/maxX};

            var hashedSpriteInput = sprites.Select(HashSprite).ToArray();
            for (int i = 0; i < hashedSpriteInput.Length; i++)
            {
                if (saveToFolder)
                {
                    var fileExist = File.Exists(folderReference.Path + $"/{hashedSpriteInput[i]}.asset");
                    if (!fileExist)
                    {
                        var tile = ScriptableObject.CreateInstance<Tile>();
                        tile.name = hashedSpriteInput[i];
                        tile.sprite = sprites[i];
                        AssetDatabase.CreateAsset(tile,folderReference.Path+$"/{hashedSpriteInput[i]}.asset");
                    }
                }
                
            }

            var wfc = new WfcUtils<string>(2,sizeInput,hashedSpriteInput,BorderBehavior.Wrap,new System.Random(DateTime.Now.Millisecond),new Neibours2(),null,WfcUtils<string>.NextCell.NextCellEnum.MinState,WfcUtils<string>.SelectPattern.SelectPatternEnum.PatternUniform);
            serializedJson = wfc.SerializeToJson();
        }
        
        public void CreateFromSelection(BoundsInt boundsInt)
        {
            var input = GetTilesFromTilemap(boundsInt, GetComponent<Tilemap>(), out var inputVec);
            // var hashedSpriteInput = sprites.Select(HashSprite).ToArray();

            var wfc = new WfcUtils<string>(2,inputVec,input,BorderBehavior.Wrap,new System.Random(DateTime.Now.Millisecond),new Neibours2(),null,WfcUtils<string>.NextCell.NextCellEnum.MinState,WfcUtils<string>.SelectPattern.SelectPatternEnum.PatternUniform);
            serializedJson = wfc.SerializeToJson();
        }

        private Vector2Int ToUnityIndex(int x, int y, int w, int h)
        {
            // return new Vector2Int(x, y);
            return new Vector2Int(x, h - y-1);
        }


        public string HashSprite(Sprite value)
        {
            var hash128 = new Hash128();
            var x = Mathf.FloorToInt(value.rect.x);
            var y = Mathf.FloorToInt(value.rect.y);
            var w = Mathf.FloorToInt(value.rect.width);
            var h = Mathf.FloorToInt(value.rect.height);
            hash128.Append(value.texture.GetPixels(x, y, w, h));
            return hash128.ToString();
        }

        public void ReHashSpriteFolder(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            
            
        }

    }
    

}
