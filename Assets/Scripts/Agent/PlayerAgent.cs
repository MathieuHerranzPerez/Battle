using Fusion;
using UnityEngine;

public class PlayerAgent : Agent
{
    // PUBLIC MEMBERS

    public Player Owner { get; set; }
    public Weapons Weapons => weapons;
    public Health Health => health;

    // PRIVATE MEMBERS
    [SerializeField] private Health health;
    [SerializeField] private Weapons weapons;
    [SerializeField] private Transform rotationTransform;

    [SerializeField] private MeshRenderer _visual;

    [SerializeField] private Vector3 _jumpImpulse = new Vector3(0f, 6f, 0f);

    [SerializeField] private NetworkRigidbody rb;
    [SerializeField] private float moveSpeed = 5f;
    [Range(0, .3f)][SerializeField] private float movementSmoothing = .05f; // How much to smooth out the movement

    private bool isLocked = false;
    private Vector3 previousVelocity = Vector3.zero;

    private float lastJumpTime = 0f;

    // Agent INTERFACE

    protected override void OnSpawned()
    {
        name = Object.InputAuthority.ToString();

        // Disable visual for local player
        if (HasInputAuthority)
        {
            _visual.material.color = new Color(113, 230, 134);
        }

        weapons.OnSpawned();
    }

    protected override void OnDespawned()
    {
        Owner = null;
    }

    protected override void ProcessEarlyFixedInput()
    {
        if (Owner == null || health.IsAlive == false)
            return;

        if (!isLocked)
        {
            Move(Owner.Input.FixedInput.Direction.x * moveSpeed * Runner.DeltaTime);

            //// Jump is extrapolated for render as well.
            if (Owner.Input.WasPressed(EInputButton.Jump))
            {
                Jump();
            }

            if (Owner.Input.FixedInput.Buttons.IsSet(EInputButton.Shoot1))
            {
                isLocked = true;
                previousVelocity = rb.Rigidbody.velocity;

                rb.Rigidbody.useGravity = false;
                rb.Rigidbody.velocity = Vector2.zero;
            }
        }
        else
        {
            if (Owner.Input.WasReleased(EInputButton.Shoot1))
            {
                isLocked = false;

                rb.Rigidbody.useGravity = true;
                rb.Rigidbody.velocity = previousVelocity;
            }
        }

        if (Owner.Input.FixedInput.Direction != Vector2.zero)
        {
            float sign = (Owner.Input.FixedInput.Direction.y < Vector2.right.y) ? -1.0f : 1.0f;
            rotationTransform.localRotation = Quaternion.Euler(0, 0, Vector2.Angle(Vector2.right, Owner.Input.FixedInput.Direction) * sign);
        }
    }

    protected override void OnFixedUpdate()
    {

    }

    protected override void ProcessLateFixedInput()
    {
        // Executed after HitboxManager. Process other non-movement actions like shooting.

        if (Owner != null)
        {
            Weapons.ProcessInput(Owner.Input);
        }
    }

    protected override void OnLateFixedUpdate()
    {
        Weapons.OnLateFixedUpdate();
    }

    protected override void ProcessRenderInput()
    {
        if (Owner == null || health.IsAlive == false)
            return;

        if (!isLocked)
        {
            Move(Owner.Input.RenderInput.Direction.x * moveSpeed * Time.deltaTime);

            // Jump is extrapolated for render as well.
            if (Owner.Input.WasPressed(EInputButton.Jump))
            {
                Jump();
            }

            if (Owner.Input.RenderInput.Buttons.IsSet(EInputButton.Shoot1))
            {
                isLocked = true;
                previousVelocity = rb.Rigidbody.velocity;

                rb.Rigidbody.useGravity = false;
                rb.Rigidbody.velocity = Vector2.zero;
            }
        }
        else
        {
            if (Owner.Input.WasReleased(EInputButton.Shoot1))
            {
                isLocked = false;

                rb.Rigidbody.useGravity = true;
                rb.Rigidbody.velocity = previousVelocity;
            }
        }

        if (Owner.Input.RenderInput.Direction != Vector2.zero)
        {
            float sign = (Owner.Input.RenderInput.Direction.y < Vector2.right.y) ? -1.0f : 1.0f;
            rotationTransform.localRotation = Quaternion.Euler(0, 0, Vector2.Angle(Vector2.right, Owner.Input.RenderInput.Direction) * sign);
        }
    }

    // 5.
    protected override void OnLateRender()
    {
        Weapons.OnRender();
    }




    private void Jump()
    {
        if (Time.time - lastJumpTime < Runner.DeltaTime)
            return;

        lastJumpTime = Time.time;
        rb.Rigidbody.velocity *= Vector2.right; //Reset y Velocity
        rb.Rigidbody.AddForce(Vector2.up * _jumpImpulse, ForceMode.Impulse);
    }

    private void Move(float move)
    {
        // Move the character by finding the target velocity
        Vector3 targetVelocity = new Vector2(move * 10f, rb.ReadVelocity().y);
        // And then smoothing it out and applying it to the character
        rb.Rigidbody.velocity = targetVelocity; // Vector3.SmoothDamp(rb.ReadVelocity(), targetVelocity, ref velocity, movementSmoothing);
    }
}
