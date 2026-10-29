using TMPro;
using UnityEngine;

//[RequireComponent(typeof(TextMeshPro))]
public class AmmoDisplay : Meter
{   
    [SerializeField]
    private WeaponController _w;

    private void Update()
    {
        //float rounded = Mathf.Round(_player.HorizontalSpeed * 100.0f) / 100.0f; 
        //_uiElement.SetText(rounded.ToString());
        string formatted = $"{_w.CurrentWeapon.AmmoInClip} | {_w.CurrentWeapon.AmmoReserve}";
        _uiElement.SetText(formatted);
    }
}
