using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JFA.scripts.editor
{
    public class JumpFloodEditorWindow : EditorWindow
    {
        private int seedCount;
        private int width;
        private int height;
        private int MaxPasses;
        private int selectedWidth;
        private int selectedHeight;
        private int displayedTexture;

        private bool ForceMaxPasses;
        private bool SaveProcess;
        private bool displayUDF;

        private JFAConfigParams configParams;
        private JumpFloodAlgorithmTex JFA = null;
        private Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();

        private Material UDFMaterial;

        [MenuItem("Window/JFA/GenerateSDF")]
        private static void ShowWindow()
        {
            var window = GetWindow<JumpFloodEditorWindow>();
            window.titleContent = new GUIContent("Jump Flooding Algorithm");
            window.Show();
        }

        private void OnGUI()
        {
            DrawProperties();
            EditorGUILayout.Space();
            SaveProcess = EditorGUILayout.Toggle("Save JFA Process:", SaveProcess);
            DrawMaxPasses();
            InitJFAConfigParams();
            int textures = Directory.GetFiles(JFAConfigParams.SavePath, "*.png", SearchOption.AllDirectories).Length;
            TryDrawJFAComputeButton(textures != 0);
            TryDisplayTexture(textures);
        }

        private void TryDisplayTexture(int texturesAmount)
        {
            if (texturesAmount == 0) return;
            EditorGUILayout.BeginHorizontal();
            displayedTexture = EditorGUILayout.IntSlider(displayedTexture, 0, texturesAmount - 1);
            displayUDF = EditorGUILayout.ToggleLeft("Display UDF", displayUDF);
            if (displayUDF && UDFMaterial == null)
            {
                UDFMaterial = new Material(Shader.Find("JFA_Analysis")) {hideFlags = HideFlags.HideAndDontSave};
            }

            EditorGUILayout.EndHorizontal();

            var tex = LoadTexture(displayedTexture);
            if (tex != null)
            {
                var AR = (float) tex.width / tex.height;
                var pixelRect = GUILayoutUtility.GetAspectRect(AR);
                Rect r = new Rect();
                r.height = Mathf.Abs(position.yMax - pixelRect.position.y) * 0.9f;
                r.width = r.height * AR;
                r.position = pixelRect.position + Vector2.right * (position.width / 2.0f - r.width / 2.0f);
                EditorGUILayout.BeginVertical();
                if (displayUDF)
                    EditorGUI.DrawPreviewTexture(r, tex, UDFMaterial);
                else
                    EditorGUI.DrawPreviewTexture(r, tex);

                EditorGUILayout.EndVertical();
            }
        }

        private void TryDrawJFAComputeButton(bool computedJFA)
        {
            if (seedCount > 0 && GUILayout.Button($"{(computedJFA ? "Recalculate" : "Compute")} JFA"))
            {
                foreach (var tex in loadedTextures)
                {
                    DestroyImmediate(tex.Value);
                }

                loadedTextures.Clear();
                ComputeJFA();
            }
        }

        private void InitJFAConfigParams()
        {
            configParams = new JFAConfigParams();
            configParams.ForceMaxPasses = ForceMaxPasses;
            configParams.MaxPasses = MaxPasses;
            configParams.RecordProcess = SaveProcess;
        }

        private void DrawProperties()
        {
            EditorGUILayout.LabelField("JFA parameters", EditorStyles.whiteBoldLabel);

            string[] options =
            {
                "Game Mode Display Size", (1 << 8).ToString(), (1 << 9).ToString(), (1 << 10).ToString(), (1 << 11).ToString(), (1 << 12).ToString()
            };

            selectedWidth = EditorGUILayout.Popup("Texture width", selectedWidth, options);
            width = selectedWidth == 0 ? EnsurePowerOf2(Camera.main.pixelWidth, options[selectedWidth]) : Int32.Parse(options[selectedWidth]);
            selectedHeight = EditorGUILayout.Popup("Texture height", selectedHeight, options);
            height = selectedHeight == 0 ? EnsurePowerOf2(Camera.main.pixelHeight, options[selectedHeight]) : Int32.Parse(options[selectedHeight]);
            EditorGUILayout.Space();
            seedCount = EditorGUILayout.IntField("Seeds:", seedCount);
            if (seedCount <= 0)
            {
                EditorGUILayout.HelpBox($"Please use a number higher than 0 and less than {width * height}", MessageType.Error);
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

        private void DrawMaxPasses()
        {
            ForceMaxPasses = EditorGUILayout.Toggle("Force Max passes:", ForceMaxPasses);
            if (ForceMaxPasses)
            {
                if (MaxPasses < GetMinPasses(width, height))
                {
                    EditorGUILayout.HelpBox($"The algorithm must run with at least {GetMinPasses(width, height)} passes", MessageType.Warning);
                }

                MaxPasses = EditorGUILayout.IntField($"Max passes :", MaxPasses);
            }
        }

        private Texture2D LoadTexture(int number)
        {
            var path = Directory.GetFiles(JFAConfigParams.SavePath, "*.png", SearchOption.AllDirectories).OrderBy(x => x).ToList()[number];
            path = Path.Combine(JFAConfigParams.SavePath, path);

            Texture2D tex;
            if (loadedTextures.TryGetValue(path, out tex))
            {
                return tex;
            }

            if (File.Exists(path))
            {
                var bytes = System.IO.File.ReadAllBytes(Path.Combine(JFAConfigParams.SavePath, path));
                tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                loadedTextures.Add(path, tex);
                return tex;
            }

            Debug.LogError($"Could not find anything at {path}");
            return null;
        }

        public void ComputeJFA()
        {
            Texture2D Seed = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                name = "Seed",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] colors = new Color [Seed.width * Seed.height];
            for (var index = 0;
                index < colors.Length;
                index++)
            {
                colors[index] = new Color(0, 0, 0, 0);
            }

            Random.InitState(System.DateTime.Now.Second);
            for (int i = 0;
                i < seedCount;
                i++)
            {
                var randPix = new Vector2Int();
                randPix.x = Random.Range(0, Seed.height);
                randPix.y = Random.Range(0, Seed.width);
                int pixi = randPix.x * Seed.width + randPix.y;
                colors[pixi] = new Color((float) randPix.x / Seed.height, (float) randPix.y / Seed.width, 0, 0);
            }

            Seed.SetPixels(colors);
            Seed.Apply(false);
            JFA = new JumpFloodAlgorithmTex(Seed, configParams);
            JFA.Compute();
        }

        private int GetMinPasses(float width, float height)
        {
            return Mathf.CeilToInt(Mathf.Log(Mathf.Max(width, height), 2f));
        }
    }
}
