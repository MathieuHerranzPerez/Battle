using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class Gameplay : ContextBehaviour
{
    // PUBLIC MEMBERS

    [Networked, HideInInspector, Capacity(200)]
    public NetworkDictionary<PlayerRef, Player> Players { get; }

    // PRIVATE METHODS

    private SpawnPoint[] _spawnPoints;
    private int _lastSpawnPoint = -1;

    private List<SpawnRequest> _spawnRequests = new List<SpawnRequest>();

    // PUBLIC METHODS

    public void Join(Player player)
    {
        if (HasStateAuthority == false)
            return;

        var playerRef = player.Object.InputAuthority;

        if (Players.ContainsKey(playerRef) == true)
        {
            Debug.LogError($"Player {playerRef} already joined");
            return;
        }

        Players.Add(playerRef, player);

        OnPlayerJoined(player);
    }

    public void Leave(Player player)
    {
        if (HasStateAuthority == false)
            return;

        if (Players.ContainsKey(player.Object.InputAuthority) == false)
            return;

        Players.Remove(player.Object.InputAuthority);

        OnPlayerLeft(player);
    }

    // NetworkBehaviour INTERFACE

    public override void Spawned()
    {
        // Register to context
        Context.Gameplay = this;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
            return;

        int currentTick = Runner.Tick;

        for (int i = _spawnRequests.Count - 1; i >= 0; i--)
        {
            var request = _spawnRequests[i];

            if (request.Tick > currentTick)
                continue;

            _spawnRequests.RemoveAt(i);

            if (request.Player == null || request.Player.Object == null)
                continue; // Player no longer valid

            if (Players.ContainsKey(request.Player.Object.InputAuthority) == false)
                continue; // Player left gameplay

            SpawnPlayerAgent(request.Player);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Clear from context
        Context.Gameplay = null;
    }

    // PROTECTED METHODS

    protected virtual void OnPlayerJoined(Player player)
    {
        SpawnPlayerAgent(player);
    }

    protected virtual void OnPlayerLeft(Player player)
    {
        DespawnPlayerAgent(player);
    }

    protected virtual void OnPlayerDeath(Player player)
    {
        AddSpawnRequest(player, 3f);
    }

    protected virtual void OnPlayerAgentSpawned(NewPlayerAgent agent)
    {
        agent.Health.SetImmortality(3f);
    }

    protected virtual void OnPlayerAgentDespawned(NewPlayerAgent agent)
    {
    }

    protected void SpawnPlayerAgent(Player player)
    {
        DespawnPlayerAgent(player);

        NewPlayerAgent agent = SpawnAgent(player.Object.InputAuthority, player.AgentPrefab) as NewPlayerAgent;
        agent.SpawnOnMap();
        player.AssignAgent(agent);

        agent.Health.FatalHitTaken += OnFatalHitTaken;

        OnPlayerAgentSpawned(agent);
    }

    protected void DespawnPlayerAgent(Player player)
    {
        if (player.ActiveAgent == null)
            return;

        player.ActiveAgent.Health.FatalHitTaken -= OnFatalHitTaken;

        OnPlayerAgentDespawned(player.ActiveAgent);

        DespawnAgent(player.ActiveAgent);
        player.ClearAgent();
    }

    protected void AddSpawnRequest(Player player, float spawnDelay)
    {
        int delayTicks = Mathf.RoundToInt(Runner.Simulation.Config.TickRate * spawnDelay);

        _spawnRequests.Add(new SpawnRequest()
        {
            Player = player,
            Tick = Runner.Tick + delayTicks,
        });
    }

    // PRIVATE METHODS

    private void OnFatalHitTaken(HitData hitData)
    {
        var health = hitData.Target as Health;

        if (health == null)
            return;

        if (Players.TryGet(health.Object.InputAuthority, out Player player) == true)
        {
            OnPlayerDeath(player);
        }
    }

    private Agent SpawnAgent(PlayerRef inputAuthority, Agent agentPrefab)
    {
        if (_spawnPoints == null)
        {
            _spawnPoints = Runner.SimulationUnityScene.FindObjectsOfTypeInOrder<SpawnPoint>(true);
        }

        _lastSpawnPoint = (_lastSpawnPoint + 1) % _spawnPoints.Length;
        var spawnPoint = _spawnPoints[_lastSpawnPoint].transform;

        var agent = Runner.Spawn(agentPrefab, spawnPoint.position, spawnPoint.rotation, inputAuthority);
        return agent;
    }

    private void DespawnAgent(Agent agent)
    {
        if (agent == null)
            return;

        Runner.Despawn(agent.Object);
    }

    // HELPERS

    public struct SpawnRequest
    {
        public Player Player;
        public int Tick;
    }
}