// MultiRoomNetworkManager.cs (Modified)
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

namespace singleSceneServer
{

    public class MultiRoomNetworkManager : MonoBehaviour
    {
        public static MultiRoomNetworkManager Instance;

        // [Header("Single Scene Room Settings")] public GameObject gameWorldPrefab; // Your game world
        public int maxPlayers = 100;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Delegate all room operations to SingleSceneRoomManager
        public void StartRoomGame(int roomId)
        {
            if (SingleSceneRoomManager.Instance != null)
            {
                SingleSceneRoomManager.Instance.StartRoomGame(roomId);
            }
            else
            {
                Debug.LogError("SingleSceneRoomManager not found!");
            }
        }

        public void ServerRemoveConnectionFromRoom(NetworkConnection conn)
        {
            if (SingleSceneRoomManager.Instance != null)
            {
                SingleSceneRoomManager.Instance.ServerRemoveConnectionFromRoom(conn);
            }
        }

        public bool TryGetRoom(NetworkConnection conn, out SingleSceneRoomManager.RoomData room)
        {
            if (SingleSceneRoomManager.Instance != null)
            {
                return SingleSceneRoomManager.Instance.TryGetRoom(conn, out room);
            }

            room = null;
            return false;
        }

        public bool TryGetRoom(int roomId, out SingleSceneRoomManager.RoomData room)
        {
            if (SingleSceneRoomManager.Instance != null)
            {
                return SingleSceneRoomManager.Instance.TryGetRoom(roomId, out room);
            }

            room = null;
            return false;
        }
    }
}