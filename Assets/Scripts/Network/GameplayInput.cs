using Fusion;
using UnityEngine;

public enum EInputButton
{
    Shoot1,
    Shoot2,
    Jump,
    Reload
}

public struct GameplayInput : INetworkInput
{
    public int WeaponSlot => WeaponButton - 1;

    public Vector2 Direction;
    public NetworkButtons Buttons;
    public byte WeaponButton;
}
