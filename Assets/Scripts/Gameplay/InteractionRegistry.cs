using System.Collections.Generic;
using UnityEngine;

public static class InteractionRegistry
{
    private static readonly Dictionary<Collider, IInteractable> interactables = new();

    public static void Register(
        Collider collider,
        IInteractable interactable)
    {
        if (collider == null || interactable == null)
            return;

        interactables[collider] = interactable;
    }

    public static void Unregister(Collider collider)
    {
        if (collider == null)
            return;

        interactables.Remove(collider);
    }

    public static bool TryGet(
        Collider collider,
        out IInteractable interactable)
    {
        return interactables.TryGetValue(
            collider,
            out interactable
        );
    }
}