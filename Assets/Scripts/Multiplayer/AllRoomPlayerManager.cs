using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System;
using Multiplayer;

public class AllRoomPlayerManager : NetworkBehaviour
{
    public readonly SyncList<PlayerController> RoomPlayerspPlayerControllers = new();
   
    public readonly SyncDictionary<int, GetPlayerInfo> roomPlayersinfo = new();
    public readonly SyncDictionary<NetworkObject,string> roomPlayersName = new();

    public readonly SyncVar<bool> canAllPlayersMove = new();

    public Action OnAllPlayerSpawned;
    
    public Transform disabledSpotTransform;

    [Server]
    public void SetAllPlayersMove(bool value)
    {
        canAllPlayersMove.Value = value;
    }
    
    [Server]
    public void SetPlayerCanMove(PlayerController playerController,bool value)
    {
        if (RoomPlayerspPlayerControllers.Contains(playerController))
        {
            if (RoomPlayerspPlayerControllers.Contains(playerController))
            {
                PlayerController player =
                    RoomPlayerspPlayerControllers.Find(
                        p => p == playerController
                    );

                player.SetCanMove(value);
            }
        }
    }
    
    

    public bool GetCanAllPlayersMove()
    {
        return canAllPlayersMove.Value;
    }

    public string GetPlayerName(NetworkObject playerNetworkObject)
    {
        return roomPlayersName[playerNetworkObject];
    }

    // AllRoomPlayerManager.cs
    public override void OnStartServer()
    {
        base.OnStartServer();
        canAllPlayersMove.OnChange += CanAllPlayersMoveOnOnChange;
        canAllPlayersMove.Value = false;
        // remove: OnAllPlayerSpawned += _OnAllPlayerSpawned;
    }

    [Server]
    public void NotifyAllPlayersSpawned()
    {
        OnAllPlayerSpawned?.Invoke();   // keep for anyone else hooking in
        _OnAllPlayerSpawned();          // but always run this deterministically
    }

    [Server]
    private void _OnAllPlayerSpawned()
    {
        LobbyGameManager lobby = FindFirstObjectByType<LobbyGameManager>();

        if (lobby == null)
        {
            Debug.LogError($"{nameof(lobby)} is null");
            return;
        }

       

        foreach (var playerInfo in roomPlayersinfo)
        {
            int clientId = playerInfo.Key;

            if (!lobby.players.TryGetValue(clientId, out LobbyPlayer lobbyPlayer))
                continue;

            GetPlayerInfo getPlayerInfo = playerInfo.Value;
            
            roomPlayersName[getPlayerInfo.thisplayerNetworkObject] = lobbyPlayer.playerName;

            getPlayerInfo.PlayerName = lobbyPlayer.playerName;

            Debug.Log($"Client {clientId} Name: {getPlayerInfo.PlayerName}");

            SetPlayerName(getPlayerInfo,clientId,getPlayerInfo.PlayerName);
        }
    }

    [Server]
    public void SetPlayerName(GetPlayerInfo getPlayerInfo,int clientId, string playerName)
    {
        getPlayerInfo.SetPlayerInfo(clientId,playerName);
    }

    [ObserversRpc]
    private void CanAllPlayersMoveOnOnChange(bool prev, bool next, bool asServer)
    {
        foreach (var player in RoomPlayerspPlayerControllers)
        {
            player.SetCanMove(next);
            player.SetCanRotateCamera(next);
        }
       
    }
    
    [ObserversRpc]
    public void DisablePlayer(NetworkObject networkObject)
    {
        if (networkObject.TryGetComponent(out PlayerController playerController))
            DisablePlayerLocal(playerController);
        else
            Debug.Log($"{nameof(PlayerController)} is null");
    }

    [ObserversRpc]
    public void DisablePlayer(PlayerController playerController)
    {
        DisablePlayerLocal(playerController);
    }

    private void DisablePlayerLocal(PlayerController playerController)
    {
        playerController.SetCanMove(false);
        playerController.SetCanRotateCamera(false);

        if (playerController.TryGetComponent(out InteractionSystem interactionSystem))
            interactionSystem.SetCanInteract(false);

        // playerController.itsOwnInfo.gameObject.GetComponent<MeshRenderer>().enabled = false;
        playerController.itsOwnInfo.GetCollider().enabled = false;
        playerController.itsOwnInfo.GetMeshRenderer().enabled = false;
        playerController.GetCharacterController().enabled = false;
        playerController.transform.position = disabledSpotTransform.position;
        playerController.transform.rotation = disabledSpotTransform.rotation;
    }
    public void EnablePlayer(NetworkObject networkObject)
    {
        if (networkObject.TryGetComponent(out PlayerController playerController))
        {
            EnablePlayer(playerController);
        }
        else
        {
            Debug.Log($"{nameof(PlayerController)} is null");
        }
    }

    public void EnablePlayer(PlayerController playerController)
    {
        playerController.SetCanMove(true);
        playerController.SetCanRotateCamera(true);
        if (playerController.TryGetComponent(out InteractionSystem interactionSystem))
        {
            interactionSystem.SetCanInteract(true);
        }
        // playerController.itsOwnInfo.gameObject.GetComponent<MeshRenderer>().enabled = true;
        playerController.itsOwnInfo.GetCollider().enabled = true;
        
    }
}