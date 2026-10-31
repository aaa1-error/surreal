using System;

public class WeaponStatusPresenter : IDisposable
{
    private readonly IWeaponProvider _provider;
    private readonly IMeterView _view;
    private readonly Func<IWeaponStatus, string> _format;
    private IWeaponStatus _current;

    public WeaponStatusPresenter(IWeaponProvider provider, IMeterView view,
                                 Func<IWeaponStatus, string> format)
    {
        _provider = provider;
        _view = view;
        _format = format;
        _provider.WeaponChanged += OnWeaponChanged;
        OnWeaponChanged();
    }

    private void OnWeaponChanged()
    {
        if (_current != null) _current.Changed -= Refresh;
        _current = _provider.CurrentWeapon;
        if (_current != null) _current.Changed += Refresh;
        Refresh();
    }

    private void Refresh() => _view.SetText(_current == null ? "-" : _format(_current));

    public void Dispose()
    {
        _provider.WeaponChanged -= OnWeaponChanged;
        if (_current != null) _current.Changed -= Refresh;
    }
}
