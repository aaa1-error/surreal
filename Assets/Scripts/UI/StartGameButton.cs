using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

//[RequireComponent(typeof(TMP_Text))]
public class StartGameButton : MonoBehaviour
{   

    public void HandleClick()
    {
        SceneManager.LoadScene(1);
    }
}
