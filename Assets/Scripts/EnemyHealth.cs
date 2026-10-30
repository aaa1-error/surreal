using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Makes an enemy killable. Implements IDamageable, so anything that already damages
/// IDamageable targets (weapons, explosions, traps) works on it with no extra code.
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    public enum VanishMode
    {
        /// <summary>SetActive(false). Cheap, and the enemy can be re-enabled later.</summary>
        Disable,
        /// <summary>Destroy(gameObject).</summary>
        Destroy
    }

    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;
    [Tooltip("Multiplier applied to incoming damage (e.g. 0.5 for armored enemies).")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("On Death")]
    [Tooltip("Components switched off when the enemy dies. Leave empty to auto-disable EnemyAI.")]
    [SerializeField] private Behaviour[] disableOnDeath;
    [SerializeField] private bool disableColliders = true;
    [Tooltip("Optional prefab spawned at the enemy's position on death (gibs, particles...).")]
    [SerializeField] private GameObject deathEffect;

    [Header("Vanish")]
    [SerializeField] private VanishMode vanishMode = VanishMode.Disable;
    [Tooltip("Seconds before the enemy vanishes. 0 = immediately. Negative = never (corpse stays).")]
    [FormerlySerializedAs("destroyDelay")]
    [SerializeField] private float vanishDelay = 0f;

    [Header("Events")]
    public UnityEvent<DamageInfo> OnDamaged;
    public UnityEvent<DamageInfo> OnDeath;

    public float Health { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private Behaviour[] behavioursToDisable;
    private Collider[] colliders;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        colliders = GetComponentsInChildren<Collider>(true);

        if (disableOnDeath != null && disableOnDeath.Length > 0)
            behavioursToDisable = disableOnDeath;
        else if (TryGetComponent(out EnemyAI ai))
            behavioursToDisable = new Behaviour[] { ai };
        else
            behavioursToDisable = new Behaviour[0];
    }

    // Runs again every time the object is re-enabled, so a disabled enemy comes back fresh.
    private void OnEnable()
    {
        Health = maxHealth;
        IsDead = false;

        foreach (Behaviour b in behavioursToDisable) if (b != null) b.enabled = true;
        foreach (Collider c in colliders) if (c != null) c.enabled = true;
        if (agent != null) agent.enabled = true;
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    public void TakeDamage(DamageInfo info)
    {
        if (IsDead || info.Amount <= 0f) return;

        Health -= info.Amount * damageMultiplier;
        OnDamaged?.Invoke(info);

        if (Health <= 0f) Die(info);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Health = Mathf.Min(maxHealth, Health + amount);
    }

    private void Die(DamageInfo info)
    {
        IsDead = true;
        Health = 0f;

        // Stop the AI and the agent so the corpse doesn't keep chasing or attacking.
        foreach (Behaviour b in behavioursToDisable) if (b != null) b.enabled = false;

        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
        }

        if (disableColliders)
            foreach (Collider c in colliders) if (c != null) c.enabled = false;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, transform.rotation);

        OnDeath?.Invoke(info);

        if (vanishDelay < 0f) return;
        if (vanishDelay == 0f) Vanish();
        else Invoke(nameof(Vanish), vanishDelay);
    }

    private void Vanish()
    {
        if (vanishMode == VanishMode.Destroy) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
