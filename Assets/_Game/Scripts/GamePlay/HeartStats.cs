using UnityEngine;

public enum HeartType
{
    Pink,
    LightBlue,
    Orange,
    Purple, 
    LightGreen,
    Yellow,
    Blue,
    Red, 
    Black, 
    Grey, 
    Green,
    PaleYellow,
    DarkRed,
    Brown, 
    Mint, 
    lightRed, 
    LightPurple,
    DrarkBlue,
    LightPink,
    LightGreen_2,
    LightPink_2,
    CharcoalBlue,
    LightYellow,
    EarthyRed,
    DarkYellow
}

[DisallowMultipleComponent]
public class HeartStats : MonoBehaviour
{
    [Header("Loại Heart")]
    public HeartType type;

    [Header("Trọng số (ưu tiên làm Leader)")]
    public int weight = 1;

    [Header("Giá trị tiền của Heart ($)")]
    public int moneyValue = 10;

    [Header("VFX khi va chạm Gate")]
    public GameObject gateHitVFX;

    [Header("Prefab khi merge 3 tim cùng loại này")]
    public GameObject mergeResultPrefab;
}
