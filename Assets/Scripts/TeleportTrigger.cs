using Unity.Cinemachine;
using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [SerializeField] private GameObject destination;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        QuakeMovement player = other.GetComponent<QuakeMovement>();
        if (player == null)
            return;

        other.transform.position = destination.transform.position;

        player.CancelVelocity();
    }
}
