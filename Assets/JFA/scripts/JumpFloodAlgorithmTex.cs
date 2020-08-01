using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class JumpFloodAlgorithmTex : JumpFloodAlgorithmBase<Texture2D>
{
    private string SaveFileName => JFAConfigParams.SaveFileName;

    public static string SavePath => JFAConfigParams.SavePath;

    public JumpFloodAlgorithmTex(Texture2D seedTexture, JFAConfigParams configParameters) : base(seedTexture, configParameters)
    {
        if (recordProcess)
        {
            seedTexture.name = string.Format(SaveFileName, "0");
            CreateDirectory();
            seedTexture.SaveTextureAsPng(SavePath);
        }
    }


    protected override void Pass(int pass, RenderTexture sourceId, RenderTexture destId)
    {
        base.Pass(pass, sourceId, destId);
        if (recordProcess)
        {
            Object.DestroyImmediate(GetTexture(pass, destId).SaveTextureAsPng(SavePath));
        }
    }

    private Texture2D GetTexture(int passNumber, RenderTexture source)
    {
        var passn = (int) Mathf.Clamp(passNumber, 0.0f, this.passes);
        var step = Mathf.FloorToInt(Mathf.Pow(2.0f, (passes - passn)));
        var Height = seedTexture.height;
        var Width = seedTexture.width;
        var processTexture = new Texture2D(Width, Height, TextureFormat.RGB24, false) {name = string.Format(SaveFileName, passNumber + 1 /*, step*/)};
        processTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        processTexture.Apply();
        return processTexture;
    }

    protected void CreateDirectory()
    {
        if (Directory.Exists(SavePath))
        {
            Directory.Delete(SavePath, true);
        }

        Directory.CreateDirectory(SavePath);
    }
}
