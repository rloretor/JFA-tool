using System.IO;
using UnityEngine;

public static class Texture2DExtensions
{
    public static Texture2D SaveTextureAsPng(this Texture2D tex, string folderPath)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes($"{folderPath}/{tex.name}_tex.png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + folderPath);
        return tex;
    }
}
