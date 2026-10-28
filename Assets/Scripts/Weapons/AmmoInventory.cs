using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reserve ammo per ammo type, capped by each type's carry limit.
/// Clips belong to the weapons, as in Half-Life.
/// A type that is not listed under Starting Ammo simply starts at 0 and can still be picked up.
/// </summary>
public class AmmoInventory : MonoBehaviour
{
    [Serializable]
    private struct StartingAmmo
    {
        public AmmoTypeData type;
        public int amount;
    }

    [Tooltip("What the player carries at the start.")]
    [SerializeField] private StartingAmmo[] _startingAmmo;

    private readonly Dictionary<AmmoTypeData, int> _amounts = new Dictionary<AmmoTypeData, int>();

    private void Awake()
    {
        if (_startingAmmo == null)
            return;

        foreach (StartingAmmo entry in _startingAmmo)
        {
            if (entry.type != null)
                _amounts[entry.type] = Mathf.Clamp(entry.amount, 0, entry.type.maxCarry);
        }
    }

    public int Get(AmmoTypeData type)
    {
        if (type == null)
            return 0;

        return _amounts.TryGetValue(type, out int amount) ? amount : 0;
    }

    /// <summary>Removes up to <paramref name="wanted"/> rounds and returns how many were actually taken.</summary>
    public int Take(AmmoTypeData type, int wanted)
    {
        if (type == null || wanted <= 0)
            return 0;

        int have = Get(type);
        int taken = Mathf.Min(wanted, have);

        if (taken > 0)
            _amounts[type] = have - taken;

        return taken;
    }

    /// <summary>Adds rounds up to the type's carry limit and returns how many were actually added.</summary>
    public int Add(AmmoTypeData type, int amount)
    {
        if (type == null || amount <= 0)
            return 0;

        int have = Get(type);
        int added = Mathf.Clamp(type.maxCarry - have, 0, amount);

        if (added > 0)
            _amounts[type] = have + added;

        return added;
    }
}
