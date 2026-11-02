using UnityEngine;
using UnityEngine.InputSystem;

public class InputBridge : MonoBehaviour
{
    private PlatformerController controller;
    private bool isStickHeld = false;

    void Awake()
    {
        controller = GetComponent<PlatformerController>();
    }

    // Link this to the 'Move' event in the Player Input component
    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        // Debug.Log($"Input Received: {input}");
        controller.SetMoveInput(input);
    }

    // Link this to the 'Jump' event in the Player Input component
    public void OnJump(InputAction.CallbackContext context)
    {
        bool isPressed = context.started;
        bool isHeld = context.performed || context.started;

        controller.SetJumpInput(isPressed, isHeld);
    }

    public void OnStick(InputAction.CallbackContext context)
    {
        isStickHeld = context.performed || context.started;
        controller.SetStickInput(isStickHeld);
    }
}