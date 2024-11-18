using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoadDirection
{
    None = 0,     // No connection
    Up = 1 << 0,  // 000000001
    Down = 1 << 1,  // 000000010
    Left = 1 << 2,  // 000000100
    Right = 1 << 3,  // 000001000
    UpLeft = 1 << 4,  // 000010000
    UpRight = 1 << 5,  // 000100000
    DownLeft = 1 << 6,  // 001000000
    DownRight = 1 << 7,  // 010000000
}
public class RoadNode
{
    
}
