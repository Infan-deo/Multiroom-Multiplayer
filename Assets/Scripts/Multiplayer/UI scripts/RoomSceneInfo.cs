using System;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RoomSceneInfo : MonoBehaviour
{
    public TextMeshProUGUI roomNameText;
    public Button joinBtn;


    private RoomSelectionManager.Entry _currentRoom;//change

    private void Start()
    {
        joinBtn.onClick.AddListener(OnJoinClicked);
    }

    private void OnJoinClicked()
    {
        InstanceFinder.ClientManager.Broadcast(new JoinRoomMessage { roomName = _currentRoom.name });
    }

    public void Setup(RoomSelectionManager.Entry currentRoom)
    {
        _currentRoom = currentRoom;
        roomNameText.text = _currentRoom.name+"  ("+_currentRoom.cur+"/"+_currentRoom.max+")";
    }
}