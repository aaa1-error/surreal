using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// TEMPORARY. Drives one weapon from input so it can be tested now.
/// Replaced by WeaponController (inventory, switching) in a later step.
/// The three actions must exist in your Input Actions asset and be enabled.
/// </summary>
public class WeaponTester : MonoBehaviour
{
    [SerializeField] private Weapon _weapon;
    [SerializeField] private InputActionReference _primary;
    [SerializeField] private InputActionReference _secondary;
    [SerializeField] private InputActionReference _reload;

    private void OnEnable()
    {
        // Harmless if the actions are already enabled by something else (e.g. PlayerInput).
        _primary.action.Enable();
        _secondary.action.Enable();
        _reload.action.Enable();
    }

    private void Start()
    {
        _weapon.Draw();
    }

    private void Update()
    {
        _weapon.SetInput(_primary.action.IsPressed(), _secondary.action.IsPressed());

        if (_reload.action.WasPressedThisFrame())
            _weapon.RequestReload();
    }
}
