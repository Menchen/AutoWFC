using System;
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
            EditorWindow wdn = GetWindow<PatternExplorerEditorToolKit>();
            wdn.titleContent = new GUIContent("ToolKit Pattern");
        }
        public void CreateGUI()
        {
            if (string.IsNullOrEmpty(PatternExplorerWindow.Current?.serializedJson))
            {
                rootVisualElement.Add(new Label("Missing"));
            }
            else
            {
                rootVisualElement.Add(new Label("Found"));
            }
        }

        private string _cachedJson;

        public void OnGUI()
        {
            if (PatternExplorerWindow.Current?.serializedJson != _cachedJson)
            {
                _cachedJson = PatternExplorerWindow.Current?.serializedJson;
                rootVisualElement.Clear();
                CreateGUI();
            }
        }
    }
}