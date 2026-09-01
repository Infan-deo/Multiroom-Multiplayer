using FishNet.Broadcast;
public struct RoomListRequestMessage : IBroadcast { }
public struct RoomListResponseMessage : IBroadcast
{
    public string[] roomNames;
    public string[] roomDatas;
    public string[] sceneNames;
    public int[] currentCounts;
    public int[] maxCounts;
    public RoomVisibility[] Roomvisibility;
}

public struct CreateRoomMessage : IBroadcast
{
    public string roomName;
    public string roomData;
    public string sceneName;
    public int maxPlayers;
    public RoomVisibility Roomvisibility;
}

public struct JoinRoomMessage : IBroadcast
{
    public string roomName;
    public int roomID;
    public bool isRoomidavailable;
}

public struct EnterLobbyMessage : IBroadcast
{
    public int roomId;
    public string roomName;
}
