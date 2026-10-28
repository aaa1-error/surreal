using UnityEngine;

/// <summary>Gives reserve ammo of one type. Not taken (and stays in the world) while the player is full.</summary>
public class AmmoPickup : Pickup
{
    [SerializeField] private AmmoType _type = AmmoType.Pistol9mm;
    [SerializeField] private int _amount = 17;

    protected override bool TryGive(WeaponController controller)
    {
        return controller.TryAddAmmo(_type, _amount);
    }
}
