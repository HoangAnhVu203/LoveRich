using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelGamePlay : UICanvas
{
    [System.Obsolete]
    public void AddHeartBTN()
    {
        HeartManager.Instance.AddHeart();
    }

    [System.Obsolete]
    public void MergeHeartBTN()
    {
        HeartManager.Instance.MergeLast3Pink();
    }
}
