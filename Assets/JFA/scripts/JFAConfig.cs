using System;
using UnityEngine;

[Serializable]
public class JFAConfig : UnityEngine.Object
{
    public static string SaveFileName = "Result_{0:00}";
    public static string SavePath => $"{Application.dataPath}/JFA/Results";
    [SerializeField] private int maxPasses;
    [SerializeField] private bool forceMaxPasses;
    [SerializeField] private bool recordProcess;

    public int MaxPasses
    {
        get => maxPasses;
        set => maxPasses = value;
    }

    public bool ForceMaxPasses
    {
        get => forceMaxPasses;
        set => forceMaxPasses = value;
    }

    public bool RecordProcess
    {
        get => recordProcess;
        set => recordProcess = value;
    }
}
