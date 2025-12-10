using UnityEngine;

public enum HeartType
{
    Pink,
    LightBlue,
    Orange,
    Purple, 
    Green,
    Yellow,
    Blue,
    Red
}

[DisallowMultipleComponent]
public class HeartUnit : MonoBehaviour
{
    public HeartType type;
}
