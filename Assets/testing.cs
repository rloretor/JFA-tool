using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class testing : MonoBehaviour
{
    public int SeedCount = 0;
    public JFAConfig Config;
    private JumpFloodAlgorithmTex JFA_tex = null;


    [ContextMenu("ComputeJFA")]
    public void Compute()
    {
        Texture2D Seed = new Texture2D(Camera.main.pixelWidth, Camera.main.pixelHeight, TextureFormat.RGB24, false)
        {
            name = "Seed",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] colors = new Color [Seed.width * Seed.height];
        for (var index = 0; index < colors.Length; index++)
        {
            colors[index] = new Color(0, 0, 0, 0);
        }

        Random.InitState(System.DateTime.Now.Second);
        for (int i = 0; i < SeedCount; i++)
        {
            var randPix = new Vector2Int();
            randPix.x = Random.Range(0, Seed.height);
            randPix.y = Random.Range(0, Seed.width);
            int pixi = randPix.x * Seed.width + randPix.y;
            colors[pixi] = new Color((float) randPix.x / Seed.height, (float) randPix.y / Seed.width, 0, 0);
        }

        Seed.SetPixels(colors);
        Seed.Apply(false);

        JFA_tex = new JumpFloodAlgorithmTex(Seed, Config);


        JFA_tex.Compute();
    }
}
