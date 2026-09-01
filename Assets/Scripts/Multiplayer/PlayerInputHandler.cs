using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jump;
    [SerializeField] private InputActionReference interact;

    private void Start()
    {
        if (interact != null)
            interact.action.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public Vector2 GetLookValue()
    {
        return lookAction.action.ReadValue<Vector2>();
    }

    public bool GetJumpInput()
    {
        return jump.action.WasPressedThisFrame();
    }

    public bool GetInteractInput()
    {
        return interact.action.WasPressedThisFrame();
    }
}