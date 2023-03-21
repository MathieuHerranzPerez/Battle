using Fusion;
using System;
using UnityEngine;

// https://doc.photonengine.com/fusion/current/game-samples/fusion-br/player

[OrderAfter(typeof(BeforeHitboxManagerUpdater))]
[OrderBefore(typeof(HitboxManager), typeof(AfterHitboxManagerUpdater))]
[RequireComponent(typeof(BeforeHitboxManagerUpdater), typeof(AfterHitboxManagerUpdater))]
public abstract class Agent : ContextBehaviour
{
    // PUBLIC MEMBERS

    public event Action<Agent> AgentDespawned;

    // NetworkBehaviour INTERFACE

    public override sealed void Spawned()
    {
        OnSpawned();
    }

    public override sealed void Despawned(NetworkRunner runner, bool hasState)
    {
        OnDespawned();

        AgentDespawned?.Invoke(this);
        AgentDespawned = null;
    }

    /// <summary>
    /// 2. Regular fixed update for agents
    /// </summary>
    public override sealed void FixedUpdateNetwork()
    {
        // At this point all agents (including proxies) have set their positions and rotations, we can run some post-processing (setting camera pivots, synchronizing other owned objects, ...).

        OnFixedUpdate();
    }

    /// <summary>
    /// 5. Regular render update for Agent
    /// </summary>
    public override sealed void Render()
    {
        // At this point all agents have set their positions and rotations, we can run some post-processing (setting camera pivots, synchronizing other owned objects, ...).

        OnRender();
    }

    // Agent INTERFACE

    protected abstract void ProcessEarlyFixedInput();
    protected abstract void ProcessLateFixedInput();
    protected abstract void ProcessRenderInput();

    protected virtual void OnSpawned() { }
    protected virtual void OnDespawned() { }
    protected virtual void OnEarlyFixedUpdate() { }
    protected virtual void OnFixedUpdate() { }
    protected virtual void OnLateFixedUpdate() { }
    protected virtual void OnEarlyRender() { }
    protected virtual void OnRender() { }
    protected virtual void OnLateRender() { }

    // MONOBEHAVIOUR

    protected virtual void Awake()
    {
        // All agents have BeforeHitboxManagerUpdater and AfterHitboxManagerUpdater component.

        // BeforeHitboxManagerUpdater provides callbacks which are executed before HitboxManager => we use this to process "movement" input - set move direction, jump, look rotation, ...

        var beforeUpdater = GetComponent<BeforeHitboxManagerUpdater>();
        beforeUpdater.SetDelegates(EarlyFixedUpdate, EarlyRender);

        // AfterHitboxManagerUpdater provides callbacks which are executed after HitboxManager => we use this to process "non-movement" input - shooting, actions, ...

        var afterUpdater = GetComponent<AfterHitboxManagerUpdater>();
        afterUpdater.SetDelegates(LateFixedUpdate, LateRender);
    }

    // PRIVATE METHODS

    /// <summary>
    /// 1. At this point new input is gathered so process movement part of it before updating positions in HitboxManager
    /// </summary>
    private void EarlyFixedUpdate()
    {
        // This method expects derived classes to make movement / look related calls to KCC.
        ProcessEarlyFixedInput();

        // This method can be used to post-process KCC update (Transform is already updated as well).
        // This is executed before any of Agent and HitboxManager FixedUpdateNetwork().
        OnEarlyFixedUpdate();
    }

    /// <summary>
    /// 3. Executed after all Agent and HitboxManager FixedUpdateNetwork() calls, process rest of player input (shooting, other non-movement related actions).
    /// </summary>
    private void LateFixedUpdate()
    {
        if (IsProxy == false)
        {
            ProcessLateFixedInput();
        }

        // This method can be used to react on player actions. At this point player input has been processed completely.
        OnLateFixedUpdate();
    }

    /// <summary>
    /// 4. Process input for render update. Only input and state authority will make changes, proxies are already interpolated.
    /// </summary>
    private void EarlyRender()
    {
        if (HasInputAuthority == true)
        {
            // This method expects derived classes to make movement / look related calls to KCC.
            ProcessRenderInput();
        }

        // This method can be used to post-process KCC update (Transform is already updated as well).
        // This is executed before any of Agent Render().
        OnEarlyRender();
    }

    /// <summary>
    /// 6. Executed after all Agent Render() calls
    /// </summary>
    private void LateRender()
    {
        // Here comes "late" render input processing of all other non-movement actions.
        // This gives you extra responsivity at the cost of maintaining extrapolation and reconcilliation.
        // Currently there are no specific actions extrapolated for render.

        // This method can be used to override final state of the object for render. At this point player input has been processed completely.
        OnLateRender();
    }
}