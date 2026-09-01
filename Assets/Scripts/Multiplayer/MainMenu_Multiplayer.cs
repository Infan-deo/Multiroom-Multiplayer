using System;
using UnityEngine;

public class MainMenu_Multiplayer : MonoBehaviour
{
    public GameObject RoomPanel;
    public GameObject LobbyPanel,mainCamera;
    
    
    public static MainMenu_Multiplayer Instance;

    private void Awake()
    {
        Instance = this;
    }

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