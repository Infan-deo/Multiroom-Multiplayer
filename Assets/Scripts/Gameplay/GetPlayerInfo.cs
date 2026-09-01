using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class GetPlayerInfo : NetworkBehaviour, IInteractable
{
    public SO_PlayerInfo PlayerInfo;
    public bool canInteract = true;
    public NetworkObject NetworkObject;
    public static Action<PlayerController> OnInteract;
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

    public void Interact(PlayerController player)
    {
        Debug.Log("GetPlayerInfoName: " + PlayerInfo.playerName);
        OnInteract?.Invoke(player);
    }
}