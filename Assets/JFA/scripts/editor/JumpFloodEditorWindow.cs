using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
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

        private bool forceMaxPasses;
        private bool saveProcess;
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
            DrawUILine(Color.black, 8);
            EditorGUILayout.Space();
            saveProcess = EditorGUILayout.Toggle("Save JFA Process:", saveProcess);
            SetMaxPasses();
            DrawUILine(Color.black, 8);
            InitJFAConfigParams();
            int textures = Directory.GetFiles(JFAConfigParams.SavePath, "*.png", SearchOption.AllDirectories).Length;
            TryDrawJFAComputeButton(textures != 0);
            TryDisplayArea(textures);
        }

        private float posx;
        private float posy;
        private float scale;
        private int debugType;

        private void TryDisplayArea(int texturesAmount)
        {
            if (texturesAmount == 0) return;
            if (UDFMaterial == null)
            {
                UDFMaterial = new Material(Shader.Find("JFA_Analysis")) {hideFlags = HideFlags.HideAndDontSave};
            }

            DrawTextureHandlers(texturesAmount);
            string[] DebugTypes = new[] {"showDistance", "filterDistance", "show cell"};
            debugType = EditorGUILayout.Popup("Debug Type", debugType, DebugTypes);
            foreach (var current in DebugTypes)
            {
                UDFMaterial.SetInt(current, DebugTypes[debugType].Equals(current) ? 1 : 0);
            }

            TryDrawTexture();
            GUILayout.Space(10);
        }

        private void TryDrawTexture()
        {
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

                UDFMaterial.SetVector("translate", new Vector4(posx, posy, scale, 0));
                UDFMaterial.SetInt("analyze", displayUDF ? 1 : 0);
                EditorGUI.DrawPreviewTexture(r, tex, UDFMaterial, ScaleMode.ScaleAndCrop, 0.0f, 0);

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawTextureHandlers(int texturesAmount)
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Result to display");
            displayedTexture = EditorGUILayout.IntSlider(displayedTexture, 0, texturesAmount - 1);
            EditorGUILayout.EndHorizontal();
            DrawUILine(Color.gray, 1, 15);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("(displacement X,displacement Y)");
            posx = EditorGUILayout.Slider(posx, -0.5f, 0.5f);
            EditorGUILayout.Separator();
            posy = EditorGUILayout.Slider(posy, -0.5f, 0.5f);
            EditorGUILayout.EndHorizontal();

            DrawUILine(Color.gray, 1, 15);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Zoom");
            scale = EditorGUILayout.Slider(scale, 1, 0);
            EditorGUILayout.EndHorizontal();
            DrawUILine(Color.gray, 1, 15);

            EditorGUILayout.EndVertical();
        }

        public void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        private void TryDrawJFAComputeButton(bool computedJFA)
        {
            bool usesSourceTex = sourceProperties == SourceOptions.SourceTexture && sourceTexture != null && sourceTexture.isReadable;
            if ((seedCount > 0 || usesSourceTex) && GUILayout.Button($"{(computedJFA ? "Recalculate" : "Compute")} JFA"))
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
            configParams.ForceMaxPasses = forceMaxPasses;
            configParams.MaxPasses = MaxPasses;
            configParams.RecordProcess = saveProcess;
        }

        private enum SourceOptions
        {
            SourceTexture = 0,
            Input = 1
        }

        SourceOptions sourceProperties = SourceOptions.SourceTexture;

        private void DrawProperties()
        {
            EditorGUILayout.LabelField("JFA parameters");
            DrawUILine(Color.black, 8);

            sourceProperties = (SourceOptions) EditorGUILayout.Popup("JFA properties source", (int) sourceProperties, Enum.GetNames(typeof(SourceOptions)));
            if (sourceProperties == SourceOptions.SourceTexture)
            {
                SetPropertiesFromTexture();
            }
            else
            {
                SetPropertiesFromInput();
            }
        }

        private void SetPropertiesFromInput()
        {
            string[] options =
            {
                "Game Mode Display Size", (1 << 8).ToString(), (1 << 9).ToString(), (1 << 10).ToString(), (1 << 11).ToString(), (1 << 12).ToString(),
            };

            selectedWidth = EditorGUILayout.Popup("Texture width", selectedWidth, options);
            width = TextureSizeInput(options[selectedWidth], true);
            selectedHeight = EditorGUILayout.Popup("Texture height", selectedHeight, options);
            height = TextureSizeInput(options[selectedHeight], false);
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

                return EnsurePowerOf2(isWidth ? Camera.main.pixelWidth : Camera.main.pixelHeight, option);
            }
        }

        Texture2D sourceTexture = null;

        private void SetPropertiesFromTexture()
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

        private void SetMaxPasses()
        {
            forceMaxPasses = EditorGUILayout.Toggle("Force Max passes:", forceMaxPasses);
            int minPasses = GetMinPasses(width, height);
            if (forceMaxPasses)
            {
                MaxPasses = EditorGUILayout.IntField($"Max passes :", MaxPasses);
                if (MaxPasses < minPasses)
                {
                    EditorGUILayout.HelpBox($"The algorithm must run with at least {GetMinPasses(width, height)} passes", MessageType.Warning);
                }
            }
            else
            {
                MaxPasses = minPasses;
            }
        }

        private Texture2D LoadTexture(int number)
        {
            var path = Directory.GetFiles(JFAConfigParams.SavePath, "*.png", SearchOption.AllDirectories).OrderBy(x => x).ToList()[number];
            path = Path.Combine(JFAConfigParams.SavePath, path);

            Texture2D tex;
            if (loadedTextures.TryGetValue(path, out tex))
            {
                tex.filterMode = FilterMode.Point;
                return tex;
            }

            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(Path.Combine(JFAConfigParams.SavePath, path));
                tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                tex.filterMode = FilterMode.Point;

                loadedTextures.Add(path, tex);
                return tex;
            }

            Debug.LogError($"Could not find anything at {path}");
            return null;
        }

        private void ComputeJFA()
        {
            Texture2D Seed = new Texture2D(width, height, TextureFormat.RGB24, false)
            {
                name = "Seed",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };
            Seed.filterMode = FilterMode.Point;
            if (sourceProperties == SourceOptions.SourceTexture)
            {
                HandleInputTexture(Seed);
            }
            else
            {
                FillSeedTexture(Seed);
            }

            JFA = new JumpFloodAlgorithmTex(Seed, configParams);
            JFA.Compute();
        }

        private void HandleInputTexture(Texture2D Seed)
        {
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
                pixelBuffer[i] = new Color((column) / sourceTexture.width, (row) / sourceTexture.height, (colorToPositions.Keys.ToList().IndexOf(pixelBuffer[i]) + 1.0f) / colorToPositions.Keys.Count, 0);
            }

            Seed.SetPixels(pixelBuffer);
            Seed.Apply();
        }

        float hash13(Vector3 p3)
        {
            p3 = new Vector3(
                (p3.x * .1031f) % 1.0f,
                (p3.y * .1031f) % 1.0f,
                (p3.z * .1031f) % 1.0f);
            p3 += Vector3.one * Vector3.Dot(p3,
                new Vector3(
                    p3.y + 33.33f,
                    p3.z + 33.33f,
                    p3.x + 33.33f
                ));
            return ((p3.x + p3.y) * p3.z) % 1.0f;
        }

        private void FillSeedTexture(Texture2D Seed)
        {
            int total = Seed.width * Seed.height;
            Color[] colors = new Color [total];
            for (var index = 0;
                index < colors.Length;
                index++)
            {
                colors[index] = new Color(0, 0, 0, 0);
            }

            Random.InitState(System.DateTime.Now.Second);
            for (int i = 0;
                i <= seedCount;
                i++)
            {
                var randPix = new Vector2Int();
                randPix.x = Random.Range(0, Seed.height);
                randPix.y = Random.Range(0, Seed.width);
                int pixi = randPix.x * Seed.width + randPix.y;
                colors[pixi] = new Color((float) randPix.x / Seed.height, (float) randPix.y / Seed.width, (float) i / seedCount, 0);
            }

            Seed.SetPixels(colors);
            Seed.Apply(false);
        }

        private int GetMinPasses(float width, float height)
        {
            return Mathf.CeilToInt(Mathf.Log(Mathf.Max(width, height), 2f));
        }
    }
}
