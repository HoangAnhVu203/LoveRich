using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    public long money;
    public int currentRoadIndex;

    public List<HeartSaveData> hearts = new List<HeartSaveData>();
    public List<GateSaveData> gates = new List<GateSaveData>();
}

[System.Serializable]
public class HeartSaveData
{
    public int level;
    public HeartType type;
}

[System.Serializable]
public class GateSaveData
{
    public int roadIndex;
    public float ratio;
}
