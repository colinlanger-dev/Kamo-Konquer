using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class AttackControlerScript : NetworkBehaviour
{
    public float attackRange = 12f;
    public Transform targetToAttack;

    public float health = 10f;
    public float unitDamage = 2f;
    public float attackSpeed = 20f;

    public readonly NetworkVariable<float> NetworkHealth = new(
        10f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float nextAttackTime;
    private NavMeshAgent agent;
    private Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkHealth.Value = health;
        NetworkHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        NetworkHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        health = newValue;
    }

    private void Update()
    {
        if (IsSpawned && !IsServer)
            return;
        if (targetToAttack == null)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetBool("IsAttacking", false);
            return;
        }

        UnitMovementScript movement = GetComponentInParent<UnitMovementScript>();
        if (movement != null && movement.CommandedToMove)
            return;

        float distance = Vector3.Distance(transform.position, targetToAttack.position);
        if (distance > attackRange)
        {
            if (agent != null && agent.enabled)
                agent.SetDestination(targetToAttack.position);
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetBool("IsWalking", true);
            return;
        }

        if (agent != null && agent.enabled && agent.hasPath)
            agent.ResetPath();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsAttacking", true);
        }

        if (Time.time < nextAttackTime)
            return;

        AttackControlerScript targetController = targetToAttack.GetComponentInParent<AttackControlerScript>();
        if (targetController != null)
            targetController.ReceiveDamage(unitDamage);
        nextAttackTime = Time.time + attackSpeed;
    }

    public void OrderAttack(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        NetworkObject target = targetTransform.GetComponentInParent<NetworkObject>();
        if (!IsSpawned)
        {
            targetToAttack = targetTransform;
            return;
        }

        if (target == null)
            return;

        if (IsServer)
            SetTargetOnServer(target);
        else
            AttackServerRpc(target.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void AttackServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        TeamManagerScript attackerTeam = TeamManagerScript.FindInParents(transform);
        TeamManagerScript.Team issuingTeam = TeamManagerScript.GetTeamForClient(rpcParams.Receive.SenderClientId);
        if (attackerTeam == null || issuingTeam == TeamManagerScript.Team.None ||
            attackerTeam.CurrentTeam.Value != issuingTeam)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject target))
            return;

        SetTargetOnServer(target);
    }

    private void SetTargetOnServer(NetworkObject target)
    {
        TeamManagerScript attackerTeam = TeamManagerScript.FindInParents(transform);
        TeamManagerScript targetTeam = TeamManagerScript.FindInParents(target.transform);

        if (attackerTeam != null && targetTeam != null &&
            attackerTeam.CurrentTeam.Value != TeamManagerScript.Team.None &&
            attackerTeam.CurrentTeam.Value == targetTeam.CurrentTeam.Value)
            return;

        if (targetTeam == null && !IsTaggedAttackTarget(target.transform))
            return;

        UnitMovementScript movement = GetComponentInParent<UnitMovementScript>();
        if (movement != null)
            movement.ClearMoveCommandForAttack();

        targetToAttack = target.transform;
    }

    public void ClearAttackTarget()
    {
        if (!IsSpawned || IsServer)
            targetToAttack = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsSpawned && !IsServer)
            return;
        if (targetToAttack != null)
            return;

        TeamManagerScript ownTeam = TeamManagerScript.FindInParents(transform);
        TeamManagerScript otherTeam = TeamManagerScript.FindInParents(other.transform);
        if (ownTeam != null && otherTeam != null &&
            ownTeam.CurrentTeam.Value != TeamManagerScript.Team.None &&
            otherTeam.CurrentTeam.Value != TeamManagerScript.Team.None)
        {
            if (ownTeam.CurrentTeam.Value == otherTeam.CurrentTeam.Value)
                return;

            targetToAttack = otherTeam.transform;
            return;
        }

        if (IsTaggedAttackTarget(other.transform))
        {
            AttackControlerScript otherController = other.GetComponentInParent<AttackControlerScript>();
            if (otherController != null)
                targetToAttack = otherController.transform;
        }
    }

    private static bool IsTaggedAttackTarget(Transform target)
    {
        return target != null && (target.CompareTag("Unit") || target.CompareTag("Building") ||
               target.root.CompareTag("Unit") || target.root.CompareTag("Building"));
    }

    public void ReceiveDamage(float damage)
    {
        if (!IsSpawned)
        {
            health -= damage;
            Debug.Log($"{name} health: {health}");
            return;
        }

        if (!IsServer)
            return;

        NetworkHealth.Value = Mathf.Max(0, NetworkHealth.Value - damage);
        health = NetworkHealth.Value;
        Debug.Log($"{name} health: {health}");
        if (health <= 0f && NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
    }
}
