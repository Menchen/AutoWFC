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

        private enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }

        private readonly IReadOnlyCollection<Direction> _allDirection = new HashSet<Direction>()
            { Direction.Left, Direction.Right, Direction.Up, Direction.Down };

        public static WfcHelper Current;

        private VisualTreeAsset _baseUiTree;

        private HashSet<Direction> _filteredDirection = new();

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

        private VisualElement _prePreview;
        private VisualElement _postPreview;

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
            if (_patterns!.All(e => e.Value != _centerValue))
            {
                _centerValue = null;
            }

            _root = _baseUiTree.Instantiate();
            rootVisualElement.Add(_root);
            _previewUp = _root.Q<Button>("UP");
            _previewLeft = _root.Q<Button>("LEFT");
            _previewRight = _root.Q<Button>("RIGHT");
            _previewDown = _root.Q<Button>("DOWN");
            _previewCenter = _root.Q<Button>("MIDDLE");
            _prePreview = _root.Q<VisualElement>("Pre-Preview");
            _postPreview = _root.Q<VisualElement>("Post-Preview");
            _ghost = _root.Q("Ghost");
            _scroll = _root.Q<ScrollView>("SCROLL");
            AddPreviewsButtonsHandler(NavigationButtonHandler);
            UpdatePreviewButtonsText("Click to show neighbours");
            _previewCenter.text = "Click to show all pattern";
            Refresh();
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

        private void UpdatePreviewsButtons()
        {
            foreach (var direction in _allDirection)
            {
                var btn = GetButtonFromDirection(direction);
                btn.style.backgroundColor =
                    _filteredDirection.Contains(direction)
                        ? new Color(184 / 255f, 88 / 255f, 88 / 255f, 1f)
                        : new Color(88 / 255f, 88 / 255f, 88 / 255f, 1f);
            }

            if (_centerValue == null)
            {
                _previewCenter.style.backgroundImage = new StyleBackground();
            }
            else
            {
                _previewCenter.style.backgroundImage = new StyleBackground(GetTileSprite(_centerValue));
            }
        }

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
                dragAndDropManipulator.OnStartDrop += () => UpdatePreviewButtonsText("Drop here to insert new pattern");
                dragAndDropManipulator.OnStartDrop += () =>
                    obj.style.unityBackgroundImageTintColor =
                        isWhiteListed ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.1f, 0.1f, 0.1f, 1);
                dragAndDropManipulator.OnStartDrop += () => _previewCenter.text = "Drop here to switch pattern";
                dragAndDropManipulator.OnEndDrop += () => _previewCenter.text = "Click to show all pattern";
                dragAndDropManipulator.OnEndDrop += () => UpdatePreviewButtonsText("Click to show neighbours");
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
                _centerValue = pattern;
            }
            else
            {
                var dir = GetDirectionFromButtonName(slot.name);
                TogglePatternWithDir(_centerValue, dir, pattern);
            }

            Refresh();
        }

        private void TogglePatternWithDir(string center, Direction dir, string pattern)
        {
            var centerPattern = _patterns.First(e => e.Value == center);
            var patternId = _patterns.First(e => e.Value == pattern);
            var oldValue = centerPattern.Valid[(int)dir][patternId.Id];
            centerPattern.Valid[(int)dir][patternId.Id] = !oldValue;
            patternId.Valid[(int)GetComplementDirection(dir)][centerPattern.Id] = !oldValue;
            Save();
        }

        private Direction GetComplementDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
            }

            throw new ArgumentException("Unknown Direction");
        }

        private void Save()
        {
            var jObj = JsonConvert.DeserializeObject<JObject>(_cachedJson);
            jObj["Patterns"] = JToken.FromObject(_patterns);
            Undo.RecordObject(Current, "Pattern Explorer edit");
            Current.serializedJson = jObj.ToString(Formatting.None);
            _cachedJson = jObj.ToString();
            EditorUtility.SetDirty(Current);

            Refresh();
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


        private void UpdatePreviewButtonsText(string text)
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

        private void UpdateWhiteList()
        {
            var center = CenterPattern;
            if (string.IsNullOrEmpty(_centerValue) || center is null)
            {
                // Select All
                _whiteListPattern = _patterns!.Select(e => e.Value).ToHashSet();
            }
            else
            {
                PopulateWhiteList(_allDirection.Except(_filteredDirection).Select(e => center.Valid[(int)e]).ToArray());
            }
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
                _filteredDirection.Clear();
            }
            else
            {
                var dir = GetDirectionFromButtonName(btn.name);
                if (_filteredDirection.Contains(dir))
                {
                    _filteredDirection.Remove(dir);
                }
                else
                {
                    _filteredDirection.Add(dir);
                }
            }

            Refresh();
        }

        private void Refresh()
        {
            UpdateWhiteList();
            UpdatePreviewsButtons();
            GeneratePatternList(_patterns, _root, _scroll, _ghost);
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