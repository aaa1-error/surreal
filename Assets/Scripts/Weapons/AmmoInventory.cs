using System;
using UnityEngine;

public enum AmmoType
{
    None,
    Pistol9mm,
    Buckshot,
}

/// <summary>Reserve ammo per type with a carry limit. Clips belong to the weapons, as in Half-Life.</summary>
public class AmmoInventory : MonoBehaviour
{
    [Serializable]
    private struct Slot
    {
        public AmmoType type;
        public int amount;
        public int max;
    }

    [SerializeField] private Slot[] _slots =
    {
        new Slot { type = AmmoType.Pistol9mm, amount = 51, max = 250 },
        new Slot { type = AmmoType.Buckshot, amount = 0, max = 125 },
    };

    public int Get(AmmoType type)
    {
        int index = IndexOf(type);
        return index >= 0 ? _slots[index].amount : 0;
    }

    /// <summary>Removes up to <paramref name="wanted"/> rounds and returns how many were actually taken.</summary>
    public int Take(AmmoType type, int wanted)
    {
        int index = IndexOf(type);
        if (index < 0 || wanted <= 0)
            return 0;

        int taken = Mathf.Min(wanted, _slots[index].amount);
        _slots[index].amount -= taken;
        return taken;
    }

    /// <summary>Adds rounds up to the carry limit and returns how many were actually added.</summary>
    public int Add(AmmoType type, int amount)
    {
        int index = IndexOf(type);
        if (index < 0 || amount <= 0)
            return 0;

        int added = Mathf.Min(amount, _slots[index].max - _slots[index].amount);
        _slots[index].amount += Mathf.Max(added, 0);
        return Mathf.Max(added, 0);
    }

    private int IndexOf(AmmoType type)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].type == type)
                return i;
        }

        return -1;
    }
}
