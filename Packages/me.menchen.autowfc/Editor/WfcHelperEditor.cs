using System;
using AutoWfc.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AutoWfc.Editor
{
    [CustomEditor(typeof(WfcHelper))]
    public class WfcHelperEditor : UnityEditor.Editor
    {
        // private BoundsInt selectionBounds;
        private Vector3Int? _p1, _p2;
        private EditorMode _editorMode;
        private SelectState _selectState;

        private enum SelectState
        {
            None,
            Drag,
            Done
        }

        private enum EditorMode
        {
            None,
            Select
        }


        private void HandleSelection()
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var mouseGlobal = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(0);
            mouseGlobal.z = 0;
            var local = _tilemap.WorldToCell(mouseGlobal);
            // HandleUtility.AddDefaultControl();
            switch (_selectState)
            {
                case SelectState.None:
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        _selectState = SelectState.Drag;
                        _p1 = local;
                        _p2 = null;
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                        Repaint(); // Refresh inspector for button disable/enable 
                    }

                    break;
                case SelectState.Drag:
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        // GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    else if (Event.current.type is EventType.MouseMove or EventType.MouseUp &&
                             Event.current.button == 0)
                    {
                        // Create Bounds
                        _selectState = SelectState.None;
                        _p2 = local;
                        _targetWfcHelper.CurrentSelection = _p1!.Value.BoundsIntFrom2Points(_p2.Value);
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                        Repaint(); // Refresh inspector for button disable/enable 
                    }

                    break;
            }
        }

        public void OnSceneGUI()
        {
            if (Event.current.type is EventType.Repaint)
            {
                var mouseGlobal = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(0);
                mouseGlobal.z = 0;
                var local = _tilemap.WorldToCell(mouseGlobal);
                // GridEditorUtility.DrawGridMarquee(_tilemap,new BoundsInt(local,new Vector3Int(1,1,0)),Color.cyan);
                // GridEditorUtility.DrawGridMarquee(_tilemap,new BoundsInt(Vector3Int.zero, new Vector3Int(1,1,0)),Color.yellow);

                if (_editorMode == EditorMode.Select)
                {
                    if (_p1 is null)
                    {
                        // Draw mouse preview
                        GridEditorUtility.DrawGridMarquee(_tilemap, new BoundsInt(local, Vector3Int.one), Color.cyan);
                    }
                    else if (_p2 is null)
                    {
                        // p1 selected and p2 missing
                        GridEditorUtility.DrawGridMarquee(_tilemap, _p1.Value.BoundsIntFrom2Points(local), Color.cyan);
                    }
                }

                if (_p1 is not null && _p2 is not null)
                {
                    // p1 & p2 selected
                    GridEditorUtility.DrawGridMarquee(_tilemap, _p1.Value.BoundsIntFrom2Points(_p2.Value), Color.cyan);
                }
            }
            else if (Event.current.type != EventType.Used)
            {
                // Force repaint on mouse movement
                switch (_editorMode)
                {
                    case EditorMode.Select:
                        HandleSelection();
                        break;
                }

                SceneView.currentDrawingSceneView.Repaint();
            }
        }

        private WfcHelper _targetWfcHelper;
        private Tilemap _tilemap;

        private void OnDisable()
        {
            if (PatternExplorerEditorToolKit.Current == _targetWfcHelper)
            {
                PatternExplorerEditorToolKit.Current = null;

            }
        }

        private void OnEnable()
        {
            _targetWfcHelper = (WfcHelper)target;
            _tilemap = _targetWfcHelper.GetComponent<Tilemap>();
            _p1 = _targetWfcHelper.CurrentSelection?.min;
            _p2 = _targetWfcHelper.CurrentSelection?.max;

            PatternExplorerEditorToolKit.Current = _targetWfcHelper;
        }

        private bool _selectActive;
        private Tool _lastTool = Tool.None;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // public static void DrawGridMarquee(GridLayout gridLayout, BoundsInt area, Color color)

            // SelectionActive = GridSelection.active;
            WfcHelper slicer = (WfcHelper)target;
            // GridEditorUtility.
            if (GUILayout.Button("Create new pattern From tileset"))
            {
                Undo.RecordObject(slicer,"Create new pattern from selection");
                slicer.CreateWfcFromTileSet();
            }

            using (new EditorGUI.DisabledScope(slicer.CurrentSelection is null))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create new pattern from selection"))
                {
                    Undo.RecordObject(slicer,"Create new pattern from selection");
                    slicer.CreateFromSelection(slicer.CurrentSelection!.Value);
                }
                GUILayout.EndHorizontal();
            }


            // GUI.enabled = ;
            bool clickedGenerateRegion;
            bool clickSetToEmpty;
            bool clickTrainFromSelection;
            bool clickUnlearnFromSelection;
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(slicer.serializedJson) || slicer.CurrentSelection is null))
            {
                GUILayout.BeginHorizontal();
                clickedGenerateRegion = GUILayout.Button("GenerateWithJson");
                clickSetToEmpty = GUILayout.Button("SetToEmpty");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                clickTrainFromSelection = GUILayout.Button("TrainFromSelection");
                using (new EditorGUI.DisabledScope(!GUI.enabled || slicer.CurrentSelection?.size.sqrMagnitude != 6))
                {
                    clickUnlearnFromSelection = GUILayout.Button("UnLearnFromSelection");
                }
            }

            if (clickTrainFromSelection)
            {
                Undo.RecordObject(slicer,"Learn pattern");
                slicer.LearnPatternFromRegion(slicer.CurrentSelection!.Value);
                EditorUtility.SetDirty(slicer);
            }

            if (clickUnlearnFromSelection)
            {
                Undo.RecordObject(slicer,"Unlearn pattern");
                slicer.UnLearnPatternFromRegion(slicer.CurrentSelection!.Value);
                EditorUtility.SetDirty(slicer);
            }

            if (clickedGenerateRegion)
            {
                var generatedWfc = slicer.GenerateWfc(slicer.CurrentSelection!.Value);
                if (generatedWfc is not null)
                {
                    // Use RegisterCompleteObjectUndo instead of RecordObject for tilemap,
                    // because it's faster to replace the object instead of tracking the changes.
                    Undo.RegisterCompleteObjectUndo(_tilemap,"WFC Tilemap");
                    slicer.ApplyWfc(generatedWfc,slicer.CurrentSelection!.Value);
                    EditorUtility.SetDirty(_tilemap);
                }
            }

            if (clickSetToEmpty)
            {
                Undo.RegisterCompleteObjectUndo(_tilemap,"Tilemap set empty");
                // _tilemap.BoxFill(slicer.CurrentSelection!.Value.position, null, 0, 0,
                //     slicer.CurrentSelection.Value.size.x, slicer.CurrentSelection.Value.size.y);
                for (int x = 0; x < slicer.CurrentSelection!.Value.size.x; x++)
                {
                    for (int y = 0; y < slicer.CurrentSelection.Value.size.y; y++)
                    {
                        _tilemap.SetTile(slicer.CurrentSelection.Value.position + new Vector3Int(x, y, 0), null);
                    }
                }
                EditorUtility.SetDirty(_tilemap);
            }

            // Disable selection if another Tool is selected
            if (Tools.current != Tool.None)
            {
                _selectActive = false;
            }

            _selectActive = GUILayout.Toggle(_selectActive, "Select");
            _editorMode = _selectActive ? EditorMode.Select : EditorMode.None;
            if (_selectActive)
            {
                _editorMode = _selectActive ? EditorMode.Select : EditorMode.None;
                if (_selectActive)
                {
                    _lastTool = Tools.current;
                    Tools.current = Tool.None;
                    Tools.hidden = true;
                }
                else
                {
                    Tools.current = _lastTool;
                    Tools.hidden = false;
                }
            }

            GUILayout.EndHorizontal();
            if (GUILayout.Button("Open Pattern Explorer"))
            {
                PatternExplorerEditorToolKit.Current = slicer;
                PatternExplorerEditorToolKit.Init();
            }
            GUILayout.Label("Editor Tools Label");
        }

    }
}