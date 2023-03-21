using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[OrderBefore(typeof(Agent))]
public class NetworkPlayerInput : ContextBehaviour, IBeforeUpdate, IBeforeTick
{
    [SerializeField] private CharacterInputHandler characterInputHandler;

    [Networked]
    public NetworkBool InputBlocked { get; set; }

    /// <summary>
    /// Holds input for fixed update.
    /// </summary>
    public GameplayInput FixedInput => fixedInput;

    /// <summary>
    /// Holds input for current frame render update.
    /// </summary>
    public GameplayInput RenderInput => renderInput;

    /// <summary>
    /// Holds combined inputs from all render frames since last fixed update. Used when Fusion input poll is triggered.
    /// </summary>
    public GameplayInput CachedInput => cachedInput;

    // We need to store last known input to compare current input against (to track actions activation/deactivation). It is also used if an input for current frame is lost.
    // This is not needed on proxies, only input authority is registered to nameof(PlayerInput) interest group.
    [Networked(nameof(NetworkPlayerInput))]
    private GameplayInput lastKnownInput { get; set; }

    private GameplayInput renderInput;
    private GameplayInput baseRenderInput;

    private GameplayInput fixedInput;
    private GameplayInput baseFixedInput;

    private GameplayInput cachedInput;
    private Vector2 cachedDirection;
    private float cachedDirectionSize;
    private bool _resetCachedInput;

    public override void Spawned()
    {
        // Reset to default state.
        renderInput = default;
        baseRenderInput = default;
        
        fixedInput = default;
        baseFixedInput = default;

        cachedInput = default;
        lastKnownInput = default;

        if (HasStateAuthority == true)
        {
            // Only state and input authority works with input and access _lastKnownInput.
            Object.SetInterestGroup(Object.InputAuthority, nameof(PlayerInput), true);
        }

        if (Runner.LocalPlayer == Object.InputAuthority)
        {
            NetworkEvents events = Runner.GetComponent<NetworkEvents>();
            events.OnInput.RemoveListener(OnInput);
            events.OnInput.AddListener(OnInput);

            Context.Input.RequestCursorLock();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        NetworkEvents events = Runner.GetComponent<NetworkEvents>();
        events.OnInput.RemoveListener(OnInput);

        if (Runner.LocalPlayer == Object.InputAuthority)
        {
            Context.Input.RequestCursorRelease();
        }
    }

    // PRIVATE METHODS

    /// <summary>
    /// 1. Collect input from devices, can be executed multiple times between FixedUpdateNetwork() calls because of faster rendering speed.
    /// </summary>
    void IBeforeUpdate.BeforeUpdate()
    {
        if (!HasInputAuthority)
            return;

        // Store last render input as a base to current render input
        baseRenderInput = renderInput;

        // Reset input for current frame to default
        renderInput = default;

        // Cached input was polled and explicit reset requested
        if (_resetCachedInput)
        {
            _resetCachedInput = false;

            cachedDirection = default;
            cachedDirectionSize = default;
            cachedInput = default;
        }

        if (!Runner.ProvideInput || !Context.Input.IsLocked || InputBlocked)
            return;

        GetStandaloneInput();
    }

    /// <summary>
    /// 2. Push cached input and reset properties, can be executed multiple times within single Unity frame if the rendering speed is slower than Fusion simulation (or there is a performance spike).
    /// </summary>
    private void OnInput(NetworkRunner runner, NetworkInput networkInput)
    {
        if (InputBlocked == true)
            return;

        GameplayInput gameplayInput = cachedInput;

        // Input is polled for single fixed update, but at this time we don't know how many times in a row OnInput() will be executed.
        // This is the reason for having a reset flag instead of resetting input immediately, otherwise we could lose input for next fixed updates (for example move direction).

        _resetCachedInput = true;

        networkInput.Set(gameplayInput);
    }

    /// <summary>
    /// 3. Read input from Fusion. On input authority the FixedInput will match CachedInput.
    /// We have to prepare fixed input before tick so it is ready when read from other objects (agents)
    /// </summary>
    void IBeforeTick.BeforeTick()
    {
        if (InputBlocked)
        {
            fixedInput = default;
            baseFixedInput = default;

            renderInput = default;
            baseRenderInput = default;

            lastKnownInput = default;
            return;
        }

        // Store last known fixed input. This will be compared against new fixed input
        baseFixedInput = lastKnownInput;

        // Set correct fixed input (in case of resimulation, there will be value from the future)
        fixedInput = lastKnownInput;

        if (GetInput<GameplayInput>(out var input))
        {
            fixedInput = input;

            // Update last known input. Will be used next tick as base and fallback
            lastKnownInput = input;
        }

        // The current fixed input will be used as a base to first Render after FUN
        baseRenderInput = fixedInput;
    }

    /// <summary>
    /// Check if the button is set in current input. FUN/Render input is resolved automatically.
    /// </summary>
    public bool IsSet(EInputButton button)
    {
        return Runner.Stage != default ? fixedInput.Buttons.IsSet(button) : renderInput.Buttons.IsSet(button);
    }

    /// <summary>
    /// Check if the button was pressed in current input.
    /// In FUN this method compares current fixed input agains previous fixed input.
    /// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
    /// </summary>
    public bool WasPressed(EInputButton button)
    {
        return Runner.Stage != default ? fixedInput.Buttons.WasPressed(baseFixedInput.Buttons, button) : renderInput.Buttons.WasPressed(baseRenderInput.Buttons, button);
    }

    /// <summary>
    /// Check if the button was released in current input.
    /// In FUN this method compares current fixed input agains previous fixed input.
    /// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
    /// </summary>
    public bool WasReleased(EInputButton button)
    {
        return Runner.Stage != default ? fixedInput.Buttons.WasReleased(baseFixedInput.Buttons, button) : renderInput.Buttons.WasReleased(baseRenderInput.Buttons, button);
    }

    public NetworkButtons GetPressedButtons()
    {
        return Runner.Stage != default ? fixedInput.Buttons.GetPressed(baseFixedInput.Buttons) : renderInput.Buttons.GetPressed(baseRenderInput.Buttons);
    }

    public NetworkButtons GetReleasedButtons()
    {
        return Runner.Stage != default ? fixedInput.Buttons.GetReleased(baseFixedInput.Buttons) : renderInput.Buttons.GetReleased(baseRenderInput.Buttons);
    }



    private void GetStandaloneInput()
    {
        float deltaTime = Time.deltaTime;

        GameplayInput input = new GameplayInput();

        input.Direction = characterInputHandler.Direction;
        input.Buttons.Set(EInputButton.Shoot1, characterInputHandler.IsShooting1);
        input.Buttons.Set(EInputButton.Shoot2, characterInputHandler.IsShooting2);
        input.Buttons.Set(EInputButton.Jump, characterInputHandler.IsJumping);

        renderInput = input;

        // Process cached input for next OnInput() call, represents accumulated inputs for all render frames since last fixed update.

        // Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
        // If the player hits W key in last frame before fixed update, the KCC will move in render update by (velocity * 0.003333f).
        // Treating this input the same way for next fixed update results in KCC moving by (velocity * 0.03333f) - 10x more.
        // Following accumulation proportionally scales move direction so it reflects frames in which input was active.
        // This way the next fixed update will correspond more accurately to what happened in render frames.

        cachedDirection += renderInput.Direction * deltaTime;
        cachedDirectionSize += deltaTime;

        cachedInput.Direction = cachedDirection / cachedDirectionSize;
        cachedInput.Buttons = new NetworkButtons(cachedInput.Buttons.Bits | renderInput.Buttons.Bits);
    }
}
