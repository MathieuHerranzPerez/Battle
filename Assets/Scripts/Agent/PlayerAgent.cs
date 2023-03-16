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

    [SerializeField] private GameObject _visual;

    [SerializeField] private Vector3 _jumpImpulse = new Vector3(0f, 6f, 0f);

    // Agent INTERFACE

    protected override void OnSpawned()
    {
        name = Object.InputAuthority.ToString();

        // Disable visual for local player
        _visual.SetActive(HasInputAuthority == false);

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
    }

    // 5.
    protected override void OnLateRender()
    {

    }
}
