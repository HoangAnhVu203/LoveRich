using UnityEngine;

public enum HeartTypes
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
    Mint
}

[DisallowMultipleComponent]
public class HeartUnit : MonoBehaviour
{
    public HeartTypes type;
}
