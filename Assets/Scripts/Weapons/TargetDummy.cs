using UnityEngine;

/// <summary>Test target. Logs every hit and refills its health a moment after dying.</summary>
public class TargetDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _resetDelay = 1.5f;

    public float Health { get; private set; }

    private void Awake()
    {
        Health = _maxHealth;
    }

    public void TakeDamage(DamageInfo info)
    {
        if (Health <= 0f)
            return;

        Health -= info.Amount;
        Debug.Log($"{name} took {info.Amount} damage ({Mathf.Max(Health, 0f)}/{_maxHealth})", this);

        if (Health <= 0f)
        {
            Debug.Log($"{name} died", this);
            //Invoke(nameof(ResetHealth), _resetDelay);
            this.gameObject.SetActive(false);
        }
    }

    private void ResetHealth()
    {
        Health = _maxHealth;
    }
}
