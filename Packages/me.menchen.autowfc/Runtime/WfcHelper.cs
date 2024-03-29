using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoWfc.GenericUtils;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using AutoWfc.Wfc;
using Random = System.Random;

namespace AutoWfc
{
    [RequireComponent(typeof(Tilemap))]
    public class WfcHelper : MonoBehaviour
    {
#if UNITY_EDITOR
        [FormerlySerializedAs("folderReference")] [Tooltip("The folder used to save tile for WFC")]
        public FolderReference tileOutputFolder;

        public BoundsInt? CurrentSelection;
        private string ResourceLocation => tileOutputFolder?.Path == null ? null : tileOutputFolder.Path + "/Resources";
        private string RawResourceLocation => tileOutputFolder?.Path;
#else
        private string ResourceLocation => null;
        private string RawResourceLocation => null;
#endif


        [Tooltip("Spritesheet used as tilemap for \"Create new model from tile set\"")]
        public Texture2D tileSet;

        [Tooltip("When creating new WFC Tile, if the same tile is found, move it or clone it?")]
        public bool cloneTileIfNotExist = false;


        [Range(0f, 1f)] [Tooltip("Control randomness")]
        public float mutation = 1f;

        public WfcUtils<string>.SelectPattern.SelectPatternEnum selectPatternEnum;
        public WfcUtils<string>.NextCell.NextCellEnum nextCellEnum;


        [HideInInspector] [TextArea(15, 20)] public string serializedJson;


        public void ApplyWfc(string[] wfc, BoundsInt bounds)
        {
            // var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            // var lookup = sprites.GroupBy(HashSprite).ToDictionary(e => e.Key, e => e.First());
            var tileLookup = new Dictionary<string, TileBase>();
            var isFolderReferenceValid = !string.IsNullOrEmpty(RawResourceLocation);

            var outputVec = new[] { bounds.size.x, bounds.size.y };
            var offset = bounds.position;


            var tilemap = GetComponent<Tilemap>();
            for (int i = 0; i < wfc.Length; i++)
            {
                var pos = ArrayUtils.UnRavelIndex(outputVec, i);
                if (!tileLookup.ContainsKey(wfc[i]))
                {
                    tileLookup[wfc[i]] = Resources.Load<Tile>(wfc[i]);
                }

                var tile = tileLookup[wfc[i]];

                // tile.sprite = output[i] is null ? null : lookup[output[i]];
                var unityIndex = ToUnityIndex(pos[0], pos[1], outputVec[0], outputVec[1]);
                // tilemap.SetEditorPreviewTile(offset+new Vector3Int(unityIndex.x, unityIndex.y), tile);

                // Use SetTiles ? Seems more efficient.
                tilemap.SetTile(offset + new Vector3Int(unityIndex.x, unityIndex.y), tile);
            }
        }


        public string[] GenerateWfc(BoundsInt bounds, string[] preset = null, int maxRetry = 10)
        {
            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            wfc.NextCellEnum = nextCellEnum;
            wfc.SelectPatternEnum = selectPatternEnum;
            wfc.MutationMultiplier = mutation;

            wfc.Logger += Debug.LogWarning;
            var outputVec = new[] { bounds.size.x, bounds.size.y };

            var tilemap = GetComponent<Tilemap>();
            preset ??= GetTilesFromTilemap(bounds, tilemap, out var inputVec);
            var retry = maxRetry > 0 ? maxRetry : 10;
            while (retry > 0)
            {
                try
                {
                    var collapsed = wfc.Collapse(outputVec, out var output, preset);
                    if (collapsed)
                    {
                        return output;
                    }
                }
                catch (Exception e)
                {
                    if (e is not ZeroElementCoefficientException)
                    {
                        throw;
                    }
                }

                retry--;
            }

            Debug.Log("Failed to WFC");
            return null;
        }

        public void SetEmpty(BoundsInt region)
        {
            var tilemap = GetComponent<Tilemap>();
            for (int x = 0; x < region.size.x; x++)
            {
                for (int y = 0; y < region.size.y; y++)
                {
                    tilemap.SetTile(region.position + new Vector3Int(x, y, 0), null);
                }
            }
        }

        public void WfcWithJson(BoundsInt bounds)
        {
            // var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            // var lookup = sprites.GroupBy(HashSprite).ToDictionary(e => e.Key, e => e.First());
            var tileLookup = new Dictionary<string, TileBase>();
            var isFolderReferenceValid = !string.IsNullOrEmpty(RawResourceLocation);

            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            wfc.NextCellEnum = nextCellEnum;
            wfc.SelectPatternEnum = selectPatternEnum;

            wfc.Logger += Debug.LogWarning;
            var outputVec = new[] { bounds.size.x, bounds.size.y };
            var offset = bounds.position;


            var tilemap = GetComponent<Tilemap>();
            var inputTiles = GetTilesFromTilemap(bounds, tilemap, out var inputVec);
            var retry = 10;
            string[] output = { };
            while (retry > 0)
            {
                var colaped = false;
                try
                {
                    colaped = wfc.Collapse(outputVec, out output, inputTiles);
                    if (colaped)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                retry--;
            }

            if (retry <= 0)
            {
                Debug.Log("Failed to WFC");
                return;
            }

            // tilemap.ClearAllTiles();
            // tilemap.ClearAllEditorPreviewTiles();

            for (int i = 0; i < output.Length; i++)
            {
                var pos = ArrayUtils.UnRavelIndex(outputVec, i);
                if (!tileLookup.ContainsKey(output[i]))
                {
#if UNITY_EDITOR
                    tileLookup[output[i]] = isFolderReferenceValid
                        ? AssetDatabase.LoadAssetAtPath<TileBase>(ResourceLocation + $"/{output[i]}.asset")
                        : Resources.Load<Tile>(output[i]);
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
                tilemap.SetTile(offset + new Vector3Int(unityIndex.x, unityIndex.y), tile);
            }
        }

        public void LearnPatternFromRegion(BoundsInt bounds)
        {
            var tilemap = GetComponent<Tilemap>();
            var inputTiles = GetTilesFromTilemap(bounds, tilemap, out var inputVec);

            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            wfc.LearnNewPattern(inputVec, inputTiles);
            serializedJson = wfc.SerializeToJson();
        }

        public void UnLearnPatternFromRegion(BoundsInt bounds)
        {
            var tilemap = GetComponent<Tilemap>();
            var inputTiles = GetTilesFromTilemap(bounds, tilemap, out var inputVec);

            var wfc = WfcUtils<string>.BuildFromJson(serializedJson);
            wfc.UnLearnPattern(inputVec, inputTiles);
            serializedJson = wfc.SerializeToJson();
        }

        public HashSet<Vector3Int> GenerateConflictMap(BoundsInt bounds, string[] before, string[] after,
            out HashSet<Vector3Int> importantEdge)
        {
            var inputVec = new[] { bounds.size.x, bounds.size.y };
            var inputLength = inputVec.Aggregate((acc, e) => acc * e);
            var result = new HashSet<Vector3Int>();
            var offset = bounds.position;
            importantEdge = new HashSet<Vector3Int>();
            for (int i = 0; i < inputLength; i++)
            {
                var pos = ArrayUtils.UnRavelIndex(inputVec, i);
                var unityIndex = ToUnityIndex(pos[0], pos[1], inputVec[0], inputVec[1]);

                if (before[i] is not null && before[i] != after[i])
                {
                    var unityPos = offset + new Vector3Int(unityIndex.x, unityIndex.y);
                    result.Add(unityPos);
                    if (pos[0] == 0 || pos[1] == 0 || pos[0] == inputVec[0] - 1 || pos[1] == inputVec[1] - 1)
                    {
                        importantEdge.Add(unityPos);
                    }
                }
            }

            return result;
        }

        public string[] GetTilesFromTilemap(BoundsInt bounds, Tilemap tilemap, out int[] inputVec)
        {
            bool saveToFolder = !string.IsNullOrEmpty(RawResourceLocation);
            inputVec = new[] { bounds.size.x, bounds.size.y };
            var inputLength = inputVec.Aggregate((acc, e) => acc * e);
            var inputTiles = new string[inputLength];
            var refreshFlag = false;
            try
            {
                for (int i = 0; i < inputLength; i++)
                {
                    var pos = ArrayUtils.UnRavelIndex(inputVec, i);
                    var unityIndex = ToUnityIndex(pos[0], pos[1], inputVec[0], inputVec[1]);

                    var offset = bounds.position;
                    var tile = tilemap.GetTile<Tile>(offset + new Vector3Int(unityIndex.x, unityIndex.y));
                    if (tile == null)
                    {
                        // Empty
                        continue;
                    }

                    inputTiles[i] = HashSprite(tile.sprite);

#if UNITY_EDITOR
                    if (saveToFolder)
                    {
                        var fileExist = File.Exists(ResourceLocation + $"/{inputTiles[i]}.asset");
                        if (!fileExist)
                        {
                            if (!Directory.Exists(ResourceLocation))
                            {
                                AssetDatabase.CreateFolder(
                                    ResourceLocation.Substring(0, ResourceLocation.LastIndexOf('/')), "Resources");
                            }

                            if (!AssetDatabase.Contains(tile))
                            {
                                AssetDatabase.CreateAsset(tile, ResourceLocation + $"/{inputTiles[i]}.asset");
                            }
                            else
                            {
                                if (cloneTileIfNotExist)
                                {
                                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(tile),
                                        ResourceLocation + $"/{inputTiles[i]}.asset");
                                }
                                else
                                {
                                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(tile),
                                        ResourceLocation + $"/{inputTiles[i]}.asset");
                                }
                            }
                        }

                        refreshFlag = true;
                    }
#endif
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"Failed to get Tile, check if output folder is set and try relearn again.");
                throw;
            }

#if UNITY_EDITOR
            if (refreshFlag)
            {
                AssetDatabase.Refresh();
            }
#endif

            return inputTiles;
        }

        public void CreateWfcFromTileSet()
        {
            bool saveToFolder = !string.IsNullOrEmpty(RawResourceLocation);
#if UNITY_EDITOR
            if (!saveToFolder)
            {
                if (!EditorUtility.DisplayDialog("Missing resource folder to save tile to",
                        "WFC might fail if no resource folder is set to save new tile", "Continue", "Cancel"))
                {
                    return;
                }
            }
#endif

            var sprites = Resources.LoadAll<Sprite>(tileSet.name);
            var pixelPerUnit = sprites[0].pixelsPerUnit;
            var refreshFlag = false;

            int maxY = Mathf.FloorToInt(tileSet.height / pixelPerUnit);
            int maxX = Mathf.FloorToInt(tileSet.width / pixelPerUnit);

            var sizeInput = new[] { maxX, sprites.Length / maxX };

            var hashedSpriteInput = sprites.Select(HashSprite).ToArray();
            for (int i = 0; i < hashedSpriteInput.Length; i++)
            {
#if UNITY_EDITOR
                if (saveToFolder)
                {
                    var fileExist = File.Exists(ResourceLocation + $"/{hashedSpriteInput[i]}.asset");
                    if (!fileExist)
                    {
                        var tile = ScriptableObject.CreateInstance<Tile>();
                        tile.name = hashedSpriteInput[i];
                        tile.sprite = sprites[i];
                        if (!Directory.Exists(ResourceLocation))
                        {
                            AssetDatabase.CreateFolder(ResourceLocation.Substring(0, ResourceLocation.LastIndexOf('/')),
                                "Resources");
                        }

                        if (!AssetDatabase.Contains(tile))
                        {
                            AssetDatabase.CreateAsset(tile, ResourceLocation + $"/{hashedSpriteInput[i]}.asset");
                        }
                        else
                        {
                            if (cloneTileIfNotExist)
                            {
                                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(tile),
                                    ResourceLocation + $"/{hashedSpriteInput[i]}.asset");
                            }
                            else
                            {
                                AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(tile),
                                    ResourceLocation + $"/{hashedSpriteInput[i]}.asset");
                            }
                        }

                        refreshFlag = true;
                    }
                }
#endif
            }
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            var wfc = new WfcUtils<string>(sizeInput, hashedSpriteInput, new Random(DateTime.Now.Millisecond),
                new Neibours2(), null, nextCellEnum, selectPatternEnum);
            serializedJson = wfc.SerializeToJson();
        }

        public void CreateFromSelection(BoundsInt boundsInt)
        {
            var input = GetTilesFromTilemap(boundsInt, GetComponent<Tilemap>(), out var inputVec);
            // var hashedSpriteInput = sprites.Select(HashSprite).ToArray();

            var wfc = new WfcUtils<string>(inputVec, input, new Random(DateTime.Now.Millisecond), new Neibours2(), null,
                WfcUtils<string>.NextCell.NextCellEnum.MinState,
                WfcUtils<string>.SelectPattern.SelectPatternEnum.PatternUniform);
            serializedJson = wfc.SerializeToJson();
        }

        private Vector2Int ToUnityIndex(int x, int y, int w, int h)
        {
            // return new Vector2Int(x, y);
            return new Vector2Int(x, h - y - 1);
        }


        public string HashSprite(Sprite value)
        {
            if (!value.texture.isReadable)
            {
#if UNITY_EDITOR
                if (EditorUtility.DisplayDialog("Failed to read texture",
                        $"Failed to read {value.texture.name}, do you wish to reimport with read?", "Reimport",
                        "Cancel"))
                {
                    TextureImporter ti =
                        (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(value.texture));
                    ti.isReadable = true;
                    ti.SaveAndReimport();
                    Debug.LogWarning($"{value.texture.name} was re-imported with write");
                }
                else
                {
                    Debug.LogWarning($"Cannot read {value.texture.name}");
                    throw new InvalidOperationException($"Cannot read {value.texture.name}");
                }
#else
                Debug.LogWarning($"Cannot read {value.texture.name}");
                throw new InvalidOperationException($"Cannot read {value.texture.name}");
#endif
            }

            var hash128 = new Hash128();
            var x = Mathf.FloorToInt(value.rect.x);
            var y = Mathf.FloorToInt(value.rect.y);
            var w = Mathf.FloorToInt(value.rect.width);
            var h = Mathf.FloorToInt(value.rect.height);
            hash128.Append(value.texture.GetPixels(x, y, w, h));
            return hash128.ToString();
        }
    }
}