using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextMeterView : MonoBehaviour, IMeterView
{
    private TMP_Text _text;

    public void SetText(string text)
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        _text.SetText(text);
    }
}
