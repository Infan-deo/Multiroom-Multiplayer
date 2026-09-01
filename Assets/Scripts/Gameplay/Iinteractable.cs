using FishNet.Object;
using UnityEngine;

public interface IInteractable
{
    NetworkObject NetworkObject { get; }
    string GetInteractionText();

    bool CanInteract(PlayerController player);

    void Interact(PlayerController player);
}
