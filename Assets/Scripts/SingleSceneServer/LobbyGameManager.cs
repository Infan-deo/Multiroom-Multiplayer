// LobbyGameManager.cs (Updated)

using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.PopupSystem;

namespace singleSceneServer
{
    public class LobbyGameManager : NetworkBehaviour
    {
        [Serializable]
        public struct LobbyPlayer
        {
            public int clientId;
            public int roomId;
            public string playerName;
            public bool isReady;

            public LobbyPlayer(int clientId, int roomId, string playerName)
            {
                this.clientId = clientId;
                this.roomId = roomId;
                this.playerName = playerName;
                this.isReady = false;
            }
        }

        private readonly SyncDictionary<int, LobbyPlayer> players = new();

        [Header("Lobby Player UI")] public Transform playerLobbyInfoContainer;
        public Transform playerLobbyPrefab;

        [Header("Lobby Rules")] [SerializeField, Range(0.01f, 1f)]
        private float readyPercentageToStart = 0.8f;

        [SerializeField] private int minimumPlayers = 2;

        [Header("Room Code Info")] public Button ShowButton;
        public Button CopyButton;
        public TextMeshProUGUI CodeText;
        public GameObject HideCodeText;
        private bool isCodeShowing;
        public int code;

        [Header("Lobby Controls")] public Button ReadyButton;
        public Button LeaveLobbyButton;

        private readonly Dictionary<int, PlayerLobbyUI> playerUIByClientId = new();
        private int localRoomId = -1;
        private readonly HashSet<int> startingRooms = new();

        public int LocalRoomId => localRoomId;

        private void Awake()
        {
            players.OnChange += OnPlayersChanged;

            if (ShowButton != null)
                ShowButton.onClick.AddListener(ToggleCodeToShow);
            if (CopyButton != null)
                CopyButton.onClick.AddListener(CopyCode);
            if (ReadyButton != null)
                ReadyButton.onClick.AddListener(ToggleLocalReady);
            if (LeaveLobbyButton != null)
                LeaveLobbyButton.onClick.AddListener(LeaveLobby);
        }

        private void OnDestroy()
        {
            players.OnChange -= OnPlayersChanged;

            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
        }

       
        
        public override void OnStartClient()
        {
            base.OnStartClient();
    
            // ✅ Enable the GameObject when client starts
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Debug.Log("LobbyGameManager activated on client start");
            }
    
            RefreshLobbyUI();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
    
            // ✅ Enable the GameObject when server starts
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Debug.Log("LobbyGameManager activated on server start");
            }
    
            InstanceFinder.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
        }

        public void SetRoomInfo(int _roomId, string _roomName)
        {
            localRoomId = _roomId;
            code = _roomId;

            if (CodeText != null)
                CodeText.text = _roomId.ToString();

            if (MainMenu_Multiplayer.Instance != null)
                MainMenu_Multiplayer.Instance.ShowLobbyPanel();

            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            RegisterPlayerServerRpc(playerName);
            RefreshLobbyUI();

            Debug.Log($"Lobby initialized: {_roomName} ({_roomId})");
        }

        private void ToggleCodeToShow()
        {
            isCodeShowing = !isCodeShowing;

            if (CodeText != null)
                CodeText.gameObject.SetActive(isCodeShowing);
            if (HideCodeText != null)
                HideCodeText.SetActive(!isCodeShowing);
        }

        private void CopyCode()
        {
            if (code <= 0)
                return;

            GUIUtility.systemCopyBuffer = code.ToString();
            PopupManager.Popup_Show(new PopupContent(
                "Room Code Copied",
                $"Room code has been copied to your clipboard.",
                showConfirmButton: true));
        }

        private void ToggleLocalReady()
        {
            if (localRoomId <= 0)
                return;

            ToggleReadyServerRpc();
        }

        private void LeaveLobby()
        {
            if (localRoomId <= 0)
                return;

            LeaveLobbyServerRpc();
        }

        // ============================================================
        // PLAYER REGISTRATION
        // ============================================================

        [ServerRpc(RequireOwnership = false)]
        public void RegisterPlayerServerRpc(string playerName, NetworkConnection sender = null)
        {
            if (sender == null)
                return;

            var manager = MultiRoomNetworkManager.Instance;
            if (manager == null || !manager.TryGetRoom(sender, out var room))
                return;

            playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();

            if (players.ContainsKey(sender.ClientId))
                return;

            players.Add(
                sender.ClientId,
                new LobbyPlayer(
                    sender.ClientId,
                    room.roomId,
                    playerName
                )
            );
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleReadyServerRpc(NetworkConnection sender = null)
        {
            if (sender == null || !players.TryGetValue(sender.ClientId, out LobbyPlayer player))
                return;

            player.isReady = !player.isReady;
            players[sender.ClientId] = player;

            TryStartRoom(player.roomId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveLobbyServerRpc(NetworkConnection sender = null)
        {
            if (sender == null)
                return;

            if (players.ContainsKey(sender.ClientId))
                players.Remove(sender.ClientId);

            MultiRoomNetworkManager.Instance?.ServerRemoveConnectionFromRoom(sender);
            sender.Disconnect(true);
        }

        // ============================================================
        // READY CHECK & GAME START
        // ============================================================

        [Server]
        private void TryStartRoom(int roomId)
        {
            if (startingRooms.Contains(roomId))
                return;

            List<LobbyPlayer> roomPlayers = new List<LobbyPlayer>();
            foreach (LobbyPlayer player in players.Values)
            {
                if (player.roomId == roomId)
                    roomPlayers.Add(player);
            }

            if (roomPlayers.Count < minimumPlayers)
                return;

            int readyCount = 0;
            foreach (LobbyPlayer player in roomPlayers)
            {
                if (player.isReady)
                    readyCount++;
            }

            int requiredReady = Mathf.CeilToInt(roomPlayers.Count * readyPercentageToStart);

            Debug.Log($"Room {roomId}: {readyCount}/{roomPlayers.Count} ready. Required: {requiredReady}");

            if (readyCount < requiredReady)
                return;

            StartGameForRoom(roomId);
        }

        // In LobbyGameManager.cs - StartGameForRoom()
        [Server]
        private void StartGameForRoom(int roomId)
        {
            MultiRoomNetworkManager manager = MultiRoomNetworkManager.Instance;

            if (manager == null)
                return;

            if (!manager.TryGetRoom(roomId, out var room))
                return;

            if (room.playerConnections.Count == 0)
            {
                startingRooms.Remove(roomId);
                return;
            }

            Debug.Log($"Starting game for room {roomId} with {room.playerConnections.Count} players.");

            // NEW: Broadcast to all clients in this room that game is starting
            foreach (var conn in room.playerConnections)
            {
                conn.Broadcast(new RoomMessages.GameStartedMessage
                {
                    roomId = roomId,
                    roomName = room.roomName
                });
            }

            // Start the game (this will load GameWorld on clients)
            manager.StartRoomGame(roomId);
        }

        // ============================================================
        // CLIENT UI
        // ============================================================

        private void OnPlayersChanged(SyncDictionaryOperation operation, int clientId, LobbyPlayer player,
            bool asServer)
        {
            if (asServer)
                return;

            RefreshLobbyUI();
        }

        private void RefreshLobbyUI()
        {
            if (playerLobbyInfoContainer == null || playerLobbyPrefab == null || localRoomId <= 0)
                return;

            foreach (Transform child in playerLobbyInfoContainer)
                Destroy(child.gameObject);

            playerUIByClientId.Clear();

            foreach (KeyValuePair<int, LobbyPlayer> pair in players)
            {
                LobbyPlayer player = pair.Value;

                if (player.roomId != localRoomId)
                    continue;

                Transform child = Instantiate(playerLobbyPrefab, playerLobbyInfoContainer);
                PlayerLobbyUI ui = child.GetComponent<PlayerLobbyUI>();

                if (ui == null)
                {
                    Debug.LogError("Lobby player prefab is missing PlayerLobbyUI.", child.gameObject);
                    continue;
                }

                // ui.Setup(ui.playerImage, player.playerName, player.isReady);
                playerUIByClientId[player.clientId] = ui;
            }
        }

        private void ServerManager_OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs state)
        {
            if (state.ConnectionState != RemoteConnectionState.Stopped)
                return;

            if (players.ContainsKey(conn.ClientId))
                players.Remove(conn.ClientId);
        }

        public IReadOnlyDictionary<int, LobbyPlayer> GetPlayers()
        {
            return players;
        }
    }
}