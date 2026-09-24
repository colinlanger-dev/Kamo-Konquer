using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class UnitMovementScript : NetworkBehaviour
{
    private NavMeshAgent agent;
    private AttackControlerScript attackController;
    private Animator animator;
    private bool isSelected;

    public bool CommandedToMove { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackControlerScript>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (agent != null && !IsServer)
            agent.enabled = false;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public void ClearMoveCommandForAttack()
    {
        if (IsSpawned && !IsServer)
            return;

        CommandedToMove = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
            agent.ResetPath();
    }

    public void OrderMove(Vector3 destination)
    {
        if (!isSelected)
            return;

        if (!IsSpawned)
        {
            ApplyMove(destination);
            return;
        }

        if (IsServer)
            ApplyMove(destination);
        else
            MoveServerRpc(destination);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveServerRpc(Vector3 destination, ServerRpcParams rpcParams = default)
    {
        TeamManagerScript teamMember = TeamManagerScript.FindInParents(transform);
        TeamManagerScript.Team issuingTeam = TeamManagerScript.GetTeamForClient(rpcParams.Receive.SenderClientId);
        if (teamMember == null || teamMember.CurrentTeam.Value != issuingTeam)
            return;

        ApplyMove(destination);
    }

    private void ApplyMove(Vector3 destination)
    {
        if (agent == null || !agent.enabled)
            return;

        if (attackController != null)
            attackController.ClearAttackTarget();

        CommandedToMove = true;
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsWalking", true);
        }

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    private void Update()
    {
        if (!IsServer && IsSpawned)
            return;

        if (CommandedToMove && agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            CommandedToMove = false;
            if (animator != null)
                animator.SetBool("IsWalking", false);
        }
    }
}
