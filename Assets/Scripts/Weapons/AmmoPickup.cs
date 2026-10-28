using UnityEngine;

/// <summary>Gives reserve ammo of one type. Not taken (and stays in the world) while the player is full.</summary>
public class AmmoPickup : Pickup
{
    [SerializeField] private AmmoTypeData _type;
    [SerializeField] private int _amount = 17;

    protected override bool TryGive(WeaponController controller)
    {
        return _type != null && controller.TryAddAmmo(_type, _amount);
    }
}
