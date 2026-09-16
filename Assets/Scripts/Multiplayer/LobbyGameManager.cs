using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Game.PopupSystem;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Multiplayer
{
    [Serializable]
    public struct LobbyPlayer 
    {
        public int clientId;
        public int roomId;
        public string playerName;
        public bool isReady;
        public SteamId steamId;

        public LobbyPlayer(int clientId, int roomId, string playerName,SteamId steamId)
        {
            this.clientId = clientId;
            this.roomId = roomId;
            this.playerName = playerName;
            this.steamId = steamId;
            this.isReady = false;
        }
    }

    public class LobbyGameManager : NetworkBehaviour
    {
   

        // One networked dictionary is shared by the scene, so roomId is part of every player entry.
        public readonly SyncDictionary<int, LobbyPlayer> players = new();

        [Header("Lobby Player UI")]
        public Transform playerLobbyInfoContainer;
        public Transform playerLobbyPrefab;

        [Header("Lobby Rules")]
        [SerializeField, Range(0.01f, 1f)] private float readyPercentageToStart = 0.8f;
        [SerializeField] private int minimumPlayers = 1;

        [Header("Room Code Info")]
        public Button ShowButton;
        public Button CopyButton;
        public GameObject ShowIcon;
        public GameObject HideIcon;
        public TextMeshProUGUI CodeText;
        public GameObject HideCodeText;
        private bool isCodeShowing;
        public int code;

        [Header("Lobby Controls")]
        public Button ReadyButton;
        public Button LeaveLobbyButton;

        public SO_PlayerInfo soPlayerInfo;

        private readonly Dictionary<int, PlayerLobbyUI> playerUIByClientId = new();
        private int localRoomId = -1;
        private readonly HashSet<int> startingRooms = new();

        public int LocalRoomId => localRoomId;
        public SteamProfile  steamProfile;
        
     
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

        public override void OnStartServer()
        {
            base.OnStartServer();
            InstanceFinder.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            RefreshLobbyUI();
        }

        // Called by MultiRoomNetworkManager after this client successfully creates/joins a room.
        [TargetRpc]
        public void TargetEnterLobby(
            NetworkConnection target,
            int roomId,
            string roomName)
        {
            localRoomId = roomId;
            code = roomId;

            if (CodeText != null)
                CodeText.text = roomId.ToString();

            // Show lobby immediately after successful create/join.
            if (MainMenu_Multiplayer.Instance != null)
                MainMenu_Multiplayer.Instance.ShowLobbyPanel();

            // Get the saved player name.
            // string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            string playerName = soPlayerInfo.playerName;

            // Register this client on the server.
            RegisterPlayerServerRpc(playerName);

            // Initial refresh. SyncDictionary.OnChange will refresh again
            // when the server adds the player.
            RefreshLobbyUI();

            Debug.Log(
                $"Entered lobby. Room ID: {roomId}, Player: {playerName}"
            );
        }
    
        public void SetRoomInfo(int _roomId, string _roomName)
        {
            localRoomId = _roomId;
            code = _roomId;

            if (CodeText != null)
                CodeText.text = _roomId.ToString();

            if (MainMenu_Multiplayer.Instance != null)
                MainMenu_Multiplayer.Instance.ShowLobbyPanel();

            string playerName = soPlayerInfo.playerName;

            RegisterPlayerServerRpc(playerName);

            RefreshLobbyUI();

            Debug.Log(
                $"Lobby initialized: {_roomName} ({_roomId})"
            );
        }

        private void ToggleCodeToShow()
        {
            isCodeShowing = !isCodeShowing;
            ShowIcon.SetActive(!isCodeShowing);
            HideIcon.SetActive(isCodeShowing);

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
                $"Room code  has been copied to your clipboard.",
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

        // --------------------------------------------------
        // PLAYER REGISTRATION
        // --------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void RegisterPlayerServerRpc(
            string playerName,
            NetworkConnection sender = null)
        {
            if (sender == null)
                return;

            MultiRoomNetworkManager manager = MultiRoomNetworkManager.Instance;

            if (manager == null ||
                !manager.TryGetRoom(sender, out MultiRoomNetworkManager.RoomInfo room))
                return;

            playerName = string.IsNullOrWhiteSpace(playerName)
                ? "Player"
                : playerName.Trim();

            if (players.ContainsKey(sender.ClientId))
                return;

            players.Add(
                sender.ClientId,
                new LobbyPlayer(
                    sender.ClientId,
                    room.roomID,
                    playerName,
                    SteamClient.SteamId
                )
            );
        }

        [Server]
        public void ServerEnterLobby(
            NetworkConnection conn,
            int roomId,
            string roomName)
        {
            if (conn == null)
                return;

            conn.Broadcast(new EnterLobbyMessage
            {
                roomId = roomId,
                roomName = roomName
            });
        }

        [Server]
        public void ServerRemovePlayer(NetworkConnection conn)
        {
            if (conn == null)
                return;

            if (players.ContainsKey(conn.ClientId))
                players.Remove(conn.ClientId);
        }

        // --------------------------------------------------
        // READY
        // --------------------------------------------------

        [ServerRpc(RequireOwnership = false)]
        public void ToggleReadyServerRpc(NetworkConnection sender = null)
        {
            if (sender == null || !players.TryGetValue(sender.ClientId, out LobbyPlayer player))
                return;

            player.isReady = !player.isReady;
            players[sender.ClientId] = player;

            TryStartRoom(player.roomId);
        }

        // --------------------------------------------------
        // LEAVE
        // --------------------------------------------------

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

        // --------------------------------------------------
        // 80% READY CHECK
        // --------------------------------------------------

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

        [Server]
        private void StartGameForRoom(int roomId)
        {
            RpcHideBoth();
            MultiRoomNetworkManager manager = MultiRoomNetworkManager.Instance;

            if (manager == null)
                return;

            if (!manager.TryGetRoom(roomId, out MultiRoomNetworkManager.RoomInfo room))
                return;

            if (room.playerConnections.Count == 0)
            {
                startingRooms.Remove(roomId);
                return;
            }

            Debug.Log(
                $"Starting game for room {roomId} with " +
                $"{room.playerConnections.Count} players."
            );

            // MultiRoomNetworkManager owns scene creation/stacking.
            // This keeps RoomMenu alive on the server and creates a
            // separate game-scene instance for this room.
            manager.StartRoomGame(roomId);
        }
    
        [ObserversRpc]
        void RpcHideBoth()
        {
            if (MainMenu_Multiplayer.Instance != null)
                MainMenu_Multiplayer.Instance.HideBoth();
        }

        // --------------------------------------------------
        // CLIENT UI
        // --------------------------------------------------

        private void OnPlayersChanged(
            SyncDictionaryOperation operation,
            int clientId,
            LobbyPlayer player,
            bool asServer)
        {
            if (asServer)
                return;

            RefreshLobbyUI();
        }

        private async void RefreshLobbyUI()
        {
            if (playerLobbyInfoContainer == null || playerLobbyPrefab == null || localRoomId <= 0)
                return;

            foreach (Transform child in playerLobbyInfoContainer)
                Destroy(child.gameObject);

            playerUIByClientId.Clear();

            foreach (KeyValuePair<int, LobbyPlayer> pair in players)
            {
                LobbyPlayer 
                    player = pair.Value;

                if (player.roomId != localRoomId)
                    continue;

                Sprite playericonfromSteamid = await steamProfile.GetPlayerSprite(player.steamId);
                Transform child = Instantiate(playerLobbyPrefab, playerLobbyInfoContainer);
                PlayerLobbyUI ui = child.GetComponent<PlayerLobbyUI>();

                if (ui == null)
                {
                    Debug.LogError("Lobby player prefab is missing PlayerLobbyUI.", child.gameObject);
                    continue;
                }

                // Player image is intentionally left as the prefab's current image for now.
                ui.Setup(playericonfromSteamid, player.playerName, player.isReady);
                playerUIByClientId[player.clientId] = ui;
            }
        }

        private void ServerManager_OnRemoteConnectionState(
            NetworkConnection conn,
            RemoteConnectionStateArgs state)
        {
            if (state.ConnectionState != RemoteConnectionState.Stopped)
                return;

            ServerRemovePlayer(conn);
        }

        public IReadOnlyDictionary<int, LobbyPlayer> GetPlayers()
        {
            return players;
        }
    }
}