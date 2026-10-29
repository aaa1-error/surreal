using TMPro;
using UnityEngine;

//[RequireComponent(typeof(TMP_Text))]
public class Meter : MonoBehaviour
{   
    protected TMP_Text _uiElement;

    private void Start()
    {
        _uiElement = GetComponent<TMP_Text>();
    }
}
