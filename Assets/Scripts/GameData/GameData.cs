using MemoryPack;
using System;

[Serializable]
[MemoryPackable]
public partial class GameData
{
    [MemoryPackOrder(0)]
    public string GameName { get; set; } = string.Empty; // Tên game (ví dụ: DefendersOfTheDawn)

    [MemoryPackOrder(1)]
    public string PlayerId { get; set; } = string.Empty;

    [MemoryPackOrder(2)]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    [MemoryPackOrder(3)]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// Request Data cho các phương thức khác
[Serializable]
[MemoryPackable]
public partial class GetRequest
{
    [MemoryPackOrder(0)]
    public string GameName ;
    [MemoryPackOrder(1)]
    public string PlayerId ;

    public GetRequest(string gameName, string playerId)
    {
        GameName = gameName;
        PlayerId = playerId;
    }
}

[Serializable]
[MemoryPackable]
public partial class DeleteRequest
{
    [MemoryPackOrder(0)]
    public string GameName ;
    [MemoryPackOrder(1)]
    public string PlayerId ;
    [MemoryPackOrder(2)]
    public string Password ;

    public DeleteRequest(string gameName, string playerId, string password)
    {
        GameName = gameName;
        PlayerId = playerId;
        Password = password;
    }
}
