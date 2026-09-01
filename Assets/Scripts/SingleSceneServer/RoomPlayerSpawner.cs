// RoomPlayerSpawner.cs
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

namespace singleSceneServer
{


    public class RoomPlayerSpawner : NetworkBehaviour
    {
        [Header("Player Prefabs")] public GameObject playerPrefab;

        private void Start()
        {
            if (!IsServer)
                return;

            // Listen for player connections
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnPlayerConnection;
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnRemoteConnectionState -= OnPlayerConnection;
        }

        private void OnPlayerConnection(NetworkConnection conn, RemoteConnectionStateArgs state)
        {
            if (state.ConnectionState == RemoteConnectionState.Started)
            {
                // Spawn player when they connect
                SpawnPlayer(conn);
            }
            else if (state.ConnectionState == RemoteConnectionState.Stopped)
            {
                // Despawn player when they disconnect
                // You'll need to track player objects per connection
                DespawnPlayer(conn);
            }
        }

        private void SpawnPlayer(NetworkConnection conn)
        {
            // Check if player is in a room
            if (!MultiRoomNetworkManager.Instance.TryGetRoom(conn, out var room))
                return;

            // Get spawn position from room
            Vector3 spawnPos = GetSpawnPositionForPlayer(conn, room);

            // Spawn player
            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            InstanceFinder.ServerManager.Spawn(playerObj, conn);

            Debug.Log($"Spawned player for {conn.ClientId} at {spawnPos} in room {room.roomId}");
        }

        private Vector3 GetSpawnPositionForPlayer(NetworkConnection conn, SingleSceneRoomManager.RoomData room)
        {
            // Use the room's spawn positions
            int index = room.playerConnections.IndexOf(conn);
            if (index >= 0 && index < room.spawnPositions.Count)
            {
                return room.spawnPositions[index];
            }

            // Fallback
            return room.roomCenter + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
        }

        private void DespawnPlayer(NetworkConnection conn)
        {
            // Find and despawn the player object for this connection
            // You'll need to track the spawned objects
            // Example: if you have a dictionary mapping connection to GameObject
            // if (playerObjects.TryGetValue(conn, out GameObject playerObj))
            // {
            //     InstanceFinder.ServerManager.Despawn(playerObj);
            //     playerObjects.Remove(conn);
            // }
        }
    }
}