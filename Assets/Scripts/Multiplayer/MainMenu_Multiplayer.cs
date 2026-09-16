using System;
using FishNet.Object;
using UnityEngine;

public class MainMenu_Multiplayer : NetworkBehaviour
{
    public GameObject RoomPanel;
    public GameObject LobbyPanel,mainCamera;
    
    
    public static MainMenu_Multiplayer Instance;

    private void Awake()
    {
        Instance = this;
    }

    // public override void OnStartServer()
    // {
    //     base.OnStartServer();
    //     if (IsServerInitialized)
    //     {
    //         mainCamera.SetActive(false);
    //     }
    // }

    public void ShowRoomPanel()
    {
        RoomPanel.SetActive(true);
        LobbyPanel.SetActive(false);
    }

    public void ShowLobbyPanel()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
        // mainCamera.SetActive(false);
    }
    public void HideBoth()
    {
        RoomPanel.SetActive(false);
        LobbyPanel.SetActive(false);
        mainCamera.SetActive(false);
    }
}