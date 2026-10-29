using UnityEngine;

//[RequireComponent(typeof(TextMeshPro))]
public class Speedometer : Meter
{   
    [SerializeField]
    private QuakeMovement _player;


    private void Update()
    {
        float rounded = Mathf.Round(_player.HorizontalSpeed * 100.0f); 
        _uiElement.SetText(rounded.ToString());
    }
}
