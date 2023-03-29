using Fusion;
using System;
using UnityEngine;

public class WeaponChargeTrigger : WeaponComponent
{
    [SerializeField] private int _cadence = 600;
    [SerializeField] private EInputButton _fireButton = EInputButton.Shoot1;
    [SerializeField] private float maxChargeTime = 1f;

    [Networked] private TickTimer _fireCooldown { get; set; }
    [Networked] private float chargeTime { get; set; } = 0f;

    private int _fireTicks;

    // WeaponComponent INTERFACE

    public override bool IsBusy => _fireCooldown.ExpiredOrNotRunning(Runner) == false;

    public event Action<float> OnChargeChanged;

    public override void ProcessInput(WeaponContext context, ref WeaponDesires desires, bool weaponBusy)
    {
        if (weaponBusy)
            return;

        if (!_fireCooldown.ExpiredOrNotRunning(Runner))
            return;

        NetworkButtons fireInput = context.Input;

        if(fireInput.IsSet(_fireButton))
        {
            chargeTime += Runner.DeltaTime;
            chargeTime = Mathf.Min(chargeTime, maxChargeTime);

            OnChargeChanged?.Invoke(chargeTime / maxChargeTime);
        }
        else
        {
            if(chargeTime > 0)
            {
                desires.Fire = true;
                float progress = chargeTime / maxChargeTime;
                desires.ChargeValue = progress;
                chargeTime = 0f;

                OnChargeChanged?.Invoke(0);
            }
        }
    }

    public override void OnFixedUpdate(WeaponContext context, WeaponDesires desires)
    {
        if (desires.HasFired == true)
        {
            _fireCooldown = TickTimer.CreateFromTicks(Runner, _fireTicks);
        }
    }

    // NetworkBehaviour INTERFACE

    public override void Spawned()
    {
        base.Spawned();

        float fireTime = 60f / _cadence;
        _fireTicks = (int)System.Math.Ceiling(fireTime / (double)Runner.DeltaTime);
    }
}
