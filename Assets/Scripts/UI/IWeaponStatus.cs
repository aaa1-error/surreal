using System;

public interface IWeaponStatus
{
    string DisplayName { get; }
    int AmmoInClip { get; }
    int AmmoReserve { get; }
    event Action Changed;   // clip or reserve changed
}

public interface IWeaponProvider
{
    IWeaponStatus CurrentWeapon { get; }   // may be null
    event Action WeaponChanged;
}
