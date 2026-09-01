using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class InteractionSystem : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactionDistance = 3f;
    // [SerializeField] private LayerMask interactionLayer;

    private PlayerController playerController;
    private PlayerInputHandler inputHandler;

    private IInteractable currentInteractable;

    private void Awake()
    {
        Debug.Log($"[Interaction] Awake: {gameObject.name}");

        playerController = GetComponent<PlayerController>();
        inputHandler = GetComponent<PlayerInputHandler>();

        Debug.Log($"[Interaction] PlayerController: {playerController}");
        Debug.Log($"[Interaction] InputHandler: {inputHandler}");
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        DetectInteractable();

        if (currentInteractable != null)
        {
           

            if (inputHandler.GetInteractInput())
            {
                Debug.Log("[Interaction] Interact input detected.");

                TryInteract();
            }
        }
    }

    private void DetectInteractable()
    {
        currentInteractable = null;

        if (interactionOrigin == null)
        {
            Debug.LogError(
                "[Interaction] Interaction Origin is NULL!"
            );

            return;
        }

        Ray ray = new Ray(
            interactionOrigin.position,
            interactionOrigin.forward
        );

        Debug.DrawRay(
            ray.origin,
            ray.direction * interactionDistance,
            Color.red
        );

        // Debug.Log(
        //     $"[Interaction] Raycast from: {ray.origin}, " +
        //     $"Direction: {ray.direction}, " +
        //     $"Distance: {interactionDistance}"
        // );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance))
        {
            // Debug.Log("[Interaction] Raycast hit NOTHING.");
            return;
        }

        // Debug.Log(
        //     $"[Interaction] Raycast HIT: " +
        //     $"{hit.collider.gameObject.name}"
        // );

        // Debug.Log(
        //     $"[Interaction] Collider: {hit.collider.name}"
        // );
        //
        // Debug.Log(
        //     $"[Interaction] Layer: " +
        //     $"{LayerMask.LayerToName(hit.collider.gameObject.layer)}"
        // );

        if (InteractionRegistry.TryGet(
                hit.collider,
                out IInteractable interactable))
        {
            // Debug.Log(
            //     $"[Interaction] Registry FOUND interactable: " +
            //     $"{interactable}"
            // );

            currentInteractable = interactable;
        }
        else
        {
            // Debug.Log(
            //     "[Interaction] Registry did NOT find an interactable " +
            //     "for this collider."
            // );
        }
    }

    private void TryInteract()
    {
        if (currentInteractable == null)
        {
            Debug.Log(
                "[Interaction] TryInteract failed: " +
                "currentInteractable is NULL."
            );

            return;
        }

      

        NetworkObject networkObject =
            currentInteractable.NetworkObject;

        if (networkObject == null)
        {
            Debug.LogError(
                "[Interaction] Interactable NetworkObject is NULL!"
            );

            return;
        }

        Debug.Log(
            $"[Interaction] Sending ServerRpc. " +
            $"Target: {networkObject.name}"
        );

        RequestInteractionServerRpc(networkObject);
    }

    [ServerRpc]
    private void RequestInteractionServerRpc(NetworkObject target)
    {
        if (target == null)
        {
            Debug.Log(
                "[Interaction][SERVER] Target is NULL!"
            );

            return;
        }

        Debug.Log(
            $"[Interaction][SERVER] Target GameObject: " +
            $"{target.gameObject.name}"
        );

        IInteractable interactable =
            target.GetComponent<IInteractable>();

        if (interactable == null)
        {
            Debug.Log(
                "[Interaction][SERVER] Target does NOT implement " +
                "IInteractable!"
            );

            return;
        }

        Debug.Log("[SERVER] Interaction requested.");

        interactable.Interact(playerController);
    }
    
    // [ObserversRpc]
    // private void InteractObserversRpc(NetworkObject target)
    // {
    //     if (target == null)
    //         return;
    //
    //     IInteractable interactable =
    //         target.GetComponent<IInteractable>();
    //
    //     if (interactable == null)
    //         return;
    //
    //     Debug.Log("[CLIENT] Calling Interact.");
    //
    //     
    // }

    
}