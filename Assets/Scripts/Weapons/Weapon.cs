using UnityEngine;

public enum WeaponState
{
    Holstered,
    Drawing,
    Idle,
    Firing,     // cooling down after a shot (or a dry click)
    Reloading,
    Holstering,
}

public enum FireMode
{
    Primary,
    Secondary,
}

/// <summary>
/// Base class for every weapon. Owns the state machine, the clip and the input rules.
/// Subclasses only decide what a shot actually does (<see cref="OnFire"/>) and,
/// optionally, how reloading and per-frame work behave.
/// All timing uses "state ends at" timestamps instead of coroutines, so it is easy to debug.
/// </summary>
public abstract class Weapon : MonoBehaviour, IWeaponStatus
{
    [SerializeField] protected WeaponData _data;
    [Tooltip("Where shots start and which way they point. Use the Main Camera.")]
    [SerializeField] protected Transform _aimOrigin;
    [SerializeField] protected AmmoInventory _ammo;
    [Tooltip("Layers shots can hit. Leave the Player layer out.")]
    [SerializeField] protected LayerMask _hitLayers = ~0;
    [SerializeField] private bool _logEvents = true;

    [Header("Sound")]
    [Tooltip("Plays the shot sounds. Optional: a 2D AudioSource is added to this object if left empty.")]
    [SerializeField] private AudioSource _shotSource;
    [Tooltip("Random pitch change per shot so repeated shots do not sound identical.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float _pitchVariation = 0.04f;

    private bool _primaryHeld;
    private bool _secondaryHeld;
    private bool _primaryWasHeld;
    private bool _secondaryWasHeld;
    private bool _reloadRequested;
    private float _stateEndTime;

    // ---------------------------------------------------------------- Display-ready values (for the HUD)

    public WeaponState State { get; private set; } = WeaponState.Holstered;
    public string DisplayName => _data.displayName;
    public int Slot => _data.slot;
    public WeaponData Data => _data;
    public bool UsesAmmo => _data.ammoType != null;
    private int _ammoInClip;
    public int AmmoInClip
    {
        get => _ammoInClip;
        protected set
        {
            if (_ammoInClip == value) return;
            _ammoInClip = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Raised when the clip or the reserve ammo of this weapon's type changes.</summary>
    public event System.Action Changed;
    public int AmmoReserve => UsesAmmo ? _ammo.Get(_data.ammoType) : 0;
    public bool IsHolstered => State == WeaponState.Holstered;
    public bool IsReloading => State == WeaponState.Reloading;

    // ---------------------------------------------------------------- Commands (called by the controller)

    /// <summary>Call every frame with the current state of the fire buttons.</summary>
    public void SetInput(bool primaryHeld, bool secondaryHeld)
    {
        _primaryHeld = primaryHeld;
        _secondaryHeld = secondaryHeld;
    }

    public void RequestReload() => _reloadRequested = true;

    public void Draw()
    {
        if (State != WeaponState.Holstered)
            return;

        SetState(WeaponState.Drawing, _data.drawTime);
    }

    public void Holster()
    {
        if (State == WeaponState.Holstered || State == WeaponState.Holstering)
            return;

        // Holstering cancels a reload: ammo is only moved when a reload step finishes.
        _reloadRequested = false;
        SetState(WeaponState.Holstering, _data.holsterTime);
    }

    // ---------------------------------------------------------------- Unity messages

    protected virtual void Awake()
    {
        if (_data == null)
        {
            Debug.LogError($"{name}: no WeaponData assigned.", this);
            enabled = false;
            return;
        }

        if (UsesAmmo && _ammo == null)
        {
            Debug.LogError($"{name}: this weapon uses ammo but no AmmoInventory is assigned.", this);
            enabled = false;
            return;
        }

        if (_data.clipSize > 0 && _data.ammoType == null)
            Debug.LogWarning($"{name}: '{_data.displayName}' has a clip size but no Ammo Type asset, so it will " +
                             "behave as if it used no ammo. Assign one in the WeaponData.", this);

        if (_shotSource == null)
        {
            _shotSource = gameObject.AddComponent<AudioSource>();
            _shotSource.playOnAwake = false;
            _shotSource.spatialBlend = 0f;  // 2D: it is the player's own weapon
        }

        if (UsesAmmo)
            _ammo.Changed += OnInventoryChanged;

        AmmoInClip = _data.clipSize;
    }

    private void OnInventoryChanged(AmmoTypeData type)
    {
        if (type == _data.ammoType)
            Changed?.Invoke();
    }

    /// <summary>Subclasses that override this must call base.OnDestroy().</summary>
    protected virtual void OnDestroy()
    {
        if (_ammo != null)
            _ammo.Changed -= OnInventoryChanged;
    }

    private void Update()
    {
        if (State != WeaponState.Holstered)
        {
            Tick();

            if (State == WeaponState.Reloading && ShouldInterruptReload())
                SetState(WeaponState.Idle);

            if (State != WeaponState.Idle && Time.time >= _stateEndTime)
                OnStateTimeUp();

            if (State == WeaponState.Idle)
                HandleIdleInput();
        }

        _primaryWasHeld = _primaryHeld;
        _secondaryWasHeld = _secondaryHeld;
    }

    // ---------------------------------------------------------------- State machine

    private void OnStateTimeUp()
    {
        switch (State)
        {
            case WeaponState.Drawing:
            case WeaponState.Firing:
                SetState(WeaponState.Idle);
                break;

            case WeaponState.Reloading:
                // A weapon can chain several reload steps (the shotgun loads one shell per step).
                if (FinishReload() && CanReload())
                    StartReload();
                else
                    SetState(WeaponState.Idle);
                break;

            case WeaponState.Holstering:
                SetState(WeaponState.Holstered);
                break;
        }
    }

    private void SetState(WeaponState newState, float duration = 0f)
    {
        State = newState;
        _stateEndTime = Time.time + duration;

        if (newState != WeaponState.Idle && newState != WeaponState.Firing)
            Log(newState.ToString());
    }

    private void HandleIdleInput()
    {
        if (_reloadRequested)
        {
            _reloadRequested = false;
            if (CanReload())
            {
                StartReload();
                return;
            }
        }

        if (TryGetRequestedFire(out FireMode mode))
            TryFire(mode);
    }

    /// <summary>Which fire mode (if any) the player is asking for this frame, honouring hold-to-fire.</summary>
    private bool TryGetRequestedFire(out FireMode mode)
    {
        bool primary = _data.holdToFire ? _primaryHeld : _primaryHeld && !_primaryWasHeld;
        bool secondary = _data.holdToFire ? _secondaryHeld : _secondaryHeld && !_secondaryWasHeld;

        if (primary && _data.primary.available)
        {
            mode = FireMode.Primary;
            return true;
        }

        if (secondary && _data.secondary.available)
        {
            mode = FireMode.Secondary;
            return true;
        }

        mode = default;
        return false;
    }

    private bool ShouldInterruptReload()
    {
        return CanInterruptReload
            && TryGetRequestedFire(out FireMode mode)
            && AmmoInClip >= _data.Get(mode).ammoCost;
    }

    private void TryFire(FireMode mode)
    {
        FireSettings settings = _data.Get(mode);

        if (UsesAmmo && AmmoInClip < settings.ammoCost)
        {
            // Like Half-Life: pulling the trigger on an empty clip reloads if there is ammo, clicks otherwise.
            if (CanReload())
            {
                StartReload();
            }
            else
            {
                Log("*click* (out of ammo)");
                SetState(WeaponState.Firing, _data.dryFireInterval);
            }

            return;
        }

        if (UsesAmmo)
            AmmoInClip -= settings.ammoCost;

        SetState(WeaponState.Firing, settings.interval);
        Log($"Fired {mode} ({AmmoInClip}/{_data.clipSize} in clip)");
        OnFire(mode, settings);
        PlayShot(settings.fireSounds);
    }

    // ---------------------------------------------------------------- Reloading (virtual so weapons can change it)

    /// <summary>True if firing may cancel an ongoing reload (the shotgun).</summary>
    protected virtual bool CanInterruptReload => false;

    protected virtual bool CanReload()
    {
        return UsesAmmo && AmmoInClip < _data.clipSize && AmmoReserve > 0;
    }

    protected virtual void StartReload()
    {
        SetState(WeaponState.Reloading, _data.reloadTime);
    }

    /// <summary>
    /// A reload step finished: move ammo into the clip.
    /// Return true to ask for another step (only honoured while <see cref="CanReload"/> is true).
    /// </summary>
    protected virtual bool FinishReload()
    {
        int needed = _data.clipSize - AmmoInClip;
        AmmoInClip += _ammo.Take(_data.ammoType, needed);
        Log($"Reload done: {AmmoInClip} in clip, {AmmoReserve} in reserve");
        return false;
    }

    // ---------------------------------------------------------------- Hooks for subclasses

    /// <summary>The shot has been paid for (ammo, cooldown). Do whatever this weapon's shot does.</summary>
    protected abstract void OnFire(FireMode mode, FireSettings settings);

    /// <summary>Runs every frame while the weapon is out (not holstered). For delayed effects like a melee swing.</summary>
    protected virtual void Tick() { }

    // ---------------------------------------------------------------- Sound

    /// <summary>Plays a random clip from the list as an overlapping one-shot. Safe with null or empty lists.</summary>
    private void PlayShot(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || _shotSource == null)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        _shotSource.pitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
        _shotSource.PlayOneShot(clip);
    }

    protected void Log(string message)
    {
        if (_logEvents)
            Debug.Log($"[{DisplayName}] {message}", this);
    }
}
