using UnityEngine;

/// <summary>One instant-hit ray with random spread. Shared by every bullet weapon.</summary>
public static class HitscanShot
{
    private const float DebugLineSeconds = 1f;

    public static bool Fire(
        Transform aim,
        float spreadDegrees,
        float range,
        LayerMask hitLayers,
        float damage,
        GameObject source,
        out RaycastHit hit)
    {
        Vector3 origin = aim.position;
        Vector3 direction = RandomInCone(aim.forward, spreadDegrees);

        bool didHit = Physics.Raycast(origin, direction, out hit, range, hitLayers, QueryTriggerInteraction.Ignore);

        // No models yet, so every shot is a debug line in the Scene view (and Game view with Gizmos on).
        Vector3 end = didHit ? hit.point : origin + direction * range;
        Debug.DrawLine(origin, end, didHit ? Color.red : Color.yellow, DebugLineSeconds);

        if (didHit)
        {
            ImpactDecals.Spawn(hit.point, hit.normal);
            
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null)
                target.TakeDamage(new DamageInfo(damage, hit.point, direction, source));
        }

        return didHit;
    }

    /// <summary>Uniformly random direction inside a cone of the given half-angle around <paramref name="forward"/>.</summary>
    private static Vector3 RandomInCone(Vector3 forward, float halfAngleDegrees)
    {
        if (halfAngleDegrees <= 0f)
            return forward;

        Vector2 disc = Random.insideUnitCircle * Mathf.Tan(halfAngleDegrees * Mathf.Deg2Rad);
        Vector3 local = new Vector3(disc.x, disc.y, 1f);
        return (Quaternion.LookRotation(forward) * local).normalized;
    }
}
