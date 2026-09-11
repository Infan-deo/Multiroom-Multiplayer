using FishNet.Object;
using UnityEngine;


[RequireComponent(typeof(PlayerInputHandler))]
public class InteractionSystem : NetworkBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactionOrigin;
    [SerializeField] private float interactionDistance = 3f;
    // [SerializeField] private LayerMask interactionLayer;

    [SerializeField]private NetworkObject _playerNetworkObject;
    private PlayerInputHandler inputHandler;

    private IInteractable currentInteractable;

    private bool _canInteract;

    private void Awake()
    {


        if (!TryGetComponent(out _playerNetworkObject))
        {
            Debug.Log(_playerNetworkObject is null);
        }
        inputHandler = GetComponent<PlayerInputHandler>();
        _canInteract = true;


    }

    private void Update()
    {
        if (!IsOwner)
            return;
        if (!_canInteract)
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

       

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance))
        {
            return;
        }

       

        if (InteractionRegistry.TryGet(
                hit.collider,
                out IInteractable interactable))
        {
          

            currentInteractable = interactable;
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

        interactable.Interact(_playerNetworkObject);
    }
    
    public void SetCanInteract(bool state)
    {
        _canInteract = state;
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