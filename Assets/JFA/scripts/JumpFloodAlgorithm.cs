using System;
using UnityEngine;

[Serializable]
public class JumpFloodAlgorithmBase<T>
    where T : Texture
{
    public T SeedTexture => seedTexture;

    protected Material JFAMat;
    protected Shader JFA;

    protected T seedTexture;
    private int passID = Shader.PropertyToID("_pass");
    private int maxPassID = Shader.PropertyToID("_maxPasses");
    protected int passes;
    protected JFAConfig config;
    protected bool recordProcess => config.RecordProcess;
    protected bool forceMaxPasses => config.ForceMaxPasses;
    protected int maxPasses => config.MaxPasses;


    public int Passes => passes;

    public JumpFloodAlgorithmBase(T seedTexture, JFAConfig configParameters)
    {
        this.seedTexture = seedTexture;
        this.config = configParameters;
        ComputePasses();
        InitMaterial();
    }

    public virtual void Compute()
    {
        var Height = seedTexture.height;
        var Width = seedTexture.width;
        var source = RenderTexture.GetTemporary(Width, Height, 0);
        var dest = RenderTexture.GetTemporary(Width, Height, 0);
        Shader.SetGlobalInt(maxPassID, passes);

        Graphics.Blit(seedTexture, source);
        for (int passNumber = 0; passNumber <= passes; passNumber++)
        {
            Pass(passNumber, source, dest);
            (source, dest) = (dest, source);
            Debug.Log("swap " + passNumber);
        }

        RenderTexture.active = null;
        source.Release();
        dest.Release();
    }

    protected virtual void Pass(int pass, RenderTexture sourceId, RenderTexture destId)
    {
        Shader.SetGlobalInt(passID, pass);
        Graphics.Blit(sourceId, destId, JFAMat);
    }

    private void InitMaterial()
    {
        JFA = Shader.Find("JFA");
        JFAMat = new Material(JFA)
        {
            name = "JFAMat",
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private void ComputePasses()
    {
        var Height = seedTexture.height;
        var Width = seedTexture.width;
        passes = Mathf.CeilToInt(Mathf.Log(Mathf.Max(Width, Height), 2f));

        if (forceMaxPasses)
        {
            passes = maxPasses;
        }
        else
        {
            if (passes > maxPasses)
            {
                Debug.LogWarning($"The algorithm needs to run with {passes}, not using {maxPasses}");
            }
        }
    }
}
