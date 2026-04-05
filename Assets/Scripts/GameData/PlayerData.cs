using MemoryPack;
using System;

[Serializable]
[MemoryPackable]
public partial class PlayerData
{
    public string PlayerID;
    public string NickName;
    public string CreateTime;
    public string LastUpdateTime;
}
