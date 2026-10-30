using Unity.Cinemachine;
using UnityEngine;

public class UnlockCursor : MonoBehaviour
{

    private void OnStart()
    {
        Cursor.lockState = CursorLockMode.None;
    }
}
