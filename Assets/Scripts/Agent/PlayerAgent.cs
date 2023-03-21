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

    [SerializeField] private MeshRenderer _visual;

    [SerializeField] private Vector3 _jumpImpulse = new Vector3(0f, 6f, 0f);

    [SerializeField] private NetworkRigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;
    [Range(0, .3f)][SerializeField] private float movementSmoothing = .05f; // How much to smooth out the movement

    private Vector3 velocity = Vector3.zero;

    // Agent INTERFACE

    protected override void OnSpawned()
    {
        name = Object.InputAuthority.ToString();

        // Disable visual for local player
        if(HasInputAuthority)
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

        Move(Owner.Input.FixedInput.Direction.x * moveSpeed * Runner.DeltaTime);

        //// Jump is extrapolated for render as well.
        if (Owner.Input.WasPressed(EInputButton.Jump))
        {
            Jump();
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
            
        }
    }

    protected override void OnLateFixedUpdate()
    {
        
    }

    protected override void ProcessRenderInput()
    {
        if (Owner == null || health.IsAlive == false)
            return;

        // Move(Owner.Input.RenderInput.Direction.x * moveSpeed * Time.deltaTime);

        // Jump is extrapolated for render as well.
        if (Owner.Input.WasPressed(EInputButton.Jump))
        {
            Jump();
        }
    }

    // 5.
    protected override void OnLateRender()
    {

    }




    private void Jump()
    {
        rb.Rigidbody.velocity *= Vector2.right; //Reset y Velocity
        rb.Rigidbody.AddForce(Vector2.up * _jumpImpulse, ForceMode2D.Impulse);
    }

    private void Move(float move)
    {
        // Move the character by finding the target velocity
        Vector3 targetVelocity = new Vector2(move * 10f, rb.ReadVelocity().y);
        // And then smoothing it out and applying it to the character
        rb.Rigidbody.velocity = targetVelocity; // Vector3.SmoothDamp(rb.ReadVelocity(), targetVelocity, ref velocity, movementSmoothing);
    }
}
