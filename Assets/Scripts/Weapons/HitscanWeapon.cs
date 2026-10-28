using UnityEngine;

/// <summary>
/// A weapon whose shot is one or more instant-hit rays. The pistol is exactly this class
/// with a pistol WeaponData asset; the shotgun will extend it with shell-by-shell reloading.
/// </summary>
public class HitscanWeapon : Weapon
{
    protected override void OnFire(FireMode mode, FireSettings settings)
    {
        GameObject source = transform.root.gameObject;

        for (int i = 0; i < settings.pellets; i++)
        {
            HitscanShot.Fire(
                _aimOrigin,
                settings.spreadDegrees,
                _data.range,
                _hitLayers,
                settings.damage,
                source,
                out _);
        }
    }
}
