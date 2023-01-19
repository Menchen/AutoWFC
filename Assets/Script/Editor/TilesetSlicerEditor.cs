using System;
using Script.Extensions;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace Script
{
    [CustomEditor(typeof(TilesetSlicer))]
    public class TilesetSlicerEditor : Editor
    {
        // private BoundsInt selectionBounds;
        private Vector3Int? _p1, _p2;
        private EditorMode _editorMode;
        private SelectState _selectState;

        private enum SelectState
        {
            None,Drag,Done
        }
        private enum EditorMode
        {
            None,Select
        }


        private void HandleSelection()
        {


            var id = GUIUtility.GetControlID(FocusType.Passive);
            var mouseGlobal = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(0);
            mouseGlobal.z = 0;
            var local = _tilemap.WorldToCell(mouseGlobal);
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
                    }
                    break;
                case SelectState.Drag:
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        Event.current.Use();
                    }else if (Event.current.type == EventType.MouseMove && Event.current.button == 0)
                    {
                        _selectState = SelectState.None;
                        _p2 = local;
                        _targetTilesetSlicer.CurrentSelection = _p1!.Value.BoundsIntFrom2Points(_p2.Value);
                        Event.current.Use();
                        GUIUtility.hotControl = 0;
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
                        GridEditorUtility.DrawGridMarquee(_tilemap,new BoundsInt(local,Vector3Int.one),Color.cyan);
                    }else if (_p2 is null)
                    {
                        // p1 selected and p2 missing
                        GridEditorUtility.DrawGridMarquee(_tilemap, _p1.Value.BoundsIntFrom2Points(local),Color.cyan);
                    }
                }

                if (_p1 is not null && _p2 is not null)
                {
                    // p1 & p2 selected
                    GridEditorUtility.DrawGridMarquee(_tilemap, _p1.Value.BoundsIntFrom2Points(_p2.Value),Color.cyan);
                }
                
            }else if (Event.current.type != EventType.Used)
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

        private TilesetSlicer _targetTilesetSlicer;
        private Tilemap _tilemap;
        private void OnEnable()
        {
            _targetTilesetSlicer = (TilesetSlicer)target;
            _tilemap = _targetTilesetSlicer.GetComponent<Tilemap>();
            _p1 = _targetTilesetSlicer.CurrentSelection?.min;
            _p2 = _targetTilesetSlicer.CurrentSelection?.max;
        }

        private bool _selectActive;
        private Tool _lastTool = Tool.None;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // public static void DrawGridMarquee(GridLayout gridLayout, BoundsInt area, Color color)
            
            // SelectionActive = GridSelection.active;
            TilesetSlicer slicer = (TilesetSlicer) target;
            // GridEditorUtility.
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate"))
            {
                slicer.WfcThis();
            }
            if (GUILayout.Button("GenerateWithJson"))
            {
                slicer.WfcWithJson( _p1 is null || _p2 is null ? null :_p1.Value.BoundsIntFrom2Points(_p2.Value));
            }

            _selectActive = GUILayout.Toggle(_selectActive, "Select");
            _editorMode = _selectActive? EditorMode.Select : EditorMode.None;
            if (_selectActive)
            {
                // _selectActive = !_selectActive;
                _editorMode = _selectActive? EditorMode.Select : EditorMode.None;
                if (_selectActive)
                {
                    _lastTool = Tools.current;
                    Tools.current = Tool.None;
                    Tools.hidden = true;
                }
                else
                {
                    // TODO Add listener for external tools change
                    Tools.current = _lastTool;
                    Tools.hidden = false;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label($"Editor Tools Label");
        }

    }
}
