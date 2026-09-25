using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Constructable : NetworkBehaviour
{
    [FormerlySerializedAs("constMaxHealth")]
    [SerializeField] private float maxLeben = 100f;
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteBeschaedigt;
    [SerializeField] private TeamManagerScript.Team owningTeam = TeamManagerScript.Team.None;
    [SerializeField, Min(0)] private int faithIncomePerSecond = 1;

    public readonly NetworkVariable<float> NetworkHealth = new(100f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private SpriteRenderer sr;
    private NavMeshObstacle obstacle;
    private Spawner spawner;

    public TeamManagerScript.Team OwningTeam
    {
        get
        {
            TeamManagerScript team = TeamManagerScript.FindInParents(transform);
            return team != null && team.IsSpawned ? team.CurrentTeam.Value : owningTeam;
        }
    }

    public int FaithIncomePerSecond => faithIncomePerSecond;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        spawner = GetComponent<Spawner>();
        obstacle = GetComponentInChildren<NavMeshObstacle>();
        UpdateSprite(maxLeben);
    }

    public override void OnNetworkSpawn()
    {
        NetworkHealth.OnValueChanged += OnHealthChanged;
        if (IsServer)
            NetworkHealth.Value = maxLeben;

        TeamManagerScript teamManager = TeamManagerScript.FindInParents(transform);
        if (teamManager != null && teamManager.IsSpawned)
            owningTeam = teamManager.CurrentTeam.Value;

        ConstructableWasPlaced();
        UpdateSprite(NetworkHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        NetworkHealth.OnValueChanged -= OnHealthChanged;
        if (IsServer && NetworkObject != null)
            ResourceManager.Instance?.NotifyBuildingDestroyed(NetworkObject.NetworkObjectId);
    }

    public void ReceiveDamage(float damage)
    {
        if (damage <= 0f || (IsSpawned && !IsServer))
            return;

        if (IsSpawned)
        {
            NetworkHealth.Value = Mathf.Max(0f, NetworkHealth.Value - damage);
            if (NetworkHealth.Value > 0f)
                return;

            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn(true);
            return;
        }

        maxLeben = Mathf.Max(0f, maxLeben - damage);
        UpdateSprite(maxLeben);
        if (maxLeben <= 0f)
            Destroy(gameObject);
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        UpdateSprite(newValue);
    }

    private void UpdateSprite(float health)
    {
        if (sr != null)
            sr.sprite = health / Mathf.Max(1f, maxLeben) < 0.5f ? spriteBeschaedigt : spriteNormal;
    }

    public void ConstructableWasPlaced()
    {
        if (obstacle == null)
            obstacle = GetComponentInChildren<NavMeshObstacle>();
        if (obstacle != null)
            obstacle.enabled = true;

        if (spawner == null)
            spawner = GetComponent<Spawner>();
        if (spawner != null && IsServer)
            spawner.InitializeForBuilding();
    }

    public void PrepareForSpawn(TeamManagerScript.Team team)
    {
        owningTeam = team;
        gameObject.tag = "Building";
        TeamManagerScript.ApplyTeamLayer(gameObject, team);

        TeamManagerScript teamManager = GetComponent<TeamManagerScript>();
        if (teamManager != null)
            teamManager.SetStartingTeam(team);
    }

    public void SetOwningTeam(TeamManagerScript.Team team)
    {
        owningTeam = team;
        gameObject.tag = "Building";
        TeamManagerScript.ApplyTeamLayer(gameObject, team);
    }
}
