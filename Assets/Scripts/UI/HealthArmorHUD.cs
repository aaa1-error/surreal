using UnityEngine;

public class HealthArmorHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth _player;
    [SerializeField] private TextMeterView _view;

    private HealthArmorPresenter _presenter;

    // Start, not OnEnable: PlayerHealth sets its values in Awake, which may not have run yet.
    private void Start() => _presenter = new HealthArmorPresenter(_player, _view);
    private void OnDestroy() => _presenter?.Dispose();
}
