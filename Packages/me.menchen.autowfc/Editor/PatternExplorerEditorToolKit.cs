using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoWfc.Editor.UIToolKit;
using AutoWfc.Extensions;
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
        
        public enum Direction
        {
            Left,Right,Up,Down
        }

        public static WfcHelper Current;

        private VisualTreeAsset _baseUiTree;

        private Dictionary<string, Sprite> _spriteLookUp = new();
        private List<WfcUtils<string>.Pattern> _patterns;

        private string _centerValue;
        private WfcUtils<string>.Pattern CenterPattern => _patterns.FirstOrDefault(e => e.Value == _centerValue);

        private Button _previewUp;
        private Button _previewDown;
        private Button _previewLeft;
        private Button _previewRight;
        private Button _previewCenter;


        private TemplateContainer _root;
        private VisualElement _ghost;
        private ScrollView _scroll;

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            if (Current == null || string.IsNullOrEmpty(Current.serializedJson))
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
                flex.Add(new Button(CreateGUI) { text = "Refresh" });
                rootVisualElement.Add(flex);
                return;
            }

            _cachedJson = Current.serializedJson;

            _baseUiTree =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Packages/me.menchen.autowfc/Editor/UIToolKit/PatternExplorer.uxml");
            _patterns = JsonConvert.DeserializeObject<JObject>(_cachedJson)["Patterns"]
                ?.ToObject<List<WfcUtils<string>.Pattern>>();
            _root = _baseUiTree.Instantiate();
            rootVisualElement.Add(_root);
            _previewUp = _root.Q<Button>("UP");
            _previewLeft = _root.Q<Button>("LEFT");
            _previewRight = _root.Q<Button>("RIGHT");
            _previewDown = _root.Q<Button>("DOWN");
            _previewCenter = _root.Q<Button>("MIDDLE");
            _ghost = _root.Q("Ghost");
            _scroll = _root.Q<ScrollView>("SCROLL");
            _whiteListPattern = _patterns!.Select(e => e.Value).ToHashSet();
            AddPreviewsButtonsHandler(NavigationButtonHandler);
            UpdatePreviewButtons("Click to show neighbours");
            _previewCenter.text = "Click to show all pattern";
            GeneratePatternList(_patterns, _root, _scroll, _ghost);
        }

        private void AddPreviewsButtonsHandler(Action<Button> handler)
        {
            _previewCenter.clicked += () => handler(_previewCenter);
            _previewLeft.clicked += () => handler(_previewLeft);
            _previewRight.clicked += () => handler(_previewRight);
            _previewUp.clicked += () => handler(_previewUp);
            _previewDown.clicked += () => handler(_previewDown);
        }

        private string _lastHoverText;

        private void GeneratePatternList(List<WfcUtils<string>.Pattern> patterns, TemplateContainer root,
            ScrollView scroll, VisualElement ghost)
        {
            var patternList = root.Q("PatternList");
            patternList.Clear();
            // var dragAndDropManipulator = new DragAndDropManipulator(root.Q("Ghost"),rootVisualElement);
            patterns?.ForEach(e =>
            {
                var obj = new VisualElement();
                var isWhiteListed = _whiteListPattern.Contains(e.Value);
                var defaultColor = isWhiteListed ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1);
                obj.style.backgroundImage = new StyleBackground(GetTileSprite(e.Value));
                obj.style.unityBackgroundImageTintColor = defaultColor;
                obj.style.height = 64;
                obj.style.width = 64;
                obj.style.marginBottom = 8;
                obj.style.marginLeft = 8;
                obj.style.marginRight = 8;
                obj.style.marginTop = 8;

                obj.tooltip = GenerateSpriteToolTip(e.Value);
                // obj.style.position = new StyleEnum<Position>(Position.Absolute);
                // var bnt = new Button(() => PatternButtonClicked(e.Value)) { text = e.Value };
                // obj.Add(bnt);
                var dragAndDropManipulator = new DragAndDropManipulator(obj, scroll, ghost);
                dragAndDropManipulator.OnStartDrop += () => UpdatePreviewButtons("Drop here to insert new pattern");
                dragAndDropManipulator.OnStartDrop += () =>
                    obj.style.unityBackgroundImageTintColor =
                        isWhiteListed ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.1f, 0.1f, 0.1f, 1);
                dragAndDropManipulator.OnStartDrop += () => _previewCenter.text = "Drop here to switch pattern";
                dragAndDropManipulator.OnEndDrop += () => _previewCenter.text = "Click to show all pattern";
                dragAndDropManipulator.OnEndDrop += () => UpdatePreviewButtons("Click to show neighbours");
                dragAndDropManipulator.OnEndDrop += () =>
                    obj.style.unityBackgroundImageTintColor = defaultColor;
                dragAndDropManipulator.OnDropped += element => DroppedHandler(e.Value, element);

                dragAndDropManipulator.OnStartSlotHover += element =>
                {
                    if (element is Button btn)
                    {
                        _lastHoverText = btn.text;
                        btn.text = "DROP HERE";
                    }
                };
                dragAndDropManipulator.OnEndSlotHover += element =>
                {
                    if (element is Button btn)
                    {
                        btn.text = _lastHoverText;
                        _lastHoverText = null;
                    }
                };
                patternList.Add(obj);
            });
        }

        private void DroppedHandler(string pattern, VisualElement slot)
        {
            if (slot.name == _previewCenter.name)
            {
                UpdateCenterValue(pattern);
                return;
            }
            // TODO Handle neighbours drop A.K.A Add to pattern
            var dir = GetDirectionFromButtonName(slot.name);
            AddPatternWithDir(_centerValue,dir,pattern);
            
        }

        private void AddPatternWithDir(string center, Direction dir, string pattern)
        {
            var centerPattern = _patterns.First(e => e.Value == center);
            var patternId = _patterns.First(e => e.Value == pattern).Id;
            centerPattern.Valid[(int)dir][patternId] = true;
            Save();
        }

        private void Save()
        {
            var jObj = JsonConvert.DeserializeObject<JObject>(_cachedJson);
            jObj["Patterns"] = JToken.FromObject(_patterns);
            Undo.RecordObject(Current,"Pattern Explorer edit");
            Current.serializedJson = jObj.ToString();
            EditorUtility.SetDirty(Current);
        }

        private Button GetButtonFromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return _previewLeft;
                case Direction.Right:
                    return _previewRight;
                case Direction.Up:
                    return _previewUp;
                case Direction.Down:
                    return _previewDown;
                default:
                    throw new ArgumentException("Unknown Direction");
            }
        }
        private Direction GetDirectionFromButtonName(string btnName)
        {
            if (btnName == _previewLeft.name)
            {
                return Direction.Left;
            }

            if (btnName == _previewUp.name)
            {
                return Direction.Up;
            }

            if (btnName == _previewRight.name)
            {
                return Direction.Right;
            }
            if (btnName == _previewDown.name)
            {
                return Direction.Down;
            }

            throw new ArgumentException("Not preview button");
        }

        private void UpdateCenterValue(string pattern)
        {
            _centerValue = pattern;
            _previewCenter.style.backgroundImage = new StyleBackground(_spriteLookUp[pattern]);
            var p = _patterns.First(e => e.Value == pattern);
            PopulateWhiteList(p.Valid);
            GeneratePatternList(_patterns, _root, _scroll, _ghost);
        }


        private void UpdatePreviewButtons(string text)
        {
            // _previewCenter.style.backgroundImage = new StyleBackground(GetTileSprite(_patterns.Last().Value));
            _previewDown.text = text;
            _previewLeft.text = text;
            _previewRight.text = text;
            _previewUp.text = text;
        }

        private string GenerateSpriteToolTip(string pattern)
        {
            if (_centerValue is null || CenterPattern is null)
            {
                return pattern;
            }

            var patternId = _patterns.First(e => e.Value == pattern).Id;
            var array = CenterPattern.Valid.Select(e => e[patternId]).ToArray();
            var sb = new StringBuilder(pattern.Length + 8);
            if (array[0])
            {
                sb.Append("W");
            }

            if (array[1])
            {
                sb.Append("E");
            }

            if (array[2])
            {
                sb.Append("N");
            }

            if (array[3])
            {
                sb.Append("S");
            }

            if (sb.Length > 0)
            {
                sb.Append(" | ");
            }

            sb.Append(pattern);
            return sb.ToString();
        }

        private void PopulateWhiteList(params BitArray[] bitArrays)
        {
            _whiteListPattern = bitArrays.SelectMany(e => e.IterateWithIndex()).Where(e => e.Item1)
                .Select(e => _patterns.First(f => f.Id == e.Item2).Value).ToHashSet();
        }

        private HashSet<string> _whiteListPattern = new();

        private void NavigationButtonHandler(Button btn)
        {
            var p = _patterns.FirstOrDefault(e => e.Value == _centerValue);
            if (p is null)
            {
                return;
            }

            if (btn.name == _previewCenter.name)
            {
                // Reset 
                UpdateCenterValue(_centerValue);
                return;
            }


            // LEFT
            // RIGHT
            // UP
            // DOWN
            if (btn.name == _previewLeft.name)
            {
                PopulateWhiteList(p.Valid[1], p.Valid[2], p.Valid[3]);
                GeneratePatternList(_patterns, _root, _scroll, _ghost);
            }
            else if (btn.name == _previewUp.name)
            {
                PopulateWhiteList(p.Valid[1], p.Valid[0], p.Valid[3]);
                GeneratePatternList(_patterns, _root, _scroll, _ghost);
            }
            else if (btn.name == _previewRight.name)
            {
                PopulateWhiteList(p.Valid[0], p.Valid[2], p.Valid[3]);
                GeneratePatternList(_patterns, _root, _scroll, _ghost);
            }
            else if (btn.name == _previewDown.name)
            {
                PopulateWhiteList(p.Valid[1], p.Valid[2], p.Valid[0]);
                GeneratePatternList(_patterns, _root, _scroll, _ghost);
            }
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
            var json = Current == null ? null : Current.serializedJson;
            if (json != _cachedJson)
            {
                _cachedJson = json;
                CreateGUI();
            }
        }
    }
}