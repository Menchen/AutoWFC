using UnityEditor;
using UnityEngine;

namespace Script
{
    [CustomEditor(typeof(TilesetSlicer))]
    public class TilesetSlicerTool : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TilesetSlicer slicer = (TilesetSlicer) target;
            if (GUILayout.Button("Slice"))
            {
                slicer.WFCThis();
            }
        }
    }
}
