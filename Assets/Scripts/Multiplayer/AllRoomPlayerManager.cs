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

    [Server]
    public void SetAllPlayersMove(bool value)
    {
        canAllPlayersMove.Value = value;
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
        }
    }
}