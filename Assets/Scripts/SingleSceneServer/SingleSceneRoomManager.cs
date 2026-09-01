// SingleSceneRoomManager.cs

using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace singleSceneServer
{
    public class SingleSceneRoomManager : NetworkBehaviour
    {
        public static SingleSceneRoomManager Instance;

        [Header("Room Settings")] public int maxRooms = 50;
        public int maxPlayersPerRoom = 8;
        public Vector2 roomSpacing = new Vector2(50f, 50f);
        public GameObject roomBoundaryPrefab; // Visual boundary for each room zone

        [Header("Spawn Settings")] public Transform[] spawnPoints; // Pre-placed spawn points
        public int spawnPointsPerRoom = 4;

        [System.Serializable]
        public class RoomData
        {
            public int roomId;
            public string roomName;
            public string sceneName;
            public int currentPlayers;
            public int maxPlayers;
            public RoomVisibility visibility;
            public Vector3 roomCenter;
            public List<NetworkConnection> playerConnections = new List<NetworkConnection>();
            public GameObject roomBoundary; // Visual boundary
            public List<Vector3> spawnPositions = new List<Vector3>();
            public bool isGameRunning;
            public float gameStartTime;
        }

        private Dictionary<int, RoomData> rooms = new Dictionary<int, RoomData>();

        private Dictionary<NetworkConnection, RoomData>
            connectionToRoom = new Dictionary<NetworkConnection, RoomData>();

        private HashSet<int> roomIds = new HashSet<int>();
        private Queue<CreateRoomRequest> createRoomRequestQueue = new Queue<CreateRoomRequest>();

        public class CreateRoomRequest
        {
            public NetworkConnection conn;
            public RoomMessages.CreateRoomMessage msg;
        }

        private bool creatingRoom;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            InstanceFinder.ServerManager.RegisterBroadcast<RoomMessages.RoomListResponseMessage>(OnRoomListRequest);
            InstanceFinder.ServerManager.RegisterBroadcast<RoomMessages.CreateRoomMessage>(OnCreateRoom);
            InstanceFinder.ServerManager.RegisterBroadcast<RoomMessages.JoinRoomMessage>(OnJoinRoom);
            InstanceFinder.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.UnregisterBroadcast<RoomMessages.RoomListResponseMessage>(
                    OnRoomListRequest);
                InstanceFinder.ServerManager.UnregisterBroadcast<RoomMessages.CreateRoomMessage>(OnCreateRoom);
                InstanceFinder.ServerManager.UnregisterBroadcast<RoomMessages.JoinRoomMessage>(OnJoinRoom);
                InstanceFinder.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
            }
        }

        private void Update()
        {
            if (createRoomRequestQueue.Count == 0 || creatingRoom)
                return;

            CreateRoomRequest request = createRoomRequestQueue.Dequeue();

            if (request?.conn == null || !InstanceFinder.ServerManager.Clients.ContainsKey(request.conn.ClientId))
                return;

            StartCoroutine(CreateRoomCoroutine(request.conn, request.msg));
        }

        // ============================================================
        // CONNECTION STATE
        // ============================================================

        private void ServerManager_OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs state)
        {
            if (state.ConnectionState != RemoteConnectionState.Stopped)
                return;

            ServerRemoveConnectionFromRoom(conn);
        }

        // ============================================================
        // ROOM LIST
        // ============================================================

        private void OnRoomListRequest(NetworkConnection conn, RoomMessages.RoomListResponseMessage msg,
            Channel channel)
        {
            var roomList = new List<RoomData>();
            foreach (var room in rooms.Values)
            {
                if (room.visibility == RoomVisibility.Public)
                    roomList.Add(room);
            }

            int count = roomList.Count;
            RoomMessages.RoomListResponseMessage response = new RoomMessages.RoomListResponseMessage
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
                var room = roomList[i];
                response.roomNames[i] = room.roomName;
                response.roomDatas[i] = "";
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

        private void OnCreateRoom(NetworkConnection conn, RoomMessages.CreateRoomMessage msg, Channel channel)
        {
            if (conn == null || connectionToRoom.ContainsKey(conn))
            {
                Debug.LogWarning($"[Server] Client {conn?.ClientId} is already in a room.");
                return;
            }

            if (rooms.Count >= maxRooms)
            {
                Debug.LogWarning($"[Server] Max rooms reached ({maxRooms})");
                return;
            }

            foreach (var room in rooms.Values)
            {
                if (room.roomName == msg.roomName)
                {
                    Debug.LogWarning($"[Server] Room '{msg.roomName}' already exists.");
                    return;
                }
            }

            createRoomRequestQueue.Enqueue(new CreateRoomRequest { conn = conn, msg = msg });
        }

        public void FindSpawnPointsInGameWorld()
        {
            // Find all GameObjects with tag "SpawnPoint"
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

            if (spawnPointObjects.Length == 0)
            {
                Debug.LogWarning(
                    "No spawn points found! Make sure GameWorld scene is loaded and spawn points have 'SpawnPoint' tag.");
                return;
            }

            // Convert to Transforms
            List<Transform> spawnTransforms = new List<Transform>();
            foreach (GameObject obj in spawnPointObjects)
            {
                spawnTransforms.Add(obj.transform);
            }

            // Assign to spawnPoints array
            spawnPoints = spawnTransforms.ToArray();
            Debug.Log($"Found {spawnPoints.Length} spawn points in GameWorld");
        }

// Call this after GameWorld loads
        private void OnGameWorldLoaded()
        {
            FindSpawnPointsInGameWorld();
        }

        private System.Collections.IEnumerator CreateRoomCoroutine(NetworkConnection conn,
            RoomMessages.CreateRoomMessage msg)
        {
            creatingRoom = true;

            int roomId = GenerateUniqueRoomId();
            Vector3 roomCenter = GetNextRoomPosition();

            RoomData room = new RoomData
            {
                roomId = roomId,
                roomName = msg.roomName,
                sceneName = msg.sceneName,
                currentPlayers = 0,
                maxPlayers = Mathf.Max(1, msg.maxPlayers),
                visibility = msg.Roomvisibility,
                roomCenter = roomCenter,
                playerConnections = new List<NetworkConnection>(),
                spawnPositions = GenerateSpawnPositions(roomCenter),
                isGameRunning = false
            };

            // Create visual boundary for the room zone
            if (roomBoundaryPrefab != null)
            {
                room.roomBoundary = Instantiate(roomBoundaryPrefab, roomCenter, Quaternion.identity);
                room.roomBoundary.name = $"RoomBoundary_{roomId}";

                // Scale the boundary based on room size
                float boundarySize = Mathf.Sqrt(room.maxPlayers) * 10f;
                room.roomBoundary.transform.localScale = new Vector3(boundarySize, 1, boundarySize);
            }

            rooms.Add(roomId, room);
            AddConnectionToRoom(conn, room);

            Debug.Log($"[Server] Created room {roomId} ('{room.roomName}') at position {roomCenter}");

            yield return null;
            creatingRoom = false;
        }

        // ============================================================
        // JOIN ROOM
        // ============================================================

        private void OnJoinRoom(NetworkConnection conn, RoomMessages.JoinRoomMessage msg, Channel channel)
        {
            if (conn == null || connectionToRoom.ContainsKey(conn))
                return;

            RoomData room = null;

            if (msg.isRoomidavailable)
            {
                rooms.TryGetValue(msg.roomID, out room);
            }
            else if (!string.IsNullOrWhiteSpace(msg.roomName))
            {
                foreach (var r in rooms.Values)
                {
                    if (r.roomName == msg.roomName)
                    {
                        room = r;
                        break;
                    }
                }
            }

            if (room == null)
            {
                Debug.LogWarning($"[Server] Room not found. ID={msg.roomID}, Name={msg.roomName}");
                return;
            }

            if (room.currentPlayers >= room.maxPlayers)
            {
                Debug.LogWarning($"[Server] Room {room.roomId} is full.");
                return;
            }

            if (room.isGameRunning)
            {
                Debug.LogWarning($"[Server] Room {room.roomId} game is already running.");
                return;
            }

            AddConnectionToRoom(conn, room);
        }

        // ============================================================
// ADD CONNECTION TO ROOM
// ============================================================

        private void AddConnectionToRoom(NetworkConnection conn, RoomData room)
        {
            if (conn == null || room == null || connectionToRoom.ContainsKey(conn))
                return;

            connectionToRoom.Add(conn, room);
            room.playerConnections.Add(conn);
            room.currentPlayers = room.playerConnections.Count;

            // ============================================================
            // BROADCAST EnterLobbyMessage to the client
            // ============================================================
            conn.Broadcast(new RoomMessages.EnterLobbyMessage
            {
                roomId = room.roomId,
                roomName = room.roomName
            });

            Debug.Log($"Broadcasted EnterLobbyMessage to client {conn.ClientId} (Room: {room.roomId})");

            // Spawn the player in the room zone
            SpawnPlayerInRoom(conn, room);

            Debug.Log($"[Server] Client {conn.ClientId} entered room {room.roomId} ({room.roomName})");
        }

        private void SpawnPlayerInRoom(NetworkConnection conn, RoomData room)
        {
            // Get spawn position for this player
            Vector3 spawnPos = GetNextSpawnPosition(room);

            // Spawn the player object in the world
            // You'll need to implement this based on your game's player spawning system
            // Example:
            // GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            // NetworkManager.ServerManager.Spawn(playerObj, conn);

            // Notify client about their spawn position
            TargetSetSpawnPosition(conn, spawnPos, room.roomId, room.roomName);
        }

        [TargetRpc]
        private void TargetSetSpawnPosition(NetworkConnection target, Vector3 spawnPos, int roomId, string roomName)
        {
            // Client receives spawn position
            // You can use this to position the player or show room info
            Debug.Log($"Spawned at {spawnPos} in room {roomId}");

            // Find LobbyGameManager and set room info
            var lobby = FindObjectOfType<LobbyGameManager>();
            if (lobby != null)
            {
                lobby.SetRoomInfo(roomId, roomName);
            }
        }

        private Vector3 GetNextSpawnPosition(RoomData room)
        {
            // Return next available spawn point
            int usedSpawns = room.playerConnections.Count - 1;
            if (usedSpawns < room.spawnPositions.Count)
            {
                return room.spawnPositions[usedSpawns];
            }
            else
            {
                // If we need more spawns than pre-generated, create new ones
                return room.roomCenter + new Vector3(
                    Random.Range(-10f, 10f),
                    0,
                    Random.Range(-10f, 10f)
                );
            }
        }

        // ============================================================
        // ROOM POSITIONING
        // ============================================================

        private Vector3 GetNextRoomPosition()
        {
            int roomCount = rooms.Count;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(maxRooms));
            int row = roomCount / columns;
            int col = roomCount % columns;

            return new Vector3(
                col * roomSpacing.x,
                0,
                row * roomSpacing.y
            );
        }

        private List<Vector3> GenerateSpawnPositions(Vector3 center)
        {
            List<Vector3> positions = new List<Vector3>();
            float radius = 8f;
            int count = spawnPointsPerRoom;

            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * 360f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                positions.Add(center + offset);
            }

            return positions;
        }

        // ============================================================
        // START GAME FOR ONE ROOM
        // ============================================================

        public void StartRoomGame(int roomId)
        {
            if (!rooms.TryGetValue(roomId, out RoomData room))
                return;

            if (room.isGameRunning)
                return;

            if (room.playerConnections.Count < 2)
            {
                Debug.LogWarning($"Room {roomId} needs at least 2 players to start");
                return;
            }

            room.isGameRunning = true;
            room.gameStartTime = Time.time;

            // Notify all players in room that game is starting
            foreach (var conn in room.playerConnections)
            {
                TargetGameStarted(conn, roomId);
            }

            Debug.Log($"Game started in room {roomId} with {room.playerConnections.Count} players");
        }

        [TargetRpc]
        private void TargetGameStarted(NetworkConnection target, int roomId)
        {
            // Client receives game start notification
            // Load the game scene or transition to game state
            Debug.Log($"Game started in room {roomId}");
        }

        // ============================================================
        // REMOVE CONNECTION
        // ============================================================

        public void ServerRemoveConnectionFromRoom(NetworkConnection conn)
        {
            if (conn == null || !connectionToRoom.TryGetValue(conn, out RoomData room))
                return;

            room.playerConnections.Remove(conn);
            room.currentPlayers = room.playerConnections.Count;
            connectionToRoom.Remove(conn);

            // Despawn the player object
            // You'll need to handle this based on your spawning system

            Debug.Log($"[Server] Client {conn.ClientId} left room {room.roomId}");

            if (room.currentPlayers <= 0)
            {
                // Clean up room
                if (room.roomBoundary != null)
                    Destroy(room.roomBoundary);

                rooms.Remove(room.roomId);
                roomIds.Remove(room.roomId);

                Debug.Log($"[Server] Room {room.roomId} removed.");
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
                roomId = Random.Range(10000, 99999);
            } while (roomIds.Contains(roomId));

            roomIds.Add(roomId);
            return roomId;
        }

        // ============================================================
        // UTILITY METHODS
        // ============================================================

        public bool TryGetRoom(int roomId, out RoomData room)
        {
            return rooms.TryGetValue(roomId, out room);
        }

        public bool TryGetRoom(NetworkConnection conn, out RoomData room)
        {
            return connectionToRoom.TryGetValue(conn, out room);
        }

        public List<RoomData> GetPublicRooms()
        {
            List<RoomData> publicRooms = new List<RoomData>();
            foreach (var room in rooms.Values)
            {
                if (room.visibility == RoomVisibility.Public)
                    publicRooms.Add(room);
            }

            return publicRooms;
        }
    }
}