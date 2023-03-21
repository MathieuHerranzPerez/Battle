using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInputHandler : MonoBehaviour
{
    public bool IsShooting1 { get; private set; } = false;
    public bool IsShooting2 { get; private set; } = false;
    public bool IsJumping { get; private set; } = false;
    public Vector2 Direction { get; private set; } = Vector2.zero;

    public void HandleShoot1(InputAction.CallbackContext context)
    {
        IsShooting1 = context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed;
    }

    public void HandleShoot2(InputAction.CallbackContext context) 
    {
        IsShooting2 = context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed;
    }

    public void HandleJump(InputAction.CallbackContext context)
    {
        IsJumping = context.phase == InputActionPhase.Started || context.phase == InputActionPhase.Performed;
    }

    public void HandleDirection(InputAction.CallbackContext context)
    {
        Direction = context.ReadValue<Vector2>();
    }
}
