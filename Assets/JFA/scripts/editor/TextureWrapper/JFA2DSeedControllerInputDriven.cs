using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JFA.editor
{
    [Serializable]
    public class JFA2DSeedControllerInputDriven : JFA2DSeedController
    {
        private int seedCount;
        private int selectedWidth;
        private int selectedHeight;

        private readonly string[] textureSizeOptions =
        {
            "Game Mode Display Size",
            (1 << 2).ToString(),
            (1 << 3).ToString(),
            (1 << 4).ToString(),
            (1 << 5).ToString(),
            (1 << 6).ToString(),
            (1 << 7).ToString(),
            (1 << 8).ToString(),
            (1 << 9).ToString(),
            (1 << 10).ToString(),
            (1 << 11).ToString(),
            (1 << 12).ToString(),
        };

        public override void DrawProperties()
        {
            selectedWidth = EditorGUILayout.Popup("Texture width", selectedWidth, textureSizeOptions);
            width = TextureSizeInput(textureSizeOptions[selectedWidth], true);
            selectedHeight = EditorGUILayout.Popup("Texture height", selectedHeight, textureSizeOptions);
            height = TextureSizeInput(textureSizeOptions[selectedHeight], false);
            EditorGUILayout.Space();
            seedCount = EditorGUILayout.IntField("Seeds:", seedCount);
            if (seedCount <= 0)
            {
                EditorGUILayout.HelpBox($"Please use a number higher than 0 and less than {width * height}", MessageType.Error);
            }
        }

        private int TextureSizeInput(string option, bool isWidth)
        {
            try
            {
                return Int32.Parse(option);
            }
            catch (Exception e)
            {
                if (Camera.main == null)
                {
                    Debug.LogError("Camera is null, cannot get properties");
                    throw e;
                }

                Camera main = Camera.main;
                return EnsurePowerOf2(isWidth ? main.pixelWidth : main.pixelHeight, option);
            }
        }

        private int EnsurePowerOf2(int value, string label)
        {
            int newVal = value;
            if (!value.IsPowerOfTwo())
            {
                newVal = value.ToNearestPow2();
                EditorGUILayout.HelpBox($"{label}({value}) is not power of two, using closest pow2 -->{newVal}", MessageType.Warning);
                return newVal;
            }

            return newVal;
        }

        public override void PaintSeeds()
        {
            base.PaintSeeds();
            int total = seed.width * seed.height;
            Color[] colors = new Color [total];
            for (var index = 0; index < colors.Length; index++)
            {
                colors[index] = new Color(0, 0, 0, 0);
            }

            Random.InitState(DateTime.Now.Second);
            for (int i = 0; i < seedCount; i++)
            {
                var randPix = new Vector2Int();
                randPix.x = Random.Range(0, seed.height);
                randPix.y = Random.Range(0, seed.width);
                int pixi = randPix.x * seed.width + randPix.y;
                colors[pixi] = new Color((float) randPix.y / seed.width, (float) randPix.x / seed.height, (float) (i + 1) / seedCount, 0);
            }

            seed.SetPixels(colors);
            seed.Apply(false);
        }

        public override bool IsTextureReady()
        {
            return seedCount > 0;
        }
    }
}
