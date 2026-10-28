using UnityEngine;

/// <summary>
/// Base for anything the player collects by walking into it. The collider is forced to be a trigger.
/// Subclasses decide what is handed over; the pickup is only used up if something was actually taken,
/// so a pickup stays put when the player is already full.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Pickup : MonoBehaviour
{
    [Tooltip("Seconds until the pickup comes back. 0 = gone for good.")]
    [SerializeField] private float _respawnSeconds = 0f;
    [Tooltip("Degrees per second the pickup spins around the vertical axis, so it is easy to spot. 0 = still.")]
    [SerializeField] private float _spinSpeed = 90f;
    [SerializeField] private bool _logEvents = true;

    private Collider _collider;
    private Renderer[] _renderers;
    private bool _taken;

    /// <summary>Try to hand the player something. Return true only if something was actually taken.</summary>
    protected abstract bool TryGive(WeaponController controller);

    protected virtual void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        _renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        if (!_taken && _spinSpeed != 0f)
            transform.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    // OnTriggerStay (not Enter) so a player standing on a pickup collects it once they have room.
    private void OnTriggerStay(Collider other)
    {
        if (_taken)
            return;

        WeaponController controller = other.GetComponentInParent<WeaponController>();
        if (controller == null)
            return;

        if (TryGive(controller))
            Take();
    }

    private void Take()
    {
        _taken = true;

        if (_logEvents)
            Debug.Log($"Picked up {name}", this);

        if (_respawnSeconds > 0f)
        {
            SetVisible(false);
            Invoke(nameof(Respawn), _respawnSeconds);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Respawn()
    {
        _taken = false;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer meshRenderer in _renderers)
            meshRenderer.enabled = visible;

        _collider.enabled = visible;
    }
}
