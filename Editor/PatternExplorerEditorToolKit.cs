using System;
using System.Collections.Generic;
using System.Linq;
using AutoWfc.Editor.UIToolKit;
using AutoWfc.Wfc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace AutoWfc.Editor
{
    public class PatternExplorerEditorToolKit : EditorWindow
    {

        [MenuItem("Tools/PatternToolKit")]
        public static void Init()
        {
            EditorWindow wdn = GetWindow<PatternExplorerEditorToolKit>();
            wdn.titleContent = new GUIContent("ToolKit Pattern");
            wdn.Show(true);
        }

        public static WfcHelper Current;

        private VisualTreeAsset _baseUiTree;

        private Dictionary<string, Sprite> _spriteLookUp = new();
        private List<WfcUtils<string>.Pattern> _patterns;

        private Button _previewUp;
        private Button _previewDown;
        private Button _previewLeft;
        private Button _previewRight;
        private Button _previewCenter;
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            if (Current == null || string.IsNullOrEmpty(Current.serializedJson)  )
            {
                var flex = new VisualElement()
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column),
                        alignItems = new StyleEnum<Align>(Align.Center),
                        alignContent = new StyleEnum<Align>(Align.Center),
                    }
                };
                
                flex.Add(new Label("Missing"));
                flex.Add(new Button(CreateGUI){text = "Refresh"});
                rootVisualElement.Add(flex);
                return;
            }
            _cachedJson = Current.serializedJson;

            _baseUiTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/me.menchen.autowfc/Editor/UIToolKit/PatternExplorer.uxml");
            _patterns = JsonConvert.DeserializeObject<JObject>(_cachedJson)["Patterns"]
                ?.ToObject<List<WfcUtils<string>.Pattern>>();
            var root = _baseUiTree.Instantiate();
            rootVisualElement.Add(root);
            _previewUp= root.Q<Button>("UP");
            _previewLeft= root.Q<Button>("LEFT");
            _previewRight= root.Q<Button>("RIGHT");
            _previewDown= root.Q<Button>("DOWN");
            _previewCenter= root.Q<Button>("MIDDLE");
            var ghost = root.Q("Ghost");
            var scroll = root.Q<ScrollView>("SCROLL");
            UpdatePreviewButtons("Click to show neighbours");
            GeneratePatternList(root, scroll, ghost);
        }

        private void GeneratePatternList(TemplateContainer root, ScrollView scroll, VisualElement ghost)
        {
            var patternList = root.Q("PatternList");
            patternList.Clear();
            // var dragAndDropManipulator = new DragAndDropManipulator(root.Q("Ghost"),rootVisualElement);
            _patterns?.ForEach(e =>
            {
                var obj = new VisualElement();
                obj.style.backgroundImage = new StyleBackground(GetTileSprite(e.Value));
                obj.style.height = 64;
                obj.style.width = 64;
                obj.style.marginBottom = 8;
                obj.style.marginLeft = 8;
                obj.style.marginRight = 8;
                obj.style.marginTop = 8;
                obj.tooltip = e.Value;
                // obj.style.position = new StyleEnum<Position>(Position.Absolute);
                // var bnt = new Button(() => PatternButtonClicked(e.Value)) { text = e.Value };
                // obj.Add(bnt);
                var dragAndDropManipulator = new DragAndDropManipulator(obj, scroll, ghost);
                dragAndDropManipulator.OnStartDrop += () => UpdatePreviewButtons("Drop here to insert new pattern");
                dragAndDropManipulator.OnEndDrop += () => UpdatePreviewButtons("Click to show neighbours");
                dragAndDropManipulator.OnDropped += element => Debug.Log(element.name);
                patternList.Add(obj);
            });
        }
        

        private void UpdatePreviewButtons(string text)
        {
            // _previewCenter.style.backgroundImage = new StyleBackground(GetTileSprite(_patterns.Last().Value));
            _previewDown.text = text;
            _previewLeft.text = text;
            _previewRight.text = text;
            _previewUp.text = text;

        }

        private void PatternButtonClicked(string hash)
        {
            Debug.Log(hash);
        }

        protected Sprite GetTileSprite(string hash)
        {
            if (_spriteLookUp.TryGetValue(hash, out var texture))
            {
                return texture;
            }

            texture = Resources.Load<Tile>(hash).sprite;
            // texture.filterMode = FilterMode.Point;
            // ((Texture2D)texture).
            _spriteLookUp[hash] = texture;
            return texture;
        }
        private string _cachedJson;

        public void OnGUI()
        {
            if ((Current == null && _cachedJson != null ) || Current.serializedJson != _cachedJson)
            {
                _cachedJson = Current.serializedJson;
                CreateGUI();
            }
        }
    }
}