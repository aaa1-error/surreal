using UnityEngine;

/// <summary>
/// Close-range swing (the crowbar). Pressing fire starts the swing; the hit lands a moment later,
/// in whatever direction you are looking at that moment. Holstering mid-swing cancels the hit.
/// Use a WeaponData with Ammo Type = None, Clip Size = 0 and the secondary fire disabled.
/// </summary>
public class MeleeWeapon : Weapon
{
    private const float DebugLineSeconds = 1f;

    [Tooltip("Seconds between pressing the button and the hit landing.")]
    [SerializeField] private float _swingDelay = 0.2f;
    [Tooltip("Radius of the swing. A fat cast is more forgiving than a thin ray.")]
    [SerializeField] private float _hitRadius = 0.25f;

    private bool _hitPending;
    private float _hitTime;
    private float _pendingDamage;

    protected override void OnFire(FireMode mode, FireSettings settings)
    {
        _hitPending = true;
        _hitTime = Time.time + _swingDelay;
        _pendingDamage = settings.damage;
    }

    protected override void Tick()
    {
        if (!_hitPending)
            return;

        if (State == WeaponState.Holstering || IsHolstered)
        {
            _hitPending = false;
            return;
        }

        if (Time.time >= _hitTime)
        {
            _hitPending = false;
            Strike();
        }
    }

    private void Strike()
    {
        Vector3 origin = _aimOrigin.position;
        Vector3 direction = _aimOrigin.forward;

        bool didHit = Physics.SphereCast(
            origin, _hitRadius, direction, out RaycastHit hit, _data.range,
            _hitLayers, QueryTriggerInteraction.Ignore);

        Vector3 end = didHit ? origin + direction * hit.distance : origin + direction * _data.range;
        Debug.DrawLine(origin, end, didHit ? Color.red : Color.yellow, DebugLineSeconds);

        if (!didHit)
        {
            Log("Swing missed");
            return;
        }

        // A cast that starts inside a collider reports no useful hit point, so fall back to the origin.
        Vector3 point = hit.distance > 0f ? hit.point : origin;
        Log($"Hit {hit.collider.name}");

        // No valid surface point when the swing started inside the collider, so no decal then.
        if (hit.distance > 0f)
            ImpactDecals.Spawn(hit.point, hit.normal);

        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        if (target != null)
            target.TakeDamage(new DamageInfo(_pendingDamage, point, direction, transform.root.gameObject));
    }
}
