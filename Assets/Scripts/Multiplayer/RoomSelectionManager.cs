using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.PopupSystem;
using Multiplayer;

public enum RoomVisibility
{
    Public,
    Private
}

public class RoomSelectionManager : MonoBehaviour
{
    [Header("Create Room")] public TMP_InputField nameField;
    public TMP_InputField maxField;
    public TMP_Dropdown roomVisibilityDropdown;
    private RoomVisibility _roomVisibility = RoomVisibility.Private;
    public string roomName;
    public Button createRoomButton;
    public Button InstantCreateRoomButton;

    [Header("Find Room")] public Button findRoomButton;

    [Header("Join Room")] public TMP_InputField roomIDField;
    public Button joinRoomButton;
    public Transform roomListPrefab;
    public Transform roomListParentTransform;

    [Header("Player info")] public TMP_InputField playerNameField;
    public Button save;
    public SO_PlayerInfo playerInfo;

    private void Start()
    {
        if (InstantCreateRoomButton != null)
            InstantCreateRoomButton.onClick.AddListener(InstantCreateRoom);
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

        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.RegisterBroadcast<RoomListResponseMessage>(OnRoomList);
        
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.RegisterBroadcast<EnterLobbyMessage>(OnEnterLobby);

        LoadSavedPlayerName();
    }

    private void InstantCreateRoom()
    {
        var msg = new CreateRoomMessage
        {
            roomName = "Room1",
            sceneName = roomName,
            maxPlayers = 2,
            Roomvisibility = RoomVisibility.Public
        };

        InstanceFinder.ClientManager.Broadcast(msg);
    }

    private void OnEnterLobby(
        EnterLobbyMessage msg,
        Channel channel)
    {
        LobbyGameManager lobby =
            FindFirstObjectByType<LobbyGameManager>();

        if (lobby != null)
        {
            lobby.SetRoomInfo(msg.roomId, msg.roomName);
        }

        if (MainMenu_Multiplayer.Instance != null)
            MainMenu_Multiplayer.Instance.ShowLobbyPanel();
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.UnregisterBroadcast<RoomListResponseMessage>(OnRoomList);
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.UnregisterBroadcast<EnterLobbyMessage>(OnEnterLobby);
    }

    private void OnRoomVisibilityChanged(int value)
    {
        _roomVisibility = value == 0
            ? RoomVisibility.Private
            : RoomVisibility.Public;
        Debug.Log(_roomVisibility);
    }

    private void SavePlayerName()
    {
        string playerName = playerNameField != null
            ? playerNameField.text.Trim()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(playerName))
            return;

        // Assumes SO_PlayerInfo exposes a public string field named playerName.
        // Change this one line if your SO uses a different field/property name.
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

    private void FindRoom()
    {
        if (InstanceFinder.ClientManager == null)
            return;

        InstanceFinder.ClientManager.Broadcast(new RoomListRequestMessage());
    }

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

        // Room ID joining intentionally does NOT check visibility.
        // Therefore private rooms can still be joined with the correct code.
        InstanceFinder.ClientManager.Broadcast(new JoinRoomMessage
        {
            roomID = roomId,
            isRoomidavailable = true
        });
    }

    public void CreateRoom()
    {
        if (!int.TryParse(maxField.text, out int maxPlayers))
        {
            PopupManager.Popup_Show(new PopupContent(
                "Invalid Player Count",
                "Please enter a valid maximum player count.",
                showConfirmButton: true));
            return;
        }

        string selectedRoomName = nameField.text.Trim();
        if (string.IsNullOrWhiteSpace(selectedRoomName))
        {
            PopupManager.Popup_Show(new PopupContent(
                "Room Name Required",
                "Please enter a room name.",
                showConfirmButton: true));
            return;
        }

        SavePlayerName();

        var msg = new CreateRoomMessage
        {
            roomName = selectedRoomName,
            sceneName = roomName,
            maxPlayers = Mathf.Max(1, maxPlayers),
            Roomvisibility = _roomVisibility
        };

        InstanceFinder.ClientManager.Broadcast(msg);
    }

    public struct Entry
    {
        public string name, data, scene;
        public int cur, max;
        public RoomVisibility roomVisibility;
    }

    private readonly List<Entry> rooms = new List<Entry>();

    private void OnRoomList(RoomListResponseMessage msg, Channel channel)
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
                roomVisibility = msg.Roomvisibility[i]
            });
        }

        foreach (var room in rooms)
        {
            // Debug.Log(room.roomVisibility);
            if (room.roomVisibility == RoomVisibility.Public)
            {
                Transform child = Instantiate(roomListPrefab, roomListParentTransform);
                child.GetComponent<RoomSceneInfo>().Setup(room);
            }
        }
    }

    private void ClearRoomList()
    {
        if (roomListParentTransform == null)
            return;

        foreach (Transform child in roomListParentTransform)
            Destroy(child.gameObject);
    }
}