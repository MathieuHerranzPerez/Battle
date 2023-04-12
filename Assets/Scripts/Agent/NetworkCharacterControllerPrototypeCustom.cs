using Fusion;
using Fusion.KCC;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[OrderBefore(typeof(NetworkTransform))]
[DisallowMultipleComponent]
public class NetworkCharacterControllerPrototypeCustom : NetworkTransform
{
    [Header("Character Controller Settings")]
    [SerializeField] private float gravity = -20.0f;
    [SerializeField] private float jumpImpulse = 8.0f;

    [SerializeField] private float reduceForcedVelocityOverTime = 5f;

    private float playerDesiredVelocityX { get; set; } = 0;

    [Networked] 
    private float localTimeScale { get; set; } = 1f;

    [Networked]
    [HideInInspector]
    public bool IsGrounded { get; set; }

    [Networked]
    private Vector2 velocity { get; set; }

    [Networked]
    private Vector2 forcedVelocity { get; set; } = new Vector2();

    [Networked]
    private float currentYVel { get; set; } = 0;

    /// <summary>
    /// Sets the default teleport interpolation velocity to be the CC's current velocity.
    /// For more details on how this field is used, see <see cref="NetworkTransform.TeleportToPosition"/>.
    /// </summary>
    protected override Vector3 DefaultTeleportInterpolationVelocity => velocity;

    public CharacterController Controller { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        CacheController();
    }

    public override void Spawned()
    {
        base.Spawned();
        CacheController();
    }

    private void CacheController()
    {
        if (Controller == null)
        {
            Controller = GetComponent<CharacterController>();

            Assert.Check(Controller != null, $"An object with {nameof(NetworkCharacterControllerPrototype)} must also have a {nameof(CharacterController)} component.");
        }
    }

    protected override void CopyFromBufferToEngine()
    {
        // Trick: CC must be disabled before resetting the transform state
        Controller.enabled = false;

        // Pull base (NetworkTransform) state from networked data buffer
        base.CopyFromBufferToEngine();

        // Re-enable CC
        Controller.enabled = true;
    }

    public void Move()
    {
        float deltaTime = Runner.DeltaTime * localTimeScale;
        Vector2 previousPos = transform.position;
        Vector2 moveVelocity = new Vector2();

        // gravity
        if (IsGrounded)
        {
            if (currentYVel < -0.1f)
            {
                // Reset velocity
                currentYVel = -0.1f;
                moveVelocity.y = currentYVel;
            }

            if (forcedVelocity.y > 0)
                moveVelocity.y += forcedVelocity.y;

            if(currentYVel > 0)
                moveVelocity.y += currentYVel;
        }
        else
        {
            currentYVel += gravity * deltaTime;
            moveVelocity.y = currentYVel + forcedVelocity.y;
        }

        moveVelocity.x = playerDesiredVelocityX + forcedVelocity.x;
        
        Controller.Move(moveVelocity * deltaTime);

        velocity = (new Vector2(transform.position.x, transform.position.y) - previousPos) * Runner.Simulation.Config.TickRate;
        IsGrounded = Controller.isGrounded;

        ReduceForcedVelocity();
    }

    public virtual void Jump(bool ignoreGrounded = false, float? overrideImpulse = null)
    {
        if (IsGrounded || ignoreGrounded)
        {
            currentYVel = overrideImpulse ?? jumpImpulse;
        }
    }

    public void AddForcedVelocity(Vector2 velocity)
    {
        forcedVelocity += velocity;
    }

    public void SetDesiredPlayerVelocity(float velocityX)
    {
        playerDesiredVelocityX = velocityX;
    }

    public void ChangeTimeLocalTimeScale(float timeScale)
    {
        localTimeScale = timeScale;
    }

    private void ReduceForcedVelocity()
    {
        Vector2 newForcedVelocity = new Vector2();
        if (forcedVelocity.x > 0f)
        {
            newForcedVelocity.x = Mathf.Max(0, forcedVelocity.x - (reduceForcedVelocityOverTime * Runner.DeltaTime * localTimeScale));
        }
        else if (forcedVelocity.x < 0f)
        {
            newForcedVelocity.x = Mathf.Min(0, forcedVelocity.x + (reduceForcedVelocityOverTime * Runner.DeltaTime * localTimeScale));
        }

        if (forcedVelocity.y > 0f)
        {
            newForcedVelocity.y = Mathf.Max(0, forcedVelocity.y - (reduceForcedVelocityOverTime * Runner.DeltaTime * localTimeScale));
        }
        else if (forcedVelocity.y < 0f)
        {
            newForcedVelocity.y = Mathf.Min(0, forcedVelocity.y + (reduceForcedVelocityOverTime * Runner.DeltaTime * localTimeScale));
        }

        forcedVelocity = newForcedVelocity;
    }
}
