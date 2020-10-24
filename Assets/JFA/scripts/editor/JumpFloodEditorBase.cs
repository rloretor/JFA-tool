using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JFA.scripts.editor
{
    public abstract class JumpFloodEditorBase : EditorWindow
    {
        protected bool shouldSaveProcess;
        protected int savedTextureCount = 0;

        protected virtual void OnGUI()
        {
            EditorGUILayout.LabelField("JFA parameters");
            EditorUtils.DrawUILine(Color.black, 8);
            DrawProperties();
            EditorUtils.DrawUILine(Color.black, 8);
            EditorGUILayout.Space();
            shouldSaveProcess = EditorGUILayout.Toggle("Save JFA Process:", shouldSaveProcess);
            InitJFAConfig();
            EditorUtils.DrawUILine(Color.black, 8);
            savedTextureCount = Directory.GetFiles(JFAConfig.SavePath, "*.png", SearchOption.AllDirectories).Length;
            DrawJFAButton();
            DrawInteractiveArea();
        }

        protected abstract void DrawProperties();
        protected abstract void InitJFAConfig();
        protected abstract void DrawJFAButton();
        protected abstract void DrawInteractiveArea();
    }
}
