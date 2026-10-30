using Unity.Cinemachine;
using UnityEngine;

public class UnlockCursor : MonoBehaviour
{

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
    }
}
