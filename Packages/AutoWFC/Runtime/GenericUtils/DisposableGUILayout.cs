using System;
using UnityEngine;

namespace AutoWfc.GenericUtils
{
    public class DisposableGUILayout
    {
        public static Horizontal CreateHorizontal => new Horizontal();
        public static Vertical CreateVertical => new Vertical();
        public static ScrollView CreateScrollView(ref Vector2 position) => new ScrollView(ref position);
        
        public class Horizontal: IDisposable
        {
            public Horizontal()
            {
                GUILayout.BeginHorizontal();
            }
            public void Dispose()
            {
                GUILayout.EndHorizontal();
            }
        }
        public class Vertical: IDisposable
        {
            public Vertical()
            {
                GUILayout.BeginVertical();
            }
            public void Dispose()
            {
                GUILayout.EndVertical();
            }
        }
        public class ScrollView: IDisposable
        {
            public ScrollView(ref Vector2 position)
            {
                position = GUILayout.BeginScrollView(position);
            }
            public void Dispose()
            {
                GUILayout.EndScrollView();
            }
        }
    }
}