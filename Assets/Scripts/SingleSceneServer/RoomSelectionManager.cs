using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.PopupSystem;
using UnityEngine.SceneManagement;

namespace singleSceneServer
{
    public class RoomSelectionManager : MonoBehaviour
    {
        [Header("Create Room")] 
        public TMP_InputField nameField;
        public TMP_InputField maxField;
        public TMP_Dropdown roomVisibilityDropdown;
        private RoomVisibility _roomVisibility = RoomVisibility.Private;
        public string roomName;
        public Button createRoomButton;

        [Header("Find Room")] 
        public Button findRoomButton;

        [Header("Join Room")] 
        public TMP_InputField roomIDField;
        public Button joinRoomButton;
        public Transform roomListPrefab;
        public Transform roomListParentTransform;

        [Header("Player info")] 
        public TMP_InputField playerNameField;
        public Button save;
        public SO_PlayerInfo playerInfo;

        [Header("Scene Management")] 
        public string gameWorldSceneName = "GameWorld";

        private bool isGameWorldLoaded = false;
        private bool isLoadingGameWorld = false;

        private void Start()
        {
            // Setup UI listeners
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(CreateRoom);
            if (findRoomButton != null)
                findRoomButton.onClick.AddListener(FindRoom);
            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(JoinRoomViaID);
            if (save != null)
                save.onClick.AddListener(SavePlayerName);
            if (roomVisibilityDropdown != null)
                roomVisibilityDropdown.onValueChanged.AddListener(OnRoomVisibilityChanged);

            // Register network broadcasts
            if (InstanceFinder.ClientManager != null)
            {
                InstanceFinder.ClientManager.RegisterBroadcast<RoomMessages.RoomListResponseMessage>(OnRoomList);
                InstanceFinder.ClientManager.RegisterBroadcast<RoomMessages.RoomCreatedMessage>(OnRoomCreated);
                InstanceFinder.ClientManager.RegisterBroadcast<RoomMessages.RoomJoinFailedMessage>(OnRoomJoinFailed);
                InstanceFinder.ClientManager.RegisterBroadcast<RoomMessages.EnterLobbyMessage>(OnEnterLobby);
                InstanceFinder.ClientManager.RegisterBroadcast<RoomMessages.GameStartedMessage>(OnGameStarted); // NEW
            }

            LoadSavedPlayerName();

            // DON'T load GameWorld here - wait for game to start
        }

        private void OnDestroy()
        {
            if (InstanceFinder.ClientManager != null)
            {
                InstanceFinder.ClientManager.UnregisterBroadcast<RoomMessages.RoomListResponseMessage>(OnRoomList);
                InstanceFinder.ClientManager.UnregisterBroadcast<RoomMessages.RoomCreatedMessage>(OnRoomCreated);
                InstanceFinder.ClientManager.UnregisterBroadcast<RoomMessages.RoomJoinFailedMessage>(OnRoomJoinFailed);
                InstanceFinder.ClientManager.UnregisterBroadcast<RoomMessages.EnterLobbyMessage>(OnEnterLobby);
                InstanceFinder.ClientManager.UnregisterBroadcast<RoomMessages.GameStartedMessage>(OnGameStarted);
            }
        }

        // ============================================================
        // SCENE MANAGEMENT
        // ============================================================

        private bool IsGameWorldLoaded()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name == gameWorldSceneName && scene.isLoaded)
                    return true;
            }
            return false;
        }

        private void LoadGameWorldScene(Action onComplete = null)
        {
            if (IsGameWorldLoaded())
            {
                isGameWorldLoaded = true;
                onComplete?.Invoke();
                return;
            }

            if (isLoadingGameWorld)
                return;

            isLoadingGameWorld = true;

            Debug.Log($"Loading GameWorld scene: {gameWorldSceneName}");

            SceneManager.LoadSceneAsync(gameWorldSceneName, LoadSceneMode.Additive)
                .completed += (asyncOp) =>
                {
                    isGameWorldLoaded = true;
                    isLoadingGameWorld = false;
                    Debug.Log("GameWorld scene loaded successfully!");
                    onComplete?.Invoke();
                };
        }

        // ============================================================
        // NETWORK MESSAGE HANDLERS
        // ============================================================

        private void OnEnterLobby(RoomMessages.EnterLobbyMessage msg, Channel channel)
        {
            // Find LobbyGameManager (in RoomMenu scene)
            LobbyGameManager lobby = FindFirstObjectByType<LobbyGameManager>();

            if (lobby != null)
            {
                lobby.SetRoomInfo(msg.roomId, msg.roomName);
            }

            // Show lobby UI (RoomMenu scene UI)
            if (MainMenu_Multiplayer.Instance != null)
                MainMenu_Multiplayer.Instance.ShowLobbyPanel();
        }

        // NEW: When game starts, load GameWorld
        private void OnGameStarted(RoomMessages.GameStartedMessage msg, Channel channel)
        {
            Debug.Log($"Game starting for room {msg.roomId}! Loading GameWorld...");

            // Show loading screen
            PopupManager.Popup_Show(new PopupContent(
                "Game Starting!",
                "Loading game world...",
                showConfirmButton: false));

            // Load GameWorld
            LoadGameWorldScene(() =>
            {
                // Close loading popup
                PopupManager.Popup_Close();
                
                // Hide lobby UI, show game UI
                if (MainMenu_Multiplayer.Instance != null)
                    MainMenu_Multiplayer.Instance.HideBoth();
                
                if (SingleSceneRoomManager.Instance != null)
                {
                    SingleSceneRoomManager.Instance.FindSpawnPointsInGameWorld();
                }

                
                Debug.Log("GameWorld loaded! Game is starting...");
            });
        }

        private void OnRoomCreated(RoomMessages.RoomCreatedMessage msg, Channel channel)
        {
            Debug.Log($"Room created! Room ID: {msg.roomId}, Name: {msg.roomName}");

            PopupManager.Popup_Show(new PopupContent(
                "Room Created!",
                $"Room '{msg.roomName}' has been created.\nRoom Code: {msg.roomId}",
                showConfirmButton: true));
        }

        private void OnRoomJoinFailed(RoomMessages.RoomJoinFailedMessage msg, Channel channel)
        {
            PopupManager.Popup_Show(new PopupContent(
                "Failed to Join Room",
                msg.reason,
                showConfirmButton: true));
        }

        // ============================================================
        // ROOM VISIBILITY
        // ============================================================

        private void OnRoomVisibilityChanged(int value)
        {
            _roomVisibility = value == 0
                ? RoomVisibility.Private
                : RoomVisibility.Public;
            Debug.Log($"Room visibility set to: {_roomVisibility}");
        }

        // ============================================================
        // PLAYER NAME MANAGEMENT
        // ============================================================

        private void SavePlayerName()
        {
            string playerName = playerNameField != null
                ? playerNameField.text.Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(playerName))
                return;

            if (playerInfo != null)
                playerInfo.playerName = playerName;

            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();

            Debug.Log($"Player name saved: {playerName}");
        }

        private void LoadSavedPlayerName()
        {
            if (playerNameField == null)
                return;

            if (PlayerPrefs.HasKey("PlayerName"))
                playerNameField.text = PlayerPrefs.GetString("PlayerName");
            else if (playerInfo != null)
                playerNameField.text = playerInfo.playerName;
        }

        private string GetPlayerName()
        {
            string playerName = playerNameField != null
                ? playerNameField.text.Trim()
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(playerName))
            {
                if (playerInfo != null)
                    playerInfo.playerName = playerName;
                PlayerPrefs.SetString("PlayerName", playerName);
                PlayerPrefs.Save();
                return playerName;
            }

            if (playerInfo != null && !string.IsNullOrWhiteSpace(playerInfo.playerName))
                return playerInfo.playerName;

            return "Player";
        }

        // ============================================================
        // FIND ROOMS
        // ============================================================

        private void FindRoom()
        {
            if (InstanceFinder.ClientManager == null)
            {
                Debug.LogWarning("ClientManager not available");
                return;
            }

            InstanceFinder.ClientManager.Broadcast(new RoomListRequestMessage());
            Debug.Log("Requesting room list...");
        }

        // ============================================================
        // JOIN ROOM VIA ID
        // ============================================================

        private void JoinRoomViaID()
        {
            if (!int.TryParse(roomIDField.text.Trim(), out int roomId))
            {
                PopupManager.Popup_Show(new PopupContent(
                    "Invalid Room Code",
                    "Please enter a valid 5-digit room code.",
                    showConfirmButton: true));
                return;
            }

            SavePlayerName();

            // Send join request to server (GameWorld NOT loaded yet)
            InstanceFinder.ClientManager.Broadcast(new RoomMessages.JoinRoomMessage
            {
                roomID = roomId,
                isRoomidavailable = true
            });

            Debug.Log($"Attempting to join room: {roomId}");
        }

        // ============================================================
        // CREATE ROOM
        // ============================================================

        public void CreateRoom()
        {
            // Validate max players
            if (!int.TryParse(maxField.text, out int maxPlayers))
            {
                PopupManager.Popup_Show(new PopupContent(
                    "Invalid Player Count",
                    "Please enter a valid maximum player count.",
                    showConfirmButton: true));
                return;
            }

            // Validate room name
            string selectedRoomName = nameField.text.Trim();
            if (string.IsNullOrWhiteSpace(selectedRoomName))
            {
                PopupManager.Popup_Show(new PopupContent(
                    "Room Name Required",
                    "Please enter a room name.",
                    showConfirmButton: true));
                return;
            }

            // Validate max players range
            if (maxPlayers < 2)
            {
                PopupManager.Popup_Show(new PopupContent(
                    "Invalid Player Count",
                    "Maximum players must be at least 2.",
                    showConfirmButton: true));
                return;
            }

            if (maxPlayers > 8)
            {
                PopupManager.Popup_Show(new PopupContent(
                    "Invalid Player Count",
                    "Maximum players cannot exceed 8.",
                    showConfirmButton: true));
                return;
            }

            SavePlayerName();

            // Send create room request (GameWorld NOT loaded yet)
            var msg = new RoomMessages.CreateRoomMessage
            {
                roomName = selectedRoomName,
                sceneName = selectedRoomName,
                maxPlayers = Mathf.Clamp(maxPlayers, 2, 8),
                Roomvisibility = _roomVisibility
            };

            InstanceFinder.ClientManager.Broadcast(msg);
            Debug.Log($"Creating room: {selectedRoomName} (Max: {maxPlayers}, Visibility: {_roomVisibility})");
        }

        // ============================================================
        // ROOM LIST HANDLING
        // ============================================================

        public struct Entry
        {
            public string name, data, scene;
            public int cur, max;
            public global::RoomVisibility roomVisibility;
            public int roomId;
        }

        private readonly List<Entry> rooms = new List<Entry>();

        private void OnRoomList(RoomMessages.RoomListResponseMessage msg, Channel channel)
        {
            rooms.Clear();
            ClearRoomList();

            if (msg.roomNames == null)
                return;

            for (int i = 0; i < msg.roomNames.Length; i++)
            {
                rooms.Add(new Entry
                {
                    name = msg.roomNames[i],
                    data = msg.roomDatas[i],
                    scene = msg.sceneNames[i],
                    cur = msg.currentCounts[i],
                    max = msg.maxCounts[i],
                    roomVisibility = msg.Roomvisibility[i],
                    roomId = msg.roomIds != null && i < msg.roomIds.Length ? msg.roomIds[i] : 0
                });
            }

            foreach (var room in rooms)
            {
                if (room.roomVisibility == RoomVisibility.Public)
                {
                    Transform child = Instantiate(roomListPrefab, roomListParentTransform);
                    RoomSceneInfo roomInfo = child.GetComponent<RoomSceneInfo>();

                    if (roomInfo != null)
                    {
                        // roomInfo.Setup(room);
                    }

                    Button joinButton = child.GetComponent<Button>();
                    if (joinButton != null)
                    {
                        int roomId = room.roomId;
                        joinButton.onClick.AddListener(() => JoinRoomById(roomId));
                    }
                }
            }
        }

        private void JoinRoomById(int roomId)
        {
            if (roomId <= 0)
                return;

            SavePlayerName();

            // Send join request (GameWorld NOT loaded yet)
            InstanceFinder.ClientManager.Broadcast(new RoomMessages.JoinRoomMessage
            {
                roomID = roomId,
                isRoomidavailable = true
            });

            Debug.Log($"Joining room by ID: {roomId}");
        }

        private void ClearRoomList()
        {
            if (roomListParentTransform == null)
                return;

            foreach (Transform child in roomListParentTransform)
                Destroy(child.gameObject);
        }
    }
}