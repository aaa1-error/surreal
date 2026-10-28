using UnityEngine;

/// <summary>
/// One asset per kind of ammo (9mm, buckshot, ...). Create via Assets > Create > Weapons > Ammo Type.
/// Weapons, pickups and the inventory all point at these assets, so adding a new ammo type needs no code.
/// </summary>
[CreateAssetMenu(menuName = "Weapons/Ammo Type", fileName = "NewAmmo")]
public class AmmoTypeData : ScriptableObject
{
    [Tooltip("Name shown to the player, e.g. on the HUD.")]
    public string displayName = "9mm";

    [Tooltip("The most reserve rounds the player can carry of this type.")]
    [Min(0)]
    public int maxCarry = 250;
}
