using Unity.Cinemachine;
using UnityEngine;

public class StupidKillTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player == null)
            return;

        DamageInfo d = new DamageInfo
        {
            Amount = 9999f
        };
        
        player.TakeDamage(d);
    }
}
