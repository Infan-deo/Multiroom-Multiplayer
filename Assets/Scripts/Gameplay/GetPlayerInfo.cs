using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class GetPlayerInfo : NetworkBehaviour, IInteractable
{
    public SO_PlayerInfo PlayerInfo;
    public bool canInteract = true;
    public NetworkObject bodyNetworkObject;
    public NetworkObject thisplayerNetworkObject;
    public static Action<PlayerController> OnInteract;
    private int _playerclientId;
    public string PlayerName;
    public TextMeshProUGUI PlayerNameText;
    public event EventHandler<NetworkObjEventArgs> OnPlayerInteractWithAnother;

    public class NetworkObjEventArgs : EventArgs
    {
        public NetworkObject from;
        public NetworkObject to;

        public NetworkObjEventArgs(NetworkObject from, NetworkObject to)
        {
            this.from = from;
            this.to = to;
        }
    }

    [SerializeField] private Collider interactionCollider;

    private void Awake()
    {
        InteractionRegistry.Register(
            interactionCollider,
            this
        );
    }

    private void OnDestroy()
    {
        InteractionRegistry.Unregister(
            interactionCollider
        );
    }

    public string GetInteractionText()
    {
        return "Player Info";
    }

    public bool CanInteract(PlayerController player)
    {
        return canInteract;
    }

    public readonly SyncVar<int> ClientId = new();
    public readonly SyncVar<string> SyncedPlayerName = new();

    public override void OnStartClient()
    {
        base.OnStartClient();
        SyncedPlayerName.OnChange += OnPlayerNameChanged;

        // handle the case where the value was already set before this client started observing
        if (!string.IsNullOrEmpty(SyncedPlayerName.Value))
            PlayerNameText.text = SyncedPlayerName.Value;
        if (IsOwner)
        {
            PlayerNameText.gameObject.SetActive(false);
        }
    }

    private void OnPlayerNameChanged(string prev, string next, bool asServer)
    {
        if (PlayerNameText != null)
            PlayerNameText.text = next;
    }

    [Server]
    public void SetPlayerInfo(int clientId, string playerName)
    {
        ClientId.Value = clientId;
        SyncedPlayerName.Value = playerName;
    }

    [ObserversRpc]
    public void SetClientInfo(int clientId)
    {
        Debug.Log("[Interaction] SetClientInfo" + clientId);
        _playerclientId = clientId;
    }

    public void Interact(PlayerController player)
    {
        // Debug.Log("GetPlayerInfoName: " + PlayerInfo.playerName);
        OnInteract?.Invoke(player);
        OnPlayerInteractWithAnother?.Invoke(this,
            new NetworkObjEventArgs(player.NetworkObject, thisplayerNetworkObject));
    }
}