using UnityEngine;

public class Speedometer : MonoBehaviour
{
    [SerializeField] private QuakeMovement _player;
    [SerializeField] private TextMeterView _view;

    private void Update() => _view.SetText(Mathf.Round(_player.HorizontalSpeed * 100f).ToString());
}
