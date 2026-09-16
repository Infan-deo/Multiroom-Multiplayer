using System.Collections;
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

    public SpectateSystem spectateSystem;


    /// <summary>
    /// Called by MultiRoomNetworkManager after this stacked
    /// GameScene instance has been created for a room.
    /// </summary>
    [Server]
    public IEnumerator Initialize(int id, Scene scene)
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
        yield return new WaitForSeconds(1.0f);
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
        int spawnIndex = 0;
        for (int i = 0; i < room.playerConnections.Count; i++)
        {
            NetworkConnection connection =
                room.playerConnections[i];

            if (connection == null)
                continue;
            if (spawnIndex >= spawns.Length)
            {
                spawnIndex = 0;
            }
            SpawnPlayer(connection, spawnIndex);
            
            spawnIndex++;
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
        Transform spawnPoint = spawns[playerIndex];
        Debug.Log(
            $"[SPAWN BEFORE] Client={connection.ClientId} " +
            $"Index={playerIndex} " +
            $"Spawn={spawnPoint.name} " +
            $"Position={spawnPoint.position}"
        );

        NetworkObject player = Instantiate(playerPrefab);

        if (player.TryGetComponent(out PlayerController playerController))
        {
            allRoomPlayerManager.RoomPlayerspPlayerControllers.Add(playerController);
            allRoomPlayerManager.roomPlayersinfo.Add(connection.ClientId, playerController.itsOwnInfo);
        }

      

      
      
        Debug.Log("Connection:"+connection+".    "+spawnPoint.name + " spawned.");
        SetPlayerPosition(player, spawnPoint);
        // if (spawnPoint != null)
        // {
        //     player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        // }
        Debug.Log(
            $"[SPAWN INSTANTIATE] Client={connection.ClientId} " +
            $"PlayerPosition={player.transform.position}"
        );
        InstanceFinder.ServerManager.Spawn(player.gameObject, connection, gameScene);
        Debug.Log(
            $"[SPAWN AFTER NETWORK SPAWN] Client={connection.ClientId} " +
            $"PlayerPosition={player.transform.position}"
        );
        // SetPositionInServer(player, spawnPoint);
        // SetPositionInClient(player, spawnPoint);
        // Now the object (and its nested GetPlayerInfo) is networked — safe to RPC
        if (playerController != null)
        {
            // playerController.itsOwnInfo.SetClientInfo(connection.ClientId);
            spectateSystem.SetLocalPlayerCamera(playerController.itsOwnInfo.playerCamera);
        }


        // Debug.Log(
        //     $"[RoomPlayerSpawner] Room {roomId} -> " +
        //     $"Client {connection.ClientId} spawned in " +
        //     $"scene {gameScene.name}."
        // );
        
    }
    
    private void SetPlayerPosition(
        NetworkObject player,
        Transform spawnPoint)
    {
        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (controller != null)
            controller.enabled = true;
    }
    [ServerRpc]
    private void SetPositionInServer(NetworkObject player, Transform spawnPoint)
    {
        if (spawnPoint != null)
        {
            player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
    }

    [ObserversRpc]
    private void SetPositionInClient(NetworkObject player, Transform spawnPoint)
    {
        if (spawnPoint != null)
        {
            player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
    }


    private Transform GetSpawnPoint(int index)
    {
        if (spawns == null || spawns.Length == 0)
            return transform;

        return spawns[index % spawns.Length];
    }
}