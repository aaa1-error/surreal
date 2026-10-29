using TMPro;
using UnityEngine;

//[RequireComponent(typeof(TextMeshPro))]
public class WeaponDisplay : Meter
{   
    [SerializeField]
    private WeaponController _weaponController;

    private void Update()
    {   
        _uiElement.SetText(_weaponController.CurrentWeapon.DisplayName);
    }
}
