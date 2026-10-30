using UnityEngine;

//[RequireComponent(typeof(TextMeshPro))]
public class HealthArmorDisplay : Meter
{   
    [SerializeField]
    private PlayerHealth _player;


    private void Update()
    {
        //float rounded = Mathf.Round(_player.HorizontalSpeed * 100.0f); 
    
        _uiElement.SetText($"{_player.Health} | {_player.Armor}");
    }
}
