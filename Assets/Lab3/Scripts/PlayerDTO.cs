using System;
using System.Collections.Generic;

[Serializable]
public struct PlayerDTO
{
    public string playerName;
    public string playerRole; 
}

[Serializable]
public struct PlayerDTO2
{
    public string playerName;
    public string playerRole; 
}

public enum MsgType : byte
{
    Text = 1,
    Player = 2,
    Transform = 3,
    Shoot = 4
}