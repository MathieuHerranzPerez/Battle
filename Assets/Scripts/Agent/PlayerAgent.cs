using Fusion;
using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
    [SerializeField] private float _maxWeaponImpulse = 30;
    [SerializeField] private float _minWeaponImpulse = 10;

    [SerializeField] private NetworkRigidbody rb;
    [SerializeField] private float moveSpeed = 5f;
    [Range(0, .3f)][SerializeField] private float movementSmoothing = .05f; // How much to smooth out the movement

    [SerializeField] private float slowTimeScale = 0.1f;
    [SerializeField] private float gravityMultiplicator = 0f;

    [Networked] private float lastPointedDirectionX { get; set; } = 1;
    [Networked] private float lastPointedDirectionY { get; set; } = 0;

    private bool isLocked = false;

    private float lastJumpTime = 0f;

    [SerializeField] private float reduceForcedVelocityOverTime = 0.5f;

    private float defaultMass;
    private Vector3 defaultVelocity;
    private Vector3 defaultAngularVelocity;

    public void SpawnOnMap()
    {
        ResetStats();
    }

    void OnEnable()
    {
        health.FatalHitTaken += HandleFataleHitTaken;
    }

    void OnDisable()
    {
        health.FatalHitTaken += HandleFataleHitTaken;
    }

    private void HandleFataleHitTaken(HitData obj)
    {
        if (isLocked)
            ChangeTimeLocalTimeScale(1 / slowTimeScale);
    }

    // Agent INTERFACE

    protected override void OnSpawned()
    {
        name = Object.InputAuthority.ToString();

        defaultMass = rb.Rigidbody.mass;
        defaultVelocity = rb.Rigidbody.velocity;
        defaultAngularVelocity = rb.Rigidbody.angularVelocity;

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
        {
            ProcessGravity();
            return;
        }

        if (!isLocked)
        {
            Move(Owner.Input.FixedInput.Direction.x * moveSpeed * Runner.DeltaTime);

            if (Owner.Input.FixedInput.Buttons.IsSet(EInputButton.Shoot1) && !weapons.CurrentWeapon.IsBusy())
            {
                isLocked = true;
                ChangeTimeLocalTimeScale(slowTimeScale);
            }

            if (Owner.Input.WasPressed(EInputButton.Jump))
            {
                Jump();
            }            
        }
        else
        {
            if (Owner.Input.WasReleased(EInputButton.Shoot1))
            {
                isLocked = false;
                ChangeTimeLocalTimeScale(1/ slowTimeScale);

                AddForceImpulse(new Vector3(-lastPointedDirectionX, -lastPointedDirectionY, 0) * _maxWeaponImpulse);
            }
        }

        ReduceVelocity();

        if (Owner.Input.FixedInput.Direction != Vector2.zero)
        {
            float sign = (Owner.Input.FixedInput.Direction.y < Vector2.right.y) ? -1.0f : 1.0f;
            rotationTransform.localRotation = Quaternion.Euler(0, 0, Vector2.Angle(Vector2.right, Owner.Input.FixedInput.Direction) * sign);

            lastPointedDirectionX = Owner.Input.FixedInput.Direction.x;
            lastPointedDirectionY = Owner.Input.FixedInput.Direction.y;
        }

        ProcessGravity();
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


    private void ProcessGravity()
    {
        float dt = isLocked ? Runner.DeltaTime * slowTimeScale : Runner.DeltaTime;
        rb.Rigidbody.velocity += Physics.gravity * gravityMultiplicator * dt;
    }

    private void Jump()
    {
        if (Time.time - lastJumpTime < Runner.DeltaTime)
            return;

        lastJumpTime = Time.time;
        rb.Rigidbody.velocity *= Vector2.right; //Reset y Velocity
        Vector3 impulse = isLocked ? _jumpImpulse * slowTimeScale : _jumpImpulse;
        AddForceImpulse(Vector2.up * impulse);
    }

    private void AddForceImpulse(Vector3 impulse)
    {
        rb.Rigidbody.AddForce(impulse, ForceMode.VelocityChange);
    }

    private void Move(float move)
    {
        rb.Rigidbody.MovePosition(rb.Rigidbody.position + new Vector3(move, 0, 0));
    }

    private void ReduceVelocity()
    {
        float newVelocityX;
        if (rb.Rigidbody.velocity.x > 0.01f)
        {
            newVelocityX = Mathf.Max(0, rb.Rigidbody.velocity.x - (reduceForcedVelocityOverTime * Runner.DeltaTime));
            rb.Rigidbody.velocity = new Vector3(newVelocityX, rb.Rigidbody.velocity.y, 0);
        }
        else if(rb.Rigidbody.velocity.x < 0.01f)
        {
            newVelocityX = Mathf.Min(0, rb.Rigidbody.velocity.x + (reduceForcedVelocityOverTime * Runner.DeltaTime));
            rb.Rigidbody.velocity = new Vector3(newVelocityX, rb.Rigidbody.velocity.y, 0);
        }
    }

    private void ChangeTimeLocalTimeScale(float timeScale)
    {
        rb.Rigidbody.mass /= timeScale;
        rb.Rigidbody.velocity *= timeScale;
        rb.Rigidbody.angularVelocity *= timeScale;
    }

    private void ResetStats()
    {
        isLocked = false;
        rb.Rigidbody.mass = defaultMass;
        rb.Rigidbody.velocity = defaultVelocity;
        rb.Rigidbody.angularVelocity = defaultAngularVelocity;
    }
}
