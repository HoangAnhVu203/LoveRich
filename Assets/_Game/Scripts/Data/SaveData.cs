using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // Wallets
    public long money;
    public long rose;

    // Road upgrade store
    public int unlockedRoadCount;
    public int roadUpgradeCount;

    // Current road
    public int currentRoadIndex;

    // Road gate counts (per road)
    public List<RoadGateCountSave> roadGateCounts = new();

    // Chain state
    public float leaderDistance;
    public bool reverseDirection;

    // Hearts in chain (store by level)
    public List<int> heartLevels = new() ;

    // Gates snapshot
    public List<GateSave> gates = new List<GateSave>(); 

    // Gate cost store
    public int gatePurchasedCount;
    public long gateLastCost;

    // Action cost store
    public long addCost;
    public long mergeCost;
}

[Serializable]
public class RoadGateCountSave
{
    public int roadIndex;
    public int count;
}

[Serializable]
public class GateSave
{
    public int roadIndex;   
    public float ratio;     
    public int girlIndex;   
}
