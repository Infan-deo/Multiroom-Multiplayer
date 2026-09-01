using FishNet.Broadcast;
using UnityEngine;

namespace singleSceneServer
{
    public struct RoomMessages : IBroadcast
    {
        // Add these to your message definitions
        public struct CreateRoomMessage : IBroadcast
        {
            public string roomName;
            public string sceneName;
            public int maxPlayers;
            public RoomVisibility Roomvisibility;
            public string playerName; // Add this field
        }

        public struct JoinRoomMessage : IBroadcast
        {
            public int roomID;
            public bool isRoomidavailable;
            public string roomName;
            public string playerName; // Add this field
        }

        public struct RoomCreatedMessage : IBroadcast
        {
            public int roomId;
            public string roomName;
        }

        public struct RoomJoinFailedMessage : IBroadcast
        {
            public string reason;
        }

        // Add to your RoomMessages class
        public struct GameStartedMessage : IBroadcast
        {
            public int roomId;
            public string roomName;
        }
        public struct EnterLobbyMessage : IBroadcast
        {
            public int roomId;
            public string roomName;
        }
// Update RoomListResponseMessage to include room IDs
        public struct RoomListResponseMessage : IBroadcast
        {
            public string[] roomNames;
            public string[] roomDatas;
            public string[] sceneNames;
            public int[] currentCounts;
            public int[] maxCounts;
            public RoomVisibility[] Roomvisibility;
            public int[] roomIds; // Add this field
        }
    }
}