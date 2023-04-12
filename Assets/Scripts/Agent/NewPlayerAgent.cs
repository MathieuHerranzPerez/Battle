using Fusion;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class NewPlayerAgent : Agent
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

    [SerializeField] private NetworkCharacterControllerPrototypeCustom ccc;
    [SerializeField] private float _maxWeaponImpulse = 30;
    [SerializeField] private float _minWeaponImpulse = 10;

    [SerializeField] private float slowTimeScale = 0.1f;
    [SerializeField] private float moveSpeed = 5f;

    [Networked] private float lastPointedDirectionX { get; set; } = 1;
    [Networked] private float lastPointedDirectionY { get; set; } = 0;
    [Networked] private NetworkBool isLocked { get; set; } = false;


    private float lastJumpTime = 0f;

    [SerializeField] private float reduceForcedVelocityOverTime = 0.5f;


    // Agent INTERFACE

    protected override void OnSpawned()
    {
        name = Object.InputAuthority.ToString();

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
        if (Owner == null)
            return;

        if (health.IsAlive)
        {
            if (!isLocked)
            {
                ccc.SetDesiredPlayerVelocity(Owner.Input.FixedInput.Direction.x * moveSpeed);

                if (Owner.Input.FixedInput.Buttons.IsSet(EInputButton.Shoot1) && !weapons.CurrentWeapon.IsBusy())
                {
                    isLocked = true;
                    ChangeTimeLocalTimeScale(slowTimeScale);
                }

                if (Owner.Input.WasPressed(EInputButton.Jump))
                {
                    ccc.Jump(true);
                }
            }
            else
            {
                if (Owner.Input.WasReleased(EInputButton.Shoot1))
                {
                    isLocked = false;
                    ChangeTimeLocalTimeScale(1f);

                    Vector2 forcedVelocity = new Vector2(-lastPointedDirectionX, -lastPointedDirectionY) * _maxWeaponImpulse;
                    ccc.AddForcedVelocity(forcedVelocity);
                }
            }

            if (Owner.Input.FixedInput.Direction != Vector2.zero)
            {
                float sign = (Owner.Input.FixedInput.Direction.y < Vector2.right.y) ? -1.0f : 1.0f;
                rotationTransform.localRotation = Quaternion.Euler(0, 0, Vector2.Angle(Vector2.right, Owner.Input.FixedInput.Direction) * sign);

                lastPointedDirectionX = Owner.Input.FixedInput.Direction.x;
                lastPointedDirectionY = Owner.Input.FixedInput.Direction.y;
            }
        }

        ccc.Move();
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

    public void SpawnOnMap()
    {
        ResetStats();
    }

    private void ChangeTimeLocalTimeScale(float timeScale)
    {
        ccc.ChangeTimeLocalTimeScale(timeScale);
    }

    private void ResetStats()
    {
        isLocked = false;
        ccc.ChangeTimeLocalTimeScale(1f);
    }
}
