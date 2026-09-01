using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using System.Collections;
using System.Collections.Generic;
using Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

public class MultiRoomNetworkManager : MonoBehaviour
{
    public static MultiRoomNetworkManager Instance;

    [Header("Room Scene Settings")]
    public LocalPhysicsMode roomPhysicsMode = LocalPhysicsMode.None;

    [HideInInspector]
    public List<RoomInfo> rooms = new List<RoomInfo>();

    public HashSet<int> roomIds = new HashSet<int>();

    [System.Serializable]
    public class RoomInfo
    {
        public string roomName;
        public string roomData;
        public string sceneName;

        public int roomID;
        public int currentPlayers;
        public int maxPlayers;

        public RoomVisibility visibility;

        // This is the permanent RoomMenu scene used by the room/lobby manager.
        public Scene scene;

        // Only connections that belong to this room.
        public List<NetworkConnection> playerConnections =
            new List<NetworkConnection>();
    }

    private readonly Dictionary<NetworkConnection, RoomInfo> connectionToRoom =
        new Dictionary<NetworkConnection, RoomInfo>();

    // O(1) lookups so create/join don't have to scan the whole room list.
    // 'rooms' (above) is kept around only to preserve stable ordering for OnRoomListRequest.
    private readonly Dictionary<int, RoomInfo> roomsById =
        new Dictionary<int, RoomInfo>();
    private readonly Dictionary<string, RoomInfo> roomsByName =
        new Dictionary<string, RoomInfo>();

    // Cache so FindLobbyGameManager only has to do the expensive
    // FindObjectsByType scan once per unique scene, not once per join.
    private readonly Dictionary<Scene, LobbyGameManager> lobbyManagersByScene =
        new Dictionary<Scene, LobbyGameManager>();

    private bool creatingRoom;

    public List<CreateRoomRequest> createRoomRequestQueue =
        new List<CreateRoomRequest>();

    public class CreateRoomRequest
    {
        public NetworkConnection conn;
        public CreateRoomMessage msg;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InstanceFinder.ServerManager.RegisterBroadcast<RoomListRequestMessage>(
            OnRoomListRequest);

        InstanceFinder.ServerManager.RegisterBroadcast<CreateRoomMessage>(
            OnCreateRoom);

        InstanceFinder.ServerManager.RegisterBroadcast<JoinRoomMessage>(
            OnJoinRoom);

        InstanceFinder.ServerManager.OnRemoteConnectionState +=
            ServerManager_OnRemoteConnectionState;

        InstanceFinder.ClientManager.OnClientConnectionState +=
            ClientManager_OnClientConnectionState;
        
        InstanceFinder.SceneManager.OnLoadEnd +=
            SceneManager_OnLoadEnd;

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.UnregisterBroadcast<RoomListRequestMessage>(
                OnRoomListRequest);

            InstanceFinder.ServerManager.UnregisterBroadcast<CreateRoomMessage>(
                OnCreateRoom);

            InstanceFinder.ServerManager.UnregisterBroadcast<JoinRoomMessage>(
                OnJoinRoom);

            InstanceFinder.ServerManager.OnRemoteConnectionState -=
                ServerManager_OnRemoteConnectionState;
            InstanceFinder.SceneManager.OnLoadEnd -=
                SceneManager_OnLoadEnd;
        }

        if (InstanceFinder.ClientManager != null)
        {
            InstanceFinder.ClientManager.OnClientConnectionState -=
                ClientManager_OnClientConnectionState;
        }
    }

   

    private void Update()
    {
        if (createRoomRequestQueue.Count == 0 || creatingRoom)
            return;

        CreateRoomRequest request = createRoomRequestQueue[0];
        createRoomRequestQueue.RemoveAt(0);

        if (request.conn == null)
            return;

        if (!InstanceFinder.ServerManager.Clients.ContainsKey(
                request.conn.ClientId))
            return;

        StartCoroutine(
            CreateRoomCoroutine(
                request.conn,
                request.msg));
    }

    // ============================================================
    // CONNECTION STATE
    // ============================================================

    private void ServerManager_OnRemoteConnectionState(
        NetworkConnection conn,
        RemoteConnectionStateArgs state)
    {
        if (state.ConnectionState != RemoteConnectionState.Stopped)
            return;

        ServerRemoveConnectionFromRoom(conn);
    }

    private void ClientManager_OnClientConnectionState(
        ClientConnectionStateArgs state)
    {
        if (state.ConnectionState != LocalConnectionState.Stopped )
            return;

        SceneManager.LoadScene(0, LoadSceneMode.Single);
        Destroy(gameObject);
    }

    // ============================================================
    // ROOM LIST
    // ============================================================

    private void OnRoomListRequest(
        NetworkConnection conn,
        RoomListRequestMessage msg,
        Channel channel)
    {
        int count = rooms.Count;

        RoomListResponseMessage response =
            new RoomListResponseMessage
            {
                roomNames = new string[count],
                roomDatas = new string[count],
                sceneNames = new string[count],
                currentCounts = new int[count],
                maxCounts = new int[count],
                Roomvisibility = new RoomVisibility[count]
            };

        for (int i = 0; i < count; i++)
        {
            RoomInfo room = rooms[i];

            response.roomNames[i] = room.roomName;
            response.roomDatas[i] = room.roomData;
            response.sceneNames[i] = room.sceneName;
            response.currentCounts[i] = room.currentPlayers;
            response.maxCounts[i] = room.maxPlayers;
            response.Roomvisibility[i] = room.visibility;
        }

        conn.Broadcast(response);
    }

    // ============================================================
    // CREATE ROOM
    // ============================================================

    private void OnCreateRoom(
        NetworkConnection conn,
        CreateRoomMessage msg,
        Channel channel)
    {
        if (conn == null)
            return;

        if (connectionToRoom.ContainsKey(conn))
        {
            Debug.LogWarning(
                $"[Server] Client {conn.ClientId} is already in a room.");
            return;
        }

        if (roomsByName.ContainsKey(msg.roomName))
        {
            Debug.LogWarning(
                $"[Server] Room '{msg.roomName}' already exists.");
            return;
        }

        createRoomRequestQueue.Add(
            new CreateRoomRequest
            {
                conn = conn,
                msg = msg
            });
    }

    private IEnumerator CreateRoomCoroutine(
        NetworkConnection conn,
        CreateRoomMessage msg)
    {
        creatingRoom = true;

        /*
         * RoomMenu is intentionally permanent.
         *
         * The room object points to the scene containing
         * LobbyGameManager. It is NOT the game scene.
         */
        Scene roomMenuScene = SceneManager.GetActiveScene();

        if (!roomMenuScene.IsValid())
        {
            Debug.LogError(
                "[Server] Active RoomMenu scene is invalid.");

            creatingRoom = false;
            yield break;
        }

        if (conn == null ||
            !InstanceFinder.ServerManager.Clients.ContainsKey(
                conn.ClientId))
        {
            creatingRoom = false;
            yield break;
        }

        RoomInfo room = new RoomInfo
        {
            roomName = msg.roomName,
            roomData = msg.roomData,
            sceneName = msg.sceneName,
            currentPlayers = 0,
            maxPlayers = Mathf.Max(1, msg.maxPlayers),
            visibility = msg.Roomvisibility,
            roomID = GenerateUniqueRoomId(),

            // Permanent room/lobby management scene.
            scene = roomMenuScene,

            playerConnections =
                new List<NetworkConnection>()
        };

        rooms.Add(room);
        roomsById[room.roomID] = room;
        roomsByName[room.roomName] = room;

        AddConnectionToRoom(conn, room);

        Debug.Log(
            $"[Server] Created room {room.roomID} " +
            $"('{room.roomName}') for Client {conn.ClientId}.");

        yield return null;

        creatingRoom = false;
    }

    // ============================================================
    // JOIN ROOM
    // ============================================================

    private void OnJoinRoom(
        NetworkConnection conn,
        JoinRoomMessage msg,
        Channel channel)
    {
        if (conn == null)
            return;

        if (connectionToRoom.ContainsKey(conn))
        {
            Debug.LogWarning(
                $"[Server] Client {conn.ClientId} is already in a room.");
            return;
        }

        RoomInfo room = null;

        /*
         * Room-code joins intentionally ignore visibility.
         * A private room can be joined with its correct room ID.
         */
        if (msg.isRoomidavailable)
        {
            roomsById.TryGetValue(msg.roomID, out room);
        }
        else if (!string.IsNullOrWhiteSpace(msg.roomName))
        {
            roomsByName.TryGetValue(msg.roomName, out room);
        }

        if (room == null)
        {
            Debug.LogWarning(
                $"[Server] Room not found. " +
                $"ID={msg.roomID}, Name={msg.roomName}");
            return;
        }

        if (room.currentPlayers >= room.maxPlayers)
        {
            Debug.LogWarning(
                $"[Server] Room {room.roomID} is full.");
            return;
        }

        AddConnectionToRoom(conn, room);
    }

    // ============================================================
    // ADD CONNECTION TO ROOM
    // ============================================================

    private void  AddConnectionToRoom(
        NetworkConnection conn,
        RoomInfo room)
    {
        if (conn == null || room == null)
            return;

        if (connectionToRoom.ContainsKey(conn))
            return;

        connectionToRoom.Add(conn, room);

        if (!room.playerConnections.Contains(conn))
            room.playerConnections.Add(conn);

        room.currentPlayers =
            room.playerConnections.Count;

        /*
         * RoomMenu is the permanent scene.
         *
         * The connection must observe this scene so the
         * LobbyGameManager NetworkObject can communicate with it.
         */
        if (!conn.Scenes.Contains(room.scene))
        {
            InstanceFinder.SceneManager.AddConnectionToScene(
                conn,
                room.scene);
        }

        Debug.Log(
            $"[Server] Client {conn.ClientId} entered room " +
            $"{room.roomID} ({room.roomName}).");

        LobbyGameManager lobby =
            FindLobbyGameManager(room.scene);

        if (lobby == null)
        {
            Debug.LogError(
                $"[Server] LobbyGameManager was not found " +
                $"in scene '{room.scene.name}'.");
            return;
        }

        /*
         * ServerEnterLobby uses EnterLobbyMessage.
         * It does NOT use TargetRpc.
         */
        lobby.ServerEnterLobby(
            conn,
            room.roomID,
            room.roomName);
    }

    private LobbyGameManager FindLobbyGameManager(Scene scene)
    {
        // Fast path: already found this scene's LobbyGameManager before.
        if (lobbyManagersByScene.TryGetValue(scene, out LobbyGameManager cached)
            && cached != null)
        {
            return cached;
        }

        // Slow path: only runs once per unique scene (in practice, once
        // total, since RoomMenu is a single persistent scene), instead
        // of once per join.
        LobbyGameManager[] lobbies =
            FindObjectsByType<LobbyGameManager>(
                FindObjectsSortMode.None);

        foreach (LobbyGameManager lobby in lobbies)
        {
            if (lobby == null)
                continue;

            if (lobby.gameObject.scene == scene)
            {
                lobbyManagersByScene[scene] = lobby;
                return lobby;
            }
        }

        return null;
    }

    // ============================================================
    // START GAME FOR ONE ROOM
    // ============================================================

    public void StartRoomGame(int roomId)
    {
        if (!TryGetRoom(roomId, out RoomInfo room))
            return;

        if (room.playerConnections.Count == 0)
            return;

        List<NetworkConnection> connections = new();

        foreach (NetworkConnection conn in room.playerConnections)
        {
            if (conn == null)
                continue;

            if (!TryGetRoom(
                    conn,
                    out RoomInfo connectionRoom))
                continue;

            if (connectionRoom.roomID != roomId)
                continue;

            connections.Add(conn);
        }

        if (connections.Count == 0)
            return;

        SceneLoadData sceneLoadData =
            new SceneLoadData(room.sceneName);

        sceneLoadData.ReplaceScenes =
            ReplaceOption.None;

        sceneLoadData.Options.AllowStacking =
            true;

        sceneLoadData.Options.LocalPhysics =
            roomPhysicsMode;

        // Pass the room ID to the server-side scene instance.
        sceneLoadData.Params.ServerParams =
            new object[]
            {
                roomId
            };

        InstanceFinder.SceneManager.LoadConnectionScenes(
            connections.ToArray(),
            sceneLoadData);
    }
    
    private void SceneManager_OnLoadEnd(
        SceneLoadEndEventArgs args)
    {
        if (args.LoadedScenes == null ||
            args.LoadedScenes.Length == 0)
            return;

        object[] serverParams =
            args.QueueData.SceneLoadData.Params.ServerParams;

        if (serverParams == null ||
            serverParams.Length == 0)
            return;

        if (serverParams[0] is not int roomId)
            return;

        Debug.Log(
            $"[Server] GameScene loaded for Room {roomId}."
        );

        foreach (Scene scene in args.LoadedScenes)
        {
            RoomPlayerSpawner spawner =
                FindGameSceneSpawner(scene);

            if (spawner == null)
            {
                Debug.LogWarning(
                    $"[Server] GameSceneSpawner not found in " +
                    $"scene '{scene.name}'."
                );

                continue;
            }

            spawner.Initialize(
                roomId,
                scene
            );

            Debug.Log(
                $"[Server] GameSceneSpawner initialized " +
                $"for Room {roomId}."
            );
        }
    }
    private RoomPlayerSpawner FindGameSceneSpawner(
        Scene scene)
    {
        RoomPlayerSpawner[] spawners =
            FindObjectsByType<RoomPlayerSpawner>(
                FindObjectsSortMode.None);

        foreach (RoomPlayerSpawner spawner in spawners)
        {
            if (spawner == null)
                continue;

            if (spawner.gameObject.scene == scene)
                return spawner;
        }

        return null;
    }

    // ============================================================
    // ROOM LOOKUP
    // ============================================================

    public bool TryGetRoom(
        NetworkConnection conn,
        out RoomInfo info)
    {
        return connectionToRoom.TryGetValue(
            conn,
            out info);
    }

    public bool TryGetRoom(
        int roomId,
        out RoomInfo info)
    {
        return roomsById.TryGetValue(roomId, out info);
    }

    // ============================================================
    // REMOVE CONNECTION
    // ============================================================

    public void ServerRemoveConnectionFromRoom(
        NetworkConnection conn)
    {
        if (conn == null)
            return;

        if (!connectionToRoom.TryGetValue(
                conn,
                out RoomInfo room))
            return;

        room.playerConnections.Remove(conn);

        room.currentPlayers =
            room.playerConnections.Count;

        connectionToRoom.Remove(conn);

        Debug.Log(
            $"[Server] Client {conn.ClientId} left room " +
            $"{room.roomID}.");

        if (room.currentPlayers <= 0)
        {
            roomIds.Remove(room.roomID);
            rooms.Remove(room);
            roomsById.Remove(room.roomID);
            roomsByName.Remove(room.roomName);

            Debug.Log(
                $"[Server] Room {room.roomID} removed.");
        }
    }

    // ============================================================
    // ROOM ID
    // ============================================================

    public int GenerateUniqueRoomId()
    {
        int roomId;

        do
        {
            roomId =
                Random.Range(10000, 99999);
        }
        while (roomIds.Contains(roomId));

        roomIds.Add(roomId);

        return roomId;
    }
}