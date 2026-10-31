using UnityEngine;

public class WeaponNameHUD : MonoBehaviour
{
    [SerializeField] private WeaponController _weapons;
    [SerializeField] private TextMeterView _view;

    private WeaponStatusPresenter _presenter;

    private void Start() => _presenter = new WeaponStatusPresenter(
        _weapons, _view, w => w.DisplayName);

    private void OnDestroy() => _presenter?.Dispose();
}
