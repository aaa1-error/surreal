using UnityEngine;


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
