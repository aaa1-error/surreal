using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple melee enemy: chases the player along the NavMesh and damages him when in range.
/// It only chases when a complete NavMesh path to the player exists ("if they can reach him");
/// otherwise it stands still and keeps re-checking.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Leave empty to auto-find the PlayerHealth in the scene.")]
    [SerializeField] private PlayerHealth target;
    [Tooltip("Only notice the player within this distance. 0 = always knows where he is.")]
    [SerializeField] private float detectionRange = 0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 540f;
    [Tooltip("How often the path to the player is recalculated.")]
    [SerializeField] private float repathInterval = 0.25f;
    [Tooltip("How far from the player's feet a NavMesh point may be and still count as reachable.")]
    [SerializeField] private float navMeshSnapDistance = 2f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackCooldown = 0.8f;
    [Tooltip("Delay between starting the swing and the damage landing.")]
    [SerializeField] private float attackWindup = 0.25f;

    private enum State { Idle, Chasing, Attacking }

    private NavMeshAgent agent;
    private NavMeshPath path;
    private State state = State.Idle;

    private float nextRepathTime;
    private float nextAttackTime;
    private float attackLandTime = -1f;

    public bool HasTarget => target != null && !target.IsDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();

        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.updateRotation = false; // we rotate manually so we can face the target while attacking
    }

    private void Start()
    {
        if (target == null) target = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (!HasTarget)
        {
            SetIdle();
            return;
        }

        Vector3 toTarget = target.transform.position - transform.position;
        float flatDistance = new Vector2(toTarget.x, toTarget.z).magnitude;

        if (detectionRange > 0f && flatDistance > detectionRange && state == State.Idle)
            return;

        // Already swinging: stand still and let the hit land.
        if (attackLandTime > 0f)
        {
            FaceTarget(toTarget);
            if (Time.time >= attackLandTime) LandAttack();
            return;
        }

        if (flatDistance <= attackRange && Mathf.Abs(toTarget.y) < 2f)
        {
            state = State.Attacking;
            agent.ResetPath();
            FaceTarget(toTarget);

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackCooldown;
                attackLandTime = Time.time + attackWindup;
            }
            return;
        }

        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            UpdateChase();
        }

        if (state == State.Chasing && agent.desiredVelocity.sqrMagnitude > 0.01f)
            FaceDirection(agent.desiredVelocity);
    }

    /// <summary>Chase only if the player's position is connected to ours by a full NavMesh path.</summary>
    private void UpdateChase()
    {
        if (!NavMesh.SamplePosition(target.transform.position, out NavMeshHit hit, navMeshSnapDistance, agent.areaMask)
            || !agent.isOnNavMesh
            || !NavMesh.CalculatePath(agent.transform.position, hit.position, agent.areaMask, path)
            || path.status != NavMeshPathStatus.PathComplete)
        {
            SetIdle();
            return;
        }

        state = State.Chasing;
        agent.isStopped = false;
        agent.SetPath(path);
    }

    private void LandAttack()
    {
        attackLandTime = -1f;
        if (!HasTarget) return;

        Vector3 toTarget = target.transform.position - transform.position;
        // The player may have moved away during the windup.
        if (new Vector2(toTarget.x, toTarget.z).magnitude > attackRange * 1.25f) return;

        Vector3 dir = toTarget.normalized;
        Vector3 point = target.transform.position;
        target.TakeDamage(new DamageInfo(attackDamage, point, dir, gameObject));
    }

    private void SetIdle()
    {
        state = State.Idle;
        attackLandTime = -1f;
        if (agent.isOnNavMesh) agent.ResetPath();
    }

    private void FaceTarget(Vector3 toTarget) => FaceDirection(toTarget);

    private void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (detectionRange > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
