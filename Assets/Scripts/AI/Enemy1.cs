using UnityEngine;
using UnityEngine.AI;

public class EnemyAI1 : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;

    [Header("AI Settings")]
    [Tooltip("The maximum distance at which the enemy will start chasing the player.")]
    public float chaseRange = 10f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Automatically find the player by tag if not assigned in the inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // Calculate distance between enemy and player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If the player is within the reach/chase range, pursue them
        if (distanceToPlayer <= chaseRange)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // Optional: Stop moving or reset path if player is out of range
            if (agent.hasPath)
                agent.ResetPath();
        }
    }

    // Visualizing the chase radius in the Unity Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}