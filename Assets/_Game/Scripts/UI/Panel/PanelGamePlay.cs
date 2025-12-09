using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelGamePlay : UICanvas
{
    public void AddHeartBTN()
    {
        HeartManager.Instance.AddHeart();
    }
}
