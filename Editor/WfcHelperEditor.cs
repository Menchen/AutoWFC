using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoWfc.Extensions;
using AutoWfc.GenericUtils;
using AutoWfc.Wfc;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

namespace AutoWfc.Editor
{
    [CustomEditor(typeof(WfcHelper))]
    public class WfcHelperEditor : UnityEditor.Editor
    {
        // private BoundsInt selectionBounds;
        private Vector3Int? _p1, _p2;
        private EditorMode _editorMode;
        private SelectState _selectState;
        private string[] _lastGeneratedRegion;

        [SerializeField] private List<Vector3Int> _conflictTiles;
        [SerializeField] private List<Vector3Int> _conflictEdgeTiles;

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


        private void DrawConflictMap()
        {
            if (_conflictTiles is not null)
            {
                foreach (var pos in _conflictTiles.Except(_conflictEdgeTiles))
                {
                    GridEditorUtility.DrawGridMarquee(_tilemap, new BoundsInt(pos, Vector3Int.one),
                        new Color(1f, 0.1f, 0.1f, 0.9f), localOffset: new Vector3(0, 0, 2));
                }

                foreach (var pos in _conflictEdgeTiles)
                {
                    GridEditorUtility.DrawGridMarquee(_tilemap, new BoundsInt(pos, Vector3Int.one),
                        new Color(155 / 255f, 25 / 255f, 170 / 255f, 1), thickness: 2f,
                        localOffset: new Vector3(0, 0, 3));
                }
            }
        }

        private string _benchmarkText;

        private void Benchmark(WfcUtils<string>.NextCell.NextCellEnum nextCellEnum,
            WfcUtils<string>.SelectPattern.SelectPatternEnum selectPatternEnum, int count)
        {
            Debug.Log($"Benchmark start for {nextCellEnum} - {selectPatternEnum} at: {DateTime.Now}");
            _targetWfcHelper.selectPatternEnum = selectPatternEnum;
            _targetWfcHelper.nextCellEnum = nextCellEnum;
            var stopWatch = new Stopwatch();
            var fail = 0;
            stopWatch.Start();
            for (int i = 0; i < count; i++)
            {
                var result = _targetWfcHelper.GenerateWfc(_targetWfcHelper.CurrentSelection!.Value, maxRetry: 1);
                if (result is null)
                {
                    fail++;
                }
            }

            stopWatch.Stop();
            Debug.Log(
                $"Benchmark end for {nextCellEnum} - {selectPatternEnum} with {fail} fails at: {DateTime.Now}, elapsed: {stopWatch.Elapsed}");
            _benchmarkText += $"{nextCellEnum} - {selectPatternEnum}, {fail}, {stopWatch.Elapsed}\n";
        }

        private void BenchmarkAll()
        {
            var oldNextCell = _targetWfcHelper.nextCellEnum;
            var oldPatternFn = _targetWfcHelper.selectPatternEnum;
            _benchmarkText = "";

            // foreach (var nextCellFn in Enum.GetValues(typeof(WfcUtils<string>.NextCell.NextCellEnum)).Cast<WfcUtils<string>.NextCell.NextCellEnum>())
            // {
            var nextCellFn = WfcUtils<string>.NextCell.NextCellEnum.MinStateEntropyWeighted;
            // var patternFn = WfcUtils<string>.SelectPattern.SelectPatternEnum.PatternUniform;
            foreach (var patternFn in Enum.GetValues(typeof(WfcUtils<string>.SelectPattern.SelectPatternEnum))
                         .Cast<WfcUtils<string>.SelectPattern.SelectPatternEnum>())
            {
                Benchmark(nextCellFn, patternFn, 500);
            }
            // }

            _targetWfcHelper.nextCellEnum = oldNextCell;
            _targetWfcHelper.selectPatternEnum = oldPatternFn;
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
                        _lastGeneratedRegion = null;
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
                        _lastGeneratedRegion = null;
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
                    GridEditorUtility.DrawGridMarquee(_tilemap, _p1.Value.BoundsIntFrom2Points(_p2.Value),
                        _lastGeneratedRegion == null ? Color.magenta : new Color(240 / 255f, 156 / 255f, 38 / 255f));
                }

                DrawConflictMap();
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

        private GUIStyle _textAreaStyle;

        private void OnEnable()
        {
            _textAreaStyle = new GUIStyle(EditorStyles.textArea ?? new GUIStyle()) { wordWrap = true };
            _targetWfcHelper = (WfcHelper)target;
            _tilemap = _targetWfcHelper.GetComponent<Tilemap>();
            _p1 = _targetWfcHelper.CurrentSelection?.min;
            _p2 = _targetWfcHelper.CurrentSelection?.max - Vector3Int.one;
            _lastGeneratedRegion = null;

            PatternExplorerEditorToolKit.Current = _targetWfcHelper;
        }

        private bool _selectActive;
        private Tool _lastTool = Tool.None;
        private bool _showJsonTextArea;

        public override void OnInspectorGUI()
        {
            WfcHelper wfcHelper = (WfcHelper)target;
            if (string.IsNullOrEmpty(wfcHelper.tileOutputFolder?.Path))
            {
                GUILayout.Label("Missing Tile output folder, some feature might not work.");
            }

            DrawDefaultInspector();
            _showJsonTextArea = EditorGUILayout.Foldout(_showJsonTextArea, "Show JSON model");
            if (_showJsonTextArea)
            {
                wfcHelper.serializedJson =
                    EditorGUILayout.TextArea(wfcHelper.serializedJson, _textAreaStyle, GUILayout.Height(300));
            }

            // public static void DrawGridMarquee(GridLayout gridLayout, BoundsInt area, Color color)

            // SelectionActive = GridSelection.active;
            // GridEditorUtility.
            if (GUILayout.Button("Create new model from tile set"))
            {
                Undo.RecordObject(wfcHelper, "Create new model from selection");
                wfcHelper.CreateWfcFromTileSet();
            }

            using (new EditorGUI.DisabledScope(wfcHelper.CurrentSelection is null))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create new model from selection"))
                {
                    Undo.RecordObject(wfcHelper, "Create new model from selection");
                    wfcHelper.CreateFromSelection(wfcHelper.CurrentSelection!.Value);
                }

                GUILayout.EndHorizontal();
            }


            // GUI.enabled = ;
            bool clickedGenerateRegion;
            bool clickSetToEmpty;
            bool clickTrainFromSelection;
            bool clickUnlearnFromSelection;
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(wfcHelper.serializedJson) ||
                                               wfcHelper.CurrentSelection is null))
            {
                using (new DisposableGUILayout.Horizontal())
                {
                    clickedGenerateRegion = GUILayout.Button("Generate tiles from model");
                    clickSetToEmpty = GUILayout.Button("Set to empty");
                }

                using (new DisposableGUILayout.Horizontal())
                {
                    clickTrainFromSelection = GUILayout.Button("Train from selected region");
                    // if (GUILayout.Button("Benchmark All"))
                    // {
                    //     BenchmarkAll();
                    // }

                    using (new EditorGUI.DisabledScope(!GUI.enabled ||
                                                       wfcHelper.CurrentSelection?.size.sqrMagnitude != 6))
                    {
                        clickUnlearnFromSelection = GUILayout.Button("Unlearn from selected region");
                    }
                }
            }

            using (new DisposableGUILayout.Horizontal())
            {
                if (clickTrainFromSelection)
                {
                    Undo.RecordObject(wfcHelper, "Learn pattern");
                    wfcHelper.LearnPatternFromRegion(wfcHelper.CurrentSelection!.Value);
                    EditorUtility.SetDirty(wfcHelper);
                }

                if (clickUnlearnFromSelection)
                {
                    Undo.RecordObject(wfcHelper, "Unlearn pattern");
                    wfcHelper.UnLearnPatternFromRegion(wfcHelper.CurrentSelection!.Value);
                    EditorUtility.SetDirty(wfcHelper);
                }
            }

            using (new DisposableGUILayout.Horizontal())
            {
                if (clickedGenerateRegion)
                {
                    if (_lastGeneratedRegion is null)
                    {
                        // Cache region before WFC
                        var beforeWfc =
                            wfcHelper.GetTilesFromTilemap(wfcHelper.CurrentSelection!.Value, _tilemap, out _);
                        _lastGeneratedRegion = beforeWfc;
                    }

                    var generatedWfc = wfcHelper.GenerateWfc(wfcHelper.CurrentSelection!.Value, _lastGeneratedRegion);
                    if (generatedWfc is not null)
                    {
                        // Use RegisterCompleteObjectUndo instead of RecordObject for tilemap,
                        // because it's faster to replace the object instead of tracking the changes.
                        Undo.SetCurrentGroupName("WFC Tilemap");
                        Undo.RegisterCompleteObjectUndo(_tilemap, "WFC Tilemap");
                        Undo.RegisterCompleteObjectUndo(this, "Conflict Map");
                        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                        _conflictTiles = wfcHelper.GenerateConflictMap(wfcHelper.CurrentSelection!.Value,
                            _lastGeneratedRegion, generatedWfc, out var edge).ToList();
                        _conflictEdgeTiles = edge.ToList();

                        wfcHelper.ApplyWfc(generatedWfc, wfcHelper.CurrentSelection!.Value);
                        EditorUtility.SetDirty(_tilemap);
                    }
                }

                if (clickSetToEmpty)
                {
                    Undo.RegisterCompleteObjectUndo(_tilemap, "Set tilemap region to empty");
                    // _tilemap.BoxFill(slicer.CurrentSelection!.Value.position, null, 0, 0,
                    //     slicer.CurrentSelection.Value.size.x, slicer.CurrentSelection.Value.size.y);
                    wfcHelper.SetEmpty(wfcHelper.CurrentSelection!.Value);

                    EditorUtility.SetDirty(_tilemap);
                }
            }

            // Disable selection if another Tool is selected
            if (Tools.current != Tool.None)
            {
                _selectActive = false;
            }

            EditorGUILayout.EditorToolbar();


            EditorGUILayout.BeginHorizontal("Toolbar", GUILayout.ExpandWidth(true));
            // GUILayout.FlexibleSpace();
            _selectActive = GUILayout.Toggle(_selectActive, "Click Here to toggle grid selection", GUI.skin.button,
                GUILayout.ExpandWidth(true));
            // GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();


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

            using (new DisposableGUILayout.Horizontal())
            {
                if (GUILayout.Button("Open Pattern Explorer"))
                {
                    PatternExplorerEditorToolKit.Current = wfcHelper;
                    PatternExplorerEditorToolKit.Init();
                }
            }

            // EditorGUILayout.TextArea(_benchmarkText);
        }
    }
}