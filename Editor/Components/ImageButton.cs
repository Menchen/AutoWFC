using UnityEditor;
using UnityEngine;

namespace AutoWfc.Editor.Components
{
    public struct ImageButton
    {
        public Rect Rect { get; set; }
        public Texture Texture { get; set; }
        public GUIStyle GUIStyle { get; set; }

        public bool Draw()
        {
            if (Texture is not null)
            {
                GUI.DrawTexture(Rect,Texture,ScaleMode.ScaleToFit);
            }
            var oldColor = GUI.color;
            if (Rect.Contains(Event.current.mousePosition))
            {
                // GUI.skin.
                if (Event.current.type == EventType.MouseDown)
                {
                    
                    GUI.color = new Color(0.188f, 0.427f, 0.714f, 0.3f);
                }
                else
                {
                    GUI.color = new Color(0.3f, 0.6f, 1.0f, 0.3f);
                }
            }
            else
            {
                GUI.color = new Color(0f, 0f, 0f, 0f);
            }
            var isPressed = GUI.Button(Rect, "", GUIStyle ?? GUI.skin.button);
            GUI.color = oldColor;

            return isPressed;
        }

//         public static void GetGuiStyle()
//         {
//             GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
//             Texture2D buttonTexture = Resources.Load<Texture2D>("ButtonTexture");
//
// // Set the background images for all three states to the button texture
//             buttonStyle.normal.background = buttonTexture;
//             buttonStyle.hover.background = buttonTexture;
//             buttonStyle.active.background = buttonTexture;
//
// // Set the text color for all three states to white
//             buttonStyle.normal.textColor = Color.white;
//             buttonStyle.hover.textColor = Color.white;
//             buttonStyle.active.textColor = Color.white;
//
// // Set the alignment of the text to center
//             buttonStyle.alignment = TextAnchor.MiddleCenter;
//
// // Set the font size to 16
//             buttonStyle.fontSize = 16;
//
// // Create a new texture with the same size as the button
//             Texture2D backgroundTexture = new Texture2D((int)buttonRect.width, (int)buttonRect.height);
//
// // Fill the texture with a transparent color
//             Color[] pixels = new Color[backgroundTexture.width * backgroundTexture.height];
//             for (int i = 0; i < pixels.Length; i++)
//             {
//                 pixels[i] = new Color(0, 0, 0, 0);
//             }
//             backgroundTexture.SetPixels(pixels);
//             backgroundTexture.Apply();
//
// // Draw the button texture onto the background texture
//             int textureWidth = buttonTexture.width;
//             int textureHeight = buttonTexture.height;
//             int xOffset = (int)((backgroundTexture.width - textureWidth) / 2);
//             int yOffset = (int)((backgroundTexture.height - textureHeight) / 2);
//             backgroundTexture.SetPixels(xOffset, yOffset, textureWidth, textureHeight, buttonTexture.GetPixels());
//             backgroundTexture.Apply();
//
// // Set the normal, hover, and active backgrounds of the button style to the background texture
//             buttonStyle.normal.background = backgroundTexture;
//             buttonStyle.hover.background = backgroundTexture;
//             buttonStyle.active.background = backgroundTexture;
//         }
    }
}