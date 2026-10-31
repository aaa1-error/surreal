using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Health + suit armor for the player. Half-Life style armor: armor soaks a share of
/// each hit and is consumed doing so; whatever it can't cover falls through to health.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable, IHealthSource
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float startingHealth = 100f;

    [Header("Armor")]
    [SerializeField] private float maxArmor = 100f;
    [SerializeField] private float startingArmor = 0f;

    [Tooltip("Fraction of incoming damage that still hits health while armor is up (HL: 0.2).")]
    [SerializeField, Range(0f, 1f)] private float armorPassThrough = 0.2f;

    [Tooltip("Armor points consumed per point of damage it absorbs (HL: 0.5).")]
    [SerializeField, Range(0f, 2f)] private float armorCostPerDamage = 0.5f;

    [Header("Rules")]
    [SerializeField] private bool invulnerable;
    [Tooltip("Ignore damage for this long after taking a hit (0 = off).")]
    [SerializeField] private float invulnerabilityTime = 0f;

    [Header("Events")]
    public UnityEvent<DamageInfo> OnDamaged;
    public UnityEvent OnDeath;

    /// <summary>Raised whenever Health or Armor changes. Call it from any future healing/armor pickup too.</summary>
    public event Action Changed;

    public float Health { get; private set; }
    public float Armor { get; private set; }
    public float MaxHealth => maxHealth;
    public float MaxArmor => maxArmor;
    public bool IsDead { get; private set; }
    public bool Invulnerable { get => invulnerable; set => invulnerable = value; }

    private float lastHitTime = float.NegativeInfinity;

    private void Awake()
    {
        Health = Mathf.Clamp(startingHealth, 1f, maxHealth);
        Armor = Mathf.Clamp(startingArmor, 0f, maxArmor);
    }

    public void TakeDamage(DamageInfo info)
    {
        if (IsDead || invulnerable || info.Amount <= 0f) return;
        if (invulnerabilityTime > 0f && Time.time - lastHitTime < invulnerabilityTime) return;
        lastHitTime = Time.time;

        float healthDamage = info.Amount;

        if (Armor > 0f)
        {
            float toHealth = info.Amount * armorPassThrough;
            float armorLoss = (info.Amount - toHealth) * armorCostPerDamage;

            if (armorLoss > Armor)
            {
                float uncovered = armorLoss - Armor;
                toHealth += armorCostPerDamage > 0f ? uncovered / armorCostPerDamage : 0f;
                armorLoss = Armor;
            }

            Armor -= armorLoss;
            healthDamage = toHealth;
        }

        Health = Mathf.Max(0f, Health - healthDamage);
        Changed?.Invoke();
        OnDamaged?.Invoke(info);

        if (Health <= 0f) Die();
    }

    private void Die()
    {
        // IsDead = true;
        // OnDeath?.Invoke();
        SceneManager.LoadScene(0);
    }
}
