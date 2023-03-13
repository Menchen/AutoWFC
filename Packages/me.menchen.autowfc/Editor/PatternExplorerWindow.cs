using System;
using System.Collections.Generic;
using AutoWfc.Editor.Components;
using AutoWfc.GenericUtils;
using AutoWfc.Wfc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AutoWfc.Editor
{
    public class PatternExplorerWindow : EditorWindow
    {
        public static WfcHelper Current;

        [MenuItem("Window/Pattern Explorer")]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            PatternExplorerWindow window =
                (PatternExplorerWindow)GetWindow(typeof(PatternExplorerWindow), false, "Pattern Explorer");
            window.Show();
        }

        private string _cachedString;
        private List<WfcUtils<string>.Pattern> _patterns;
        private Vector2 _patternScroll;
        private Dictionary<string, Texture> _spriteLookUp = new();

        protected void OnGUI()
        {
            if (Current == null)
            {
                ShowCenterLabel("No WfcHelper found, try to select an instance in hierarchy or open it in inspector");
                return;
            }

            var jsonChanged = UpdateCachedJson();
            if (jsonChanged || _patterns is null)
            {
                _patterns = JsonConvert.DeserializeObject<JObject>(_cachedString)["Patterns"]
                    ?.ToObject<List<WfcUtils<string>.Pattern>>();
            }

            if (_patterns is null)
            {
                ShowCenterLabel("Failed to read pattern");
                return;
            }

            using (DisposableGUILayout.CreateScrollView(ref _patternScroll))
            {
                var y = 0;
                foreach (var pattern in _patterns)
                {
                    // EditorGUI.DrawPreviewTexture(new Rect(0,y,64,64),GetTexture(pattern.Value));
                    var texture = GetTexture(pattern.Value);
                    // texture.height = 64;
                    // texture.width = 64;
                    // EditorGUIUtility.
                    // GUIContent content = new GUIContent(texture, pattern.Value);
                    if (DrawIconButton(GetTexture(pattern.Value)))
                    {
                        Debug.Log(pattern.Value);
                    }

                    // EditorGUILayout.ObjectField(GetSprite(pattern.Value), typeof(Sprite), false);
                    // GUILayout.Label(pattern.Value);
                    y += 70;
                }
            }
        }

        protected bool DrawIconButton(Texture texture)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(4, 4, 4, 4),
                fixedHeight = 64,
                fixedWidth = 64,
                padding = new RectOffset(4,4,4,4),
                stretchWidth = false,
                stretchHeight = false,
            };
            Rect rect = GUILayoutUtility.GetRect(64, 64,64,64,buttonStyle);
            // GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            // var click = GUI.Button(rect, "",buttonStyle );
            
            // if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            // {
                // return true;
                // Handle button click
            // }

            // return false;
            var clicked = new ImageButton() {Rect = rect,Texture = texture,GUIStyle = buttonStyle}.Draw();






            return clicked;
        }

        protected Texture GetTexture(string hash)
        {
            if (_spriteLookUp.TryGetValue(hash, out var texture))
            {
                return texture;
            }

            texture = AssetPreview.GetAssetPreview(Resources.Load<Tile>(hash).sprite);
            texture.filterMode = FilterMode.Point;
            // ((Texture2D)texture).
            _spriteLookUp[hash] = texture;
            return texture;
        }


        protected bool UpdateCachedJson()
        {
            var same = Equals(_cachedString, Current.serializedJson);

            _cachedString = Current.serializedJson;
            return !same;
        }

        private static void ShowCenterLabel(string text)
        {
            // Center in both axis
            using (DisposableGUILayout.CreateHorizontal)
            {
                GUILayout.FlexibleSpace();
                using (DisposableGUILayout.CreateVertical)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(text);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}