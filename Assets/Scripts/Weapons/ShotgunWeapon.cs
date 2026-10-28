using UnityEngine;

/// <summary>
/// A hitscan weapon that reloads one shell at a time and lets you fire mid-reload.
/// Pellets, spread, the double-barrel alt fire (ammo cost 2) and every number come from its WeaponData.
/// In that asset, Reload Time is the time per shell.
/// </summary>
public class ShotgunWeapon : HitscanWeapon
{
    protected override bool CanInterruptReload => true;

    protected override bool FinishReload()
    {
        AmmoInClip += _ammo.Take(_data.ammoType, 1);
        Log($"Shell loaded: {AmmoInClip}/{_data.clipSize} in clip, {AmmoReserve} in reserve");

        // Ask for another step. The base class only continues while there is room and reserve ammo.
        return true;
    }
}
