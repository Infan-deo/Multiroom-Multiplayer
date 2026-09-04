using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct RoomInfo : IEvent
{
    public int roomId;
    public Scene gameScene;

    public RoomInfo(int roomId, Scene scene)
    {
        this.roomId = roomId;
        this.gameScene = scene;
    }
}

public struct clientIdEventbus : IEvent
{
    public int clientId;

    public clientIdEventbus(int clientId)
    {
        this.clientId = clientId;
    }
}

public class RoomPlayerSpawner : NetworkBehaviour
{
    [Header("Player")] [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawns")] [SerializeField] private Transform[] spawns;

    [Header("Room")] [SerializeField] private int roomId;

    private Scene gameScene;

    private bool initialized;

    public int RoomId => roomId;

    public WorldSpawnerServer worldSpawnerServer;

    public AllRoomPlayerManager allRoomPlayerManager;


    /// <summary>
    /// Called by MultiRoomNetworkManager after this stacked
    /// GameScene instance has been created for a room.
    /// </summary>
    [Server]
    public void Initialize(int id, Scene scene)
    {
        roomId = id;
        gameScene = scene;
        initialized = true;

        Debug.Log(
            $"[RoomPlayerSpawner] Initialized Room {roomId} " +
            $"in Scene {gameScene.name}."
        );
        EventBus<RoomInfo>.Raise(new RoomInfo(id, gameScene));
        worldSpawnerServer.SpawnWorldLocal(gameScene);
        SpawnRoomPlayers();
    }

    [Server]
    private void SpawnRoomPlayers()
    {
        if (playerPrefab == null)
        {
            Debug.LogError(
                $"[RoomPlayerSpawner] Player Prefab is not assigned for Room {roomId}."
            );
            return;
        }

        MultiRoomNetworkManager manager =
            MultiRoomNetworkManager.Instance;

        if (manager == null)
            return;

        if (!manager.TryGetRoom(
                roomId,
                out MultiRoomNetworkManager.RoomInfo room))
        {
            Debug.LogError(
                $"[RoomPlayerSpawner] Room {roomId} not found."
            );
            return;
        }

        for (int i = 0; i < room.playerConnections.Count; i++)
        {
            NetworkConnection connection =
                room.playerConnections[i];

            if (connection == null)
                continue;

            SpawnPlayer(connection, i);
        }

        allRoomPlayerManager?.NotifyAllPlayersSpawned();
        // allRoomPlayerManager?.OnAllPlayerSpawned?.Invoke();
    }

    [Server]
    private void SpawnPlayer(
        NetworkConnection connection,
        int playerIndex)
    {
        if (connection == null)
            return;

        if (connection.FirstObject != null)
        {
            Debug.LogWarning(
                $"[RoomPlayerSpawner] Client {connection.ClientId} " +
                "already has a player. Skipping."
            );

            return;
        }

        NetworkObject player = Instantiate(playerPrefab);

        if (player.TryGetComponent(out PlayerController playerController))
        {
            allRoomPlayerManager.RoomPlayerspPlayerControllers.Add(playerController);
            allRoomPlayerManager.roomPlayersinfo.Add(connection.ClientId, playerController.itsOwnInfo);
        }

        Transform spawnPoint = GetSpawnPoint(playerIndex);
        if (spawnPoint != null)
        {
            player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }

        InstanceFinder.ServerManager.Spawn(player.gameObject, connection, gameScene);

        // Now the object (and its nested GetPlayerInfo) is networked — safe to RPC
        if (playerController != null)
        {
            playerController.itsOwnInfo.SetClientInfo(connection.ClientId);
        }


        Debug.Log(
            $"[RoomPlayerSpawner] Room {roomId} -> " +
            $"Client {connection.ClientId} spawned in " +
            $"scene {gameScene.name}."
        );
    }


    private Transform GetSpawnPoint(int index)
    {
        if (spawns == null || spawns.Length == 0)
            return transform;

        return spawns[index % spawns.Length];
    }
}