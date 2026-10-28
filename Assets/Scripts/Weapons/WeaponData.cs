using System;
using UnityEngine;

/// <summary>Numbers for one fire mode (primary or secondary).</summary>
[Serializable]
public struct FireSettings
{
    [Tooltip("Turn off for weapons without this mode.")]
    public bool available;
    public float damage;
    [Tooltip("Seconds between shots.")]
    public float interval;
    [Tooltip("Half-angle of the random cone, in degrees. 0 = perfectly accurate.")]
    public float spreadDegrees;
    [Tooltip("Rounds taken from the clip per shot.")]
    public int ammoCost;
    [Tooltip("Rays per shot. 1 for a pistol, more for a shotgun.")]
    public int pellets;
}

/// <summary>One asset per weapon. Create via Assets > Create > Weapons > Weapon Data.</summary>
[CreateAssetMenu(menuName = "Weapons/Weapon Data", fileName = "NewWeapon")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Glock 17";
    [Tooltip("Slot number used by the 1/2/3 keys later.")]
    public int slot = 1;

    [Header("Ammo")]
    [Tooltip("Leave empty for weapons that use no ammo (crowbar).")]
    public AmmoTypeData ammoType;
    [Tooltip("0 for weapons that use no ammo (crowbar).")]
    public int clipSize = 17;

    [Header("Timing (seconds)")]
    public float drawTime = 0.8f;
    public float holsterTime = 0.4f;
    public float reloadTime = 1.5f;
    [Tooltip("Delay after pulling the trigger on an empty clip.")]
    public float dryFireInterval = 0.3f;

    [Header("General")]
    public float range = 100f;
    [Tooltip("On = holding the button keeps firing. Off = one shot per click.")]
    public bool holdToFire = true;

    [Header("Fire modes")]
    public FireSettings primary = new FireSettings
    {
        available = true, damage = 8f, interval = 0.3f, spreadDegrees = 0.6f, ammoCost = 1, pellets = 1,
    };

    public FireSettings secondary = new FireSettings
    {
        available = true, damage = 8f, interval = 0.2f, spreadDegrees = 5.7f, ammoCost = 1, pellets = 1,
    };

    public FireSettings Get(FireMode mode) => mode == FireMode.Primary ? primary : secondary;
}
