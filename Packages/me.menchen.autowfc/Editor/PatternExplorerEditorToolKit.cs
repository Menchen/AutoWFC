using System;
using System.Collections.Generic;
using AutoWfc.Wfc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AutoWfc.Editor
{
    public class PatternExplorerEditorToolKit : EditorWindow
    {

        [MenuItem("Tools/PatternToolKit")]
        public static void Init()
        {
            Uikit = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/me.menchen.autowfc/Editor/UIToolKit/PatternExplorer.uxml");
            EditorWindow wdn = GetWindow<PatternExplorerEditorToolKit>();
            wdn.titleContent = new GUIContent("ToolKit Pattern");
        }

        public static VisualTreeAsset Uikit;

        private List<WfcUtils<string>.Pattern> _patterns;
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            _cachedJson = PatternExplorerWindow.Current?.serializedJson;
            if (string.IsNullOrEmpty(_cachedJson))
            {
                rootVisualElement.Add(new Label("Missing"));
                return;
            }
            
            _patterns = JsonConvert.DeserializeObject<JObject>(_cachedJson)["Patterns"]
                ?.ToObject<List<WfcUtils<string>.Pattern>>();
            var root = Uikit.Instantiate();
            rootVisualElement.Add(root);
            var patternList = root.Q("#PatternList");
            patternList.Clear();
            _patterns?.ForEach(e=>patternList.Add(new Button(){text = e.Value}));
        }

        private string _cachedJson;

        public void OnGUI()
        {
            if (PatternExplorerWindow.Current?.serializedJson != _cachedJson)
            {
                _cachedJson = PatternExplorerWindow.Current?.serializedJson;
                CreateGUI();
            }
        }
    }
}