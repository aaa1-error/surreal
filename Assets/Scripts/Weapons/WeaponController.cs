using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sits on the player. Reads input, forwards it to the current weapon and handles switching:
/// the current weapon holsters first, then the new one draws. Input is ignored in between.
/// Also tracks which weapons the player owns, so weapons can be picked up during play.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapons")]
    [Tooltip("Weapons the player starts with. The first one is drawn at the start.")]
    [SerializeField] private Weapon[] _weapons;
    [Tooltip("Weapon objects that exist on the player but are not owned until a WeaponPickup gives them.")]
    [SerializeField] private Weapon[] _lockedWeapons;
    [Tooltip("Where picked-up ammo goes. Found on this object if left empty.")]
    [SerializeField] private AmmoInventory _ammo;
    [Tooltip("Draw a weapon the first time it is picked up.")]
    [SerializeField] private bool _switchToNewWeapons = true;

    [Header("Input")]
    [SerializeField] private InputActionReference _primary;
    [SerializeField] private InputActionReference _secondary;
    [SerializeField] private InputActionReference _reload;
    [Tooltip("Optional. Cycle to the next weapon (e.g. scroll up).")]
    [SerializeField] private InputActionReference _next;
    [Tooltip("Optional. Cycle to the previous weapon (e.g. scroll down).")]
    [SerializeField] private InputActionReference _previous;
    [Tooltip("Optional. Element 0 selects slot 1, element 1 selects slot 2, and so on.")]
    [SerializeField] private InputActionReference[] _slotActions;

    private readonly List<Weapon> _owned = new List<Weapon>();   // kept sorted by slot
    private readonly List<Weapon> _locked = new List<Weapon>();
    private Weapon _current;  // weapon that is out (or being holstered)
    private Weapon _pending;  // weapon we are switching to

    public Weapon CurrentWeapon => _current;
    public bool IsSwitching => _pending != null;
    public IReadOnlyList<Weapon> OwnedWeapons => _owned;

    // ---------------------------------------------------------------- Unity messages

    private void Awake()
    {
        if (_ammo == null)
            _ammo = GetComponent<AmmoInventory>();

        if (_weapons != null)
        {
            foreach (Weapon weapon in _weapons)
            {
                if (weapon != null && !_owned.Contains(weapon))
                    InsertSorted(weapon);
            }
        }

        if (_lockedWeapons != null)
        {
            foreach (Weapon weapon in _lockedWeapons)
            {
                if (weapon != null && !_owned.Contains(weapon) && !_locked.Contains(weapon))
                    _locked.Add(weapon);
            }
        }
    }

    private void OnEnable()
    {
        // Harmless if something else (e.g. PlayerInput) already enabled them.
        EnableAction(_primary);
        EnableAction(_secondary);
        EnableAction(_reload);
        EnableAction(_next);
        EnableAction(_previous);

        if (_slotActions != null)
        {
            foreach (InputActionReference slotAction in _slotActions)
                EnableAction(slotAction);
        }
    }

    private void Start()
    {
        if (_weapons != null && _weapons.Length > 0 && _weapons[0] != null)
            Equip(_weapons[0]);
    }

    private void Update()
    {
        HandleSwitchInput();
        FinishPendingSwitch();
        ForwardWeaponInput();
    }

    // ---------------------------------------------------------------- Pickups

    /// <summary>
    /// Gives the player the weapon described by <paramref name="data"/> plus some reserve ammo.
    /// If the player already has it, only the ammo is taken. Returns false if nothing was taken
    /// (so the pickup stays in the world).
    /// </summary>
    public bool TryPickupWeapon(WeaponData data, int ammoAmount)
    {
        if (FindByData(_owned, data) != null)
            return TryAddAmmo(data.ammoType, ammoAmount);

        Weapon weapon = FindByData(_locked, data);
        if (weapon == null)
        {
            Debug.LogWarning($"WeaponController: no weapon object for '{data.displayName}' is set up on the player. " +
                             "Add it to the Locked Weapons list.", this);
            return false;
        }

        _locked.Remove(weapon);
        InsertSorted(weapon);
        TryAddAmmo(data.ammoType, ammoAmount);  // bonus ammo; fine if nothing could be added

        if (_switchToNewWeapons || _current == null)
            Select(weapon);

        return true;
    }

    /// <summary>Adds reserve ammo. Returns false if the player was already full of that type.</summary>
    public bool TryAddAmmo(AmmoType type, int amount)
    {
        if (type == AmmoType.None || _ammo == null)
            return false;

        return _ammo.Add(type, amount) > 0;
    }

    // ---------------------------------------------------------------- Switching

    public void Select(Weapon weapon)
    {
        if (weapon == null || !_owned.Contains(weapon))
            return;

        if (_current == null)
        {
            Equip(weapon);
            return;
        }

        if (weapon == _current && _pending == null)
            return;  // already out and not switching

        if (weapon == _pending)
            return;  // already on its way

        _pending = weapon;
        _current.Holster();  // no-op if it is already holstering
    }

    private void Equip(Weapon weapon)
    {
        _current = weapon;
        _pending = null;
        weapon.Draw();
    }

    private void FinishPendingSwitch()
    {
        if (_pending != null && _current.IsHolstered)
            Equip(_pending);
    }

    private void Cycle(int direction)
    {
        Weapon from = _pending != null ? _pending : _current;
        if (from == null)
            return;

        int count = _owned.Count;
        int index = _owned.IndexOf(from);
        Select(_owned[(index + direction + count) % count]);
    }

    /// <summary>Selects the first owned weapon in the slot; pressing again moves on to the next one in that slot.</summary>
    private void SelectSlot(int slot)
    {
        Weapon reference = _pending != null ? _pending : _current;
        Weapon first = null;
        Weapon afterReference = null;
        bool seenReference = false;

        foreach (Weapon weapon in _owned)
        {
            if (weapon.Slot != slot)
                continue;

            if (first == null)
                first = weapon;

            if (seenReference && afterReference == null)
                afterReference = weapon;

            if (weapon == reference)
                seenReference = true;
        }

        Select(afterReference != null ? afterReference : first);
    }

    private void InsertSorted(Weapon weapon)
    {
        int index = 0;
        while (index < _owned.Count && _owned[index].Slot <= weapon.Slot)
            index++;

        _owned.Insert(index, weapon);
    }

    private static Weapon FindByData(List<Weapon> list, WeaponData data)
    {
        foreach (Weapon weapon in list)
        {
            if (weapon.Data == data)
                return weapon;
        }

        return null;
    }

    // ---------------------------------------------------------------- Input

    private void HandleSwitchInput()
    {
        if (_slotActions != null)
        {
            for (int i = 0; i < _slotActions.Length; i++)
            {
                if (_slotActions[i] != null && _slotActions[i].action.WasPressedThisFrame())
                    SelectSlot(i + 1);
            }
        }

        if (_next != null && _next.action.WasPressedThisFrame())
            Cycle(1);

        if (_previous != null && _previous.action.WasPressedThisFrame())
            Cycle(-1);
    }

    private void ForwardWeaponInput()
    {
        if (_current == null)
            return;

        // While switching, the outgoing weapon gets no input.
        if (IsSwitching)
        {
            _current.SetInput(false, false);
            return;
        }

        _current.SetInput(_primary.action.IsPressed(), _secondary.action.IsPressed());

        if (_reload.action.WasPressedThisFrame())
            _current.RequestReload();
    }

    private static void EnableAction(InputActionReference reference)
    {
        if (reference != null)
            reference.action.Enable();
    }
}
