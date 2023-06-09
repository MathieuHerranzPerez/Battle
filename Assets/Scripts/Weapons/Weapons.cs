using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class WeaponContext
{
    public IProjectileManager ProjectileManager;

    public Vector3 FireDirection;
    public Vector3 FirePosition;

    public NetworkButtons Input;
    public NetworkButtons PressedInput;
    public NetworkButtons ReleasedInput;

    public int OwnerObjectInstanceID;
}

[RequireComponent(typeof(ProjectileManager))]
public class Weapons : ContextBehaviour
{
    // PUBLIC MEMBERS

    public bool IsSwitchingWeapon => _switchCooldown.ExpiredOrNotRunning(Runner) == false;
    public float ElapsedSwitchTime => _weaponSwitchDuration - _switchCooldown.RemainingTime(Runner).GetValueOrDefault();

    public Weapon CurrentWeapon => _weapons[CurrentWeaponSlot];
    public Weapon PendingWeapon => _weapons[PendingWeaponSlot];

    [Networked, HideInInspector]
    public int CurrentWeaponSlot { get; private set; }
    [Networked(OnChanged = nameof(OnPendingWeaponChanged), OnChangedTargets = OnChangedTargets.All), HideInInspector]
    public int PendingWeaponSlot { get; private set; }

    public int Version => _version;

    // PRIVATE MEMBERS

    [Networked(OnChanged = nameof(OnWeaponsChanged), OnChangedTargets = OnChangedTargets.All), Capacity(12)]
    private NetworkArray<Weapon> _weapons { get; }
    [Networked]
    private TickTimer _switchCooldown { get; set; }

    [SerializeField] private Weapon[] _initialWeapons;
    [SerializeField] private Transform _weaponsRoot;
    [SerializeField] private Transform _fireTransform;

    [Header("Weapon Switch")]
    [SerializeField]
    private float _weaponSwitchDuration = 1f;
    [SerializeField, Tooltip("When the actual weapon swap happens during weapon switch")]
    private float _weaponSwapTime = 0.5f;

    private int _version;

    private ProjectileManager _projectileManager;
    private WeaponContext _context = new WeaponContext();

    private bool _forceWeaponsRefresh;

    // PUBLIC METHODS

    public void ProcessInput(NetworkPlayerInput input)
    {
        if (IsProxy)
            return;

        if (CurrentWeapon == null)
            return;

        SwitchWeapon(input.FixedInput.WeaponSlot, false);

        if (!IsSwitchingWeapon)
        {
            _context.Input = input.FixedInput.Buttons;
            _context.PressedInput = input.GetPressedButtons();
            _context.ReleasedInput = input.GetReleasedButtons();
            _context.FirePosition = _fireTransform.position;
            _context.FireDirection = _fireTransform.rotation * Vector3.right;

            CurrentWeapon.ProcessInput(_context);
        }
    }

    public void SwitchWeapon(int weaponSlot, bool immediate)
    {
        if (weaponSlot < 0 || weaponSlot >= _weapons.Length)
            return;

        var weapon = _weapons[weaponSlot];
        if (weapon == null)
            return;

        if (immediate == true || _weaponSwitchDuration <= 0f)
        {
            PendingWeaponSlot = weaponSlot;
            CurrentWeaponSlot = weaponSlot;
            _switchCooldown = default;
        }
        else
        {
            StartWeaponSwitch(weaponSlot);
        }
    }

    public void OnSpawned()
    {
        _projectileManager.OnSpawned();

        if (HasStateAuthority == false)
            return;

        int minWeaponSlot = 0;

        // Spawn initial weapons
        for (int i = 0; i < _initialWeapons.Length; i++)
        {
            var weaponPrefab = _initialWeapons[i];
            if (weaponPrefab == null)
                continue;

            var weapon = Runner.Spawn(weaponPrefab, inputAuthority: Object.InputAuthority);
            AddWeapon(weapon);

            if (minWeaponSlot == 0 || minWeaponSlot > weapon.WeaponSlot)
            {
                minWeaponSlot = weapon.WeaponSlot;
            }
        }

        // Equip first weapon
        SwitchWeapon(minWeaponSlot, true);
    }

    public void OnLateFixedUpdate()
    {
        UpdateWeaponSwitch();

        _projectileManager.OnFixedUpdate();
    }

    public void OnRender()
    {
        if (CurrentWeapon == null)
            return;

        _context.FirePosition = _fireTransform.position;
        _context.FireDirection = _fireTransform.rotation * Vector3.right;

        RefreshWeapons();

        _projectileManager.OnRender(CurrentWeapon.BarrelTransforms);
        CurrentWeapon.OnRender(_context);
    }

    public void OnDespawned()
    {
        // Cleanup weapons
        for (int i = 0; i < _weapons.Length; i++)
        {
            var weapon = _weapons[i];
            if (weapon != null)
            {
                RemoveWeapon(weapon.WeaponSlot, true);
            }
        }
    }

    public int GetNextWeaponSlot(int fromSlot, bool ignoreZeroWeapon = false)
    {
        int weaponsLength = _weapons.Length;

        for (int i = 0; i < weaponsLength; i++)
        {
            int slot = (fromSlot + i + 1) % weaponsLength;

            if (slot == 0 && ignoreZeroWeapon == true)
                continue;

            if (_weapons[slot] != null)
                return slot;
        }

        return 0;
    }

    public int GetPreviousWeaponSlot(int fromSlot, bool ignoreZeroWeapon = false)
    {
        int weaponsLength = _weapons.Length;

        for (int i = 0; i < weaponsLength; i++)
        {
            int slot = (weaponsLength + fromSlot - i - 1) % weaponsLength;

            if (slot == 0 && ignoreZeroWeapon == true)
                continue;

            if (_weapons[slot] != null)
                return slot;
        }

        return 0;
    }

    public void GetAllWeapons(List<Weapon> weapons)
    {
        for (int i = 0; i < _weapons.Length; i++)
        {
            if (_weapons[i] != null)
            {
                weapons.Add(_weapons[i]);
            }
        }
    }

    // MONOBEHAVIOUR

    protected void Awake()
    {
        _projectileManager = GetComponent<ProjectileManager>();

        _context.ProjectileManager = _projectileManager;
        _context.OwnerObjectInstanceID = gameObject.GetInstanceID();
    }

    // PRIVATE METHODS

    private void StartWeaponSwitch(int weaponSlot)
    {
        if (weaponSlot == PendingWeaponSlot)
            return;

        PendingWeaponSlot = weaponSlot;

        if (ElapsedSwitchTime < _weaponSwapTime)
            return; // We haven't swap weapon yet, just continue with new pending weapon

        _switchCooldown = TickTimer.CreateFromSeconds(Runner, _weaponSwitchDuration);
    }

    private void UpdateWeaponSwitch()
    {
        if (IsProxy == true)
            return;

        if (CurrentWeaponSlot == PendingWeaponSlot)
            return;

        if (ElapsedSwitchTime < _weaponSwapTime)
            return;

        CurrentWeaponSlot = PendingWeaponSlot;
    }

    private void RefreshWeapons()
    {
        Weapon currentWeapon = CurrentWeapon;

        if (_forceWeaponsRefresh == false && currentWeapon.IsArmed == true)
            return; // Proper weapon is ready

        for (int i = 0; i < _weapons.Length; i++)
        {
            var weapon = _weapons[i];

            if (weapon == null)
                continue;

            if (weapon != currentWeapon)
            {
                weapon.DisarmWeapon();
                weapon.gameObject.SetActive(false);
            }
        }

        currentWeapon.transform.SetParent(_weaponsRoot, false);
        currentWeapon.gameObject.SetActive(true);

        currentWeapon.ArmWeapon();

        _forceWeaponsRefresh = false;
    }

    private void AddWeapon(Weapon weapon)
    {
        if (weapon == null)
            return;

        RemoveWeapon(weapon.WeaponSlot);

        weapon.Object.AssignInputAuthority(Object.InputAuthority);

        _weapons.Set(weapon.WeaponSlot, weapon);
    }

    private void RemoveWeapon(int slot, bool despawn = true)
    {
        var weapon = _weapons[slot];
        if (weapon == null)
            return;

        if (despawn == true)
        {
            Runner.Despawn(weapon.Object);
        }
        else
        {
            weapon.Object.RemoveInputAuthority();
        }

        _weapons.Set(slot, null);
    }

    // NETWORK CALLBACKS

    public static void OnPendingWeaponChanged(Changed<Weapons> changed)
    {
        //changed.Behaviour._agent.Effects.PlaySound(changed.Behaviour._weaponSwitchSound);
    }

    public static void OnWeaponsChanged(Changed<Weapons> changed)
    {
        changed.Behaviour._forceWeaponsRefresh = true;
        changed.Behaviour._version++;
    }
}