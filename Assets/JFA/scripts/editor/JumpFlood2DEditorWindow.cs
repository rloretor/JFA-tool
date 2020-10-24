using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JFA.editor;
using UnityEditor;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace JFA.scripts.editor
{
    public class JumpFlood2DEditorWindow : JumpFloodEditorBase
    {
        private int maxPasses;
        private int displayedTexture;
        private int debugType;

        private static readonly int uniformTranslate = Shader.PropertyToID("translate");

        private float posx;
        private float posy;
        private float scale;

        private bool forceMaxPasses;
        private bool displayUDF;

        private JFA2DSeedController seedController;

        private enum SourceOptions
        {
            SourceTexture = 0,
            Input = 1
        }

        private JFAConfig _config;
        private JumpFloodAlgorithmTex JFA = null;
        private Material UDFMaterial;

        private readonly Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
        private readonly string[] DebugTypes = {"show cell", "showDistance", "filterDistance", "boxFilterDistance"};

        [MenuItem("Window/JFA/GenerateSDF")]
        private static void ShowWindow()
        {
            var window = GetWindow<JumpFlood2DEditorWindow>();
            window.titleContent = new GUIContent("Jump Flooding Algorithm");
            window.Show();
        }

        protected override void DrawInteractiveArea()
        {
            if (savedTextureCount == 0) return;
            if (UDFMaterial == null)
            {
                UDFMaterial = new Material(Shader.Find("JFA_Analysis")) {hideFlags = HideFlags.HideAndDontSave};
            }

            DrawTextureHandlers(savedTextureCount);

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
            Texture2D tex = LoadTexture(displayedTexture);
            if (tex == null) return;

            float AR = (float) tex.width / tex.height;
            Rect pixelRect = GUILayoutUtility.GetAspectRect(AR);
            var height = Mathf.Abs(position.yMax - pixelRect.position.y) * 0.75f;
            var width = height * AR;

            TryDrawSaveButton(tex);
        }

        private void TryDrawSaveButton(Texture2D tex)
        {
            float AR = (float) tex.width / tex.height;
            Rect r, screenRect;
            r = screenRect = GUILayoutUtility.GetLastRect();
            r.yMax = position.height - 40;

            EditorGUI.DrawPreviewTexture(r, tex, UDFMaterial, ScaleMode.ScaleToFit, AR);

            Vector2 mousePosition = Event.current.mousePosition;


            if (screenRect.Contains(mousePosition))
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                {
                    posx += (mousePosition.x - screenRect.min.x) / screenRect.width;
                    posy += (mousePosition.y - screenRect.min.y) / screenRect.height;
                }
            }

            UDFMaterial.SetVector(uniformTranslate, new Vector4(0, 0, scale, 0));


            r.yMin = r.yMax;
            r.yMax = position.height;
            if (GUI.Button(r, "Save texture"))
            {
                var temp = RenderTexture.GetTemporary(tex.width, tex.height, 0);
                Texture2D tex2 = new Texture2D(tex.width, tex.height)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                Graphics.Blit(tex, temp, UDFMaterial, 0);
                RenderTexture.active = temp;
                tex2.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
                tex2.Apply();
                tex2.SaveTextureAsPng($"{Application.dataPath}/JFA/");
                RenderTexture.active = null;
                temp.Release();
            }


            // EditorGUILayout.EndVertical();
        }

        private void DrawTextureHandlers(int texturesAmount)
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Result to display");
            displayedTexture = EditorGUILayout.IntSlider(displayedTexture, 0, texturesAmount - 1);
            EditorGUILayout.EndHorizontal();
            EditorUtils.DrawUILine(Color.gray, 1, 15);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Zoom");
            scale = EditorGUILayout.Slider(scale, 1, 0);
            EditorGUILayout.EndHorizontal();
            EditorUtils.DrawUILine(Color.gray, 1, 15);

            EditorGUILayout.EndVertical();
        }


        protected override void DrawJFAButton()
        {
            bool computedJFA = savedTextureCount != 0;
            if (seedController.IsTextureReady() && GUILayout.Button($"{(computedJFA ? "Recalculate" : "Compute")} JFA"))
            {
                displayedTexture = 0;
                posx = posy = 0;
                scale = 1;

                foreach (var tex in loadedTextures)
                {
                    DestroyImmediate(tex.Value);
                }

                loadedTextures.Clear();
                ComputeJFA();
            }
        }

        protected override void InitJFAConfig()
        {
            DrawAndSetMaxPasses();

            _config = new JFAConfig {ForceMaxPasses = forceMaxPasses, MaxPasses = maxPasses, RecordProcess = shouldSaveProcess};
        }

        SourceOptions sourceProperties = SourceOptions.SourceTexture;

        protected override void DrawProperties()
        {
            SourceOptions tempSourceProperties = (SourceOptions) EditorGUILayout.Popup("JFA properties source", (int) sourceProperties, Enum.GetNames(typeof(SourceOptions)));

            if (seedController == null || tempSourceProperties != sourceProperties)
            {
                if (sourceProperties == SourceOptions.SourceTexture)
                {
                    seedController = new JFA2DSeedControllerTextureDriven();
                }
                else
                {
                    seedController = new JFA2DSeedControllerInputDriven();
                }
            }

            sourceProperties = tempSourceProperties;
            seedController.DrawProperties();
        }


        private void DrawAndSetMaxPasses()
        {
            forceMaxPasses = EditorGUILayout.Toggle("Force Max passes:", forceMaxPasses);
            int minPasses = seedController.GetMinPasses();
            if (forceMaxPasses)
            {
                maxPasses = EditorGUILayout.IntField($"Max passes :", maxPasses);
                if (maxPasses < minPasses)
                {
                    EditorGUILayout.HelpBox($"The algorithm must run with at least {minPasses} passes", MessageType.Warning);
                }
            }
            else
            {
                maxPasses = minPasses;
            }
        }

        private Texture2D LoadTexture(int number)
        {
            var path = Directory.GetFiles(JFAConfig.SavePath, "*.png", SearchOption.AllDirectories).OrderBy(x => x).ToList()[number];
            path = Path.Combine(JFAConfig.SavePath, path);

            Texture2D tex;
            if (loadedTextures.TryGetValue(path, out tex))
            {
                tex.filterMode = FilterMode.Point;
                return tex;
            }

            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(Path.Combine(JFAConfig.SavePath, path));
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
            seedController.PaintSeeds();

            JFA = new JumpFloodAlgorithmTex(seedController.Seed, _config);
            JFA.Compute();
        }
    }
}
