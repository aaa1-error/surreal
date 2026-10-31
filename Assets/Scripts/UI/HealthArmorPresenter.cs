using System;

public class HealthArmorPresenter : IDisposable
{
    private readonly IHealthSource _model;
    private readonly IMeterView _view;

    public HealthArmorPresenter(IHealthSource model, IMeterView view)
    {
        _model = model;
        _view = view;
        _model.Changed += Refresh;
        Refresh();
    }

    // Ceiling so 0.4 HP shows as 1 and you never see "79.99999" or "0" while alive.
    private void Refresh() =>
        _view.SetText($"{(int)Math.Ceiling(_model.Health)} | {(int)Math.Ceiling(_model.Armor)}");

    public void Dispose() => _model.Changed -= Refresh;
}
