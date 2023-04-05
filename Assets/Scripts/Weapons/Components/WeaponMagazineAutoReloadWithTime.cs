using Fusion;
using UnityEngine;

public class WeaponMagazineAutoReloadWithTime : WeaponComponent
{
    [SerializeField] private int maxAmmo = 3;
    [SerializeField] private bool spawnWithMaxAmmo = true;
    [SerializeField] private float oneBulletReloadTimeSec = 1.5f;

    [Networked]
    private int availableAmmo { get; set; } = 0;

    [Networked]
    private TickTimer nextBulletReloadCountdown { get; set; }

    public override bool IsBusy => availableAmmo <= 0;

    public override void ProcessInput(WeaponContext context, ref WeaponDesires desires, bool weaponBusy)
    {
        if(availableAmmo <= 0) 
            return;

        desires.Reload = false;
        desires.AmmoAvailable = availableAmmo > 0;
    }

    public override void OnFixedUpdate(WeaponContext context, WeaponDesires desires)
    {
        if (availableAmmo < maxAmmo && nextBulletReloadCountdown.Expired(Runner))
        {
            ++availableAmmo;
        }

        if (desires.HasFired)
        {
            --availableAmmo;
        }

        if(availableAmmo < maxAmmo && nextBulletReloadCountdown.ExpiredOrNotRunning(Runner))
        {
            nextBulletReloadCountdown = TickTimer.CreateFromSeconds(Runner, oneBulletReloadTimeSec);
        }
    }

    // NetworkBehaviour INTERFACE

    public override void Spawned()
    {
        availableAmmo = spawnWithMaxAmmo ? maxAmmo : 0;
    }
}
