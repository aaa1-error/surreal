using UnityEngine;

public class AmmoHUD : MonoBehaviour
{
    [SerializeField] private WeaponController _weapons;
    [SerializeField] private TextMeterView _view;

    private WeaponStatusPresenter _presenter;

    private void Start() => _presenter = new WeaponStatusPresenter(
        _weapons, _view, w => w.AmmoInClip + " | " + w.AmmoReserve);

    private void OnDestroy() => _presenter?.Dispose();
}
