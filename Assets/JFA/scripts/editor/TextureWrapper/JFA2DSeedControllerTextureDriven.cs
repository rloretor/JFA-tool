using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JFA.editor
{
    [Serializable]
    public class JFA2DSeedControllerTextureDriven : JFA2DSeedController
    {
        Texture2D sourceTexture;

        public override void DrawProperties()
        {
            GUILayout.BeginHorizontal();
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.UpperCenter;
            style.fixedWidth = 70;
            GUILayout.Label("JFA source", style);
            sourceTexture = (Texture2D) EditorGUILayout.ObjectField(sourceTexture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
            GUILayout.EndHorizontal();

            if (sourceTexture != null)
            {
                if (sourceTexture.isReadable == false)
                {
                    Debug.LogError($"Texture is not readable {sourceTexture.name}");
                    sourceTexture = null;
                    return;
                }

                width = sourceTexture.width;
                height = sourceTexture.height;
            }
        }

        public override void PaintSeeds()
        {
            base.PaintSeeds();

            Color[] pixelBuffer = sourceTexture.GetPixels();
            Dictionary<Color, List<Vector2>> colorToPositions = new Dictionary<Color, List<Vector2>>();
            for (int i = 0; i < pixelBuffer.Length; i++)
            {
                Color c = pixelBuffer[i];
                if (c.a == 0)
                {
                    continue;
                }

                if (colorToPositions.ContainsKey(c) == false)
                {
                    colorToPositions[c] = new List<Vector2>();
                }

                float row = (float) i / sourceTexture.width;
                float column = i % sourceTexture.width;
                colorToPositions[c].Add(new Vector2(column / sourceTexture.width, row / sourceTexture.height));
            }

            foreach (KeyValuePair<Color, List<Vector2>> pair in colorToPositions)
            {
                Vector2 average = colorToPositions[pair.Key].Aggregate(new Vector2(0, 0), (s, v) => s + v) / (float) colorToPositions[pair.Key].Count;
                colorToPositions[pair.Key].Add(average);
            }

            var keysList = colorToPositions.Keys.ToList();
            for (int i = 0; i < pixelBuffer.Length; i++)
            {
                Color c = pixelBuffer[i];
                if (c.a == 0)
                {
                    pixelBuffer[i] = new Color(0, 0, 0, 0);
                    continue;
                }

                Vector2 pos = colorToPositions[pixelBuffer[i]].Last();
                float row = (float) i / sourceTexture.width;
                float column = i % sourceTexture.width;
                pixelBuffer[i] = new Color((column) / sourceTexture.width, (row) / sourceTexture.height, (keysList.IndexOf(pixelBuffer[i]) + 1.0f) / colorToPositions.Keys.Count, 0);
            }

            seed.SetPixels(pixelBuffer);
            seed.Apply();
        }

        public override bool IsTextureReady()
        {
            return sourceTexture != null && sourceTexture.isReadable;
        }
    }
}
