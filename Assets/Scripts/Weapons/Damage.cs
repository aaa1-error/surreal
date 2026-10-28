using UnityEngine;

/// <summary>Everything a target needs to know about one hit.</summary>
public struct DamageInfo
{
    public float Amount;
    public Vector3 Point;
    public Vector3 Direction;
    public GameObject Source;

    public DamageInfo(float amount, Vector3 point, Vector3 direction, GameObject source)
    {
        Amount = amount;
        Point = point;
        Direction = direction;
        Source = source;
    }
}

/// <summary>Implement on anything that can take damage (enemies, props, the player).</summary>
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}
