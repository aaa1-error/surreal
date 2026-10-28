using UnityEngine;

/// <summary>
/// Gives the player a weapon, matched by its WeaponData asset. If the player already owns it,
/// only the ammo is taken (and only if there is room for it).
/// The weapon object itself must already exist on the player, listed under Locked Weapons.
/// </summary>
public class WeaponPickup : Pickup
{
    [SerializeField] private WeaponData _weapon;
    [Tooltip("Reserve ammo that comes with the weapon.")]
    [SerializeField] private int _ammoAmount = 17;

    protected override bool TryGive(WeaponController controller)
    {
        return _weapon != null && controller.TryPickupWeapon(_weapon, _ammoAmount);
    }
}
