using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

public class Spawner : NetworkBehaviour
{
    public enum UnitType
    {
        Tank,
        RPG,
        Minigun
    }

    [Header("Azad unit prefabs")]
    [SerializeField] private GameObject azadTankPrefab;
    [SerializeField] private GameObject azadRpgPrefab;
    [SerializeField] private GameObject azadMinigunPrefab;

    [Header("Kamo unit prefabs")]
    [SerializeField] private GameObject kamoTankPrefab;
    [SerializeField] private GameObject kamoRpgPrefab;
    [SerializeField] private GameObject kamoMinigunPrefab;

    [Header("Production")]
    [SerializeField, Min(0)] private int tankCost = 100;
    [SerializeField, Min(0)] private int rpgCost = 100;
    [SerializeField, Min(0)] private int minigunCost = 100;
    [SerializeField, Min(0.1f)] private float productionSeconds = 10f;
    [SerializeField] private Vector3 spawnOffset = new(0f, 0f, -5f);
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 3f;
    [SerializeField, Min(0.1f)] private float navMeshFallbackRadius = 15f;

    public readonly NetworkVariable<int> QueueCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> CurrentUnitType = new(-1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> SecondsRemaining = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly Queue<UnitType> productionQueue = new();
    private bool initialized;
    private bool cancelled;
    private float timer;

    public bool CanProduce => IsSpawned && !cancelled && !IsDestroyed && ResourceManager.Instance != null &&
                              !ResourceManager.Instance.IsMatchOver;
    public bool IsDestroyed { get; private set; }

    public override void OnNetworkSpawn()
    {
        QueueCount.OnValueChanged += OnQueueChanged;
        CurrentUnitType.OnValueChanged += OnQueueChanged;
        SecondsRemaining.OnValueChanged += OnSecondsChanged;

        if (IsServer)
        {
            QueueCount.Value = 0;
            CurrentUnitType.Value = -1;
            SecondsRemaining.Value = 0f;
            InitializeForBuilding();
        }
    }

    public override void OnNetworkDespawn()
    {
        QueueCount.OnValueChanged -= OnQueueChanged;
        CurrentUnitType.OnValueChanged -= OnQueueChanged;
        SecondsRemaining.OnValueChanged -= OnSecondsChanged;

        IsDestroyed = true;
        productionQueue.Clear();
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || productionQueue.Count == 0 || cancelled)
            return;

        if (ResourceManager.Instance == null || ResourceManager.Instance.IsMatchOver)
        {
            CancelProduction();
            return;
        }

        timer -= Time.deltaTime;
        float displaySeconds = Mathf.Max(0f, Mathf.Ceil(timer));
        if (!Mathf.Approximately(displaySeconds, SecondsRemaining.Value))
            SecondsRemaining.Value = displaySeconds;

        if (timer <= 0f && TryProduce(productionQueue.Peek()))
        {
            productionQueue.Dequeue();
            QueueCount.Value = productionQueue.Count;
            if (productionQueue.Count == 0)
            {
                CurrentUnitType.Value = -1;
                SecondsRemaining.Value = 0f;
                timer = 0f;
            }
            else
            {
                CurrentUnitType.Value = (int)productionQueue.Peek();
                timer = productionSeconds;
                SecondsRemaining.Value = Mathf.Ceil(timer);
            }
        }
        else if (timer <= 0f)
        {
            // Keep the paid order in the queue and retry when a NavMesh position is available.
            timer = 1f;
            SecondsRemaining.Value = 1f;
        }
    }

    public void InitializeForBuilding()
    {
        if (initialized || !IsServer)
            return;

        if (!TryFindSpawnPoint(out NavMeshHit hit, out Vector3 requestedPosition, true))
        {
            Debug.LogError($"Barracks '{name}' has no valid NavMesh spawn point within " +
                           $"{Mathf.Max(navMeshSampleRadius, navMeshFallbackRadius):0.#} units of {requestedPosition}.", this);
            return;
        }

        initialized = true;
        Debug.Log($"Barracks '{name}' spawn point is on NavMesh at {hit.position}.", this);
    }

    public void RequestUnit(int unitType)
    {
        if (!IsSpawned || cancelled || unitType < 0 || unitType > (int)UnitType.Minigun ||
            ResourceManager.Instance == null || ResourceManager.Instance.IsMatchOver)
            return;

        if (IsServer)
            TryQueueUnit((UnitType)unitType, NetworkManager.ServerClientId);
        else
            QueueUnitServerRpc(unitType);
    }

    [ServerRpc(RequireOwnership = false)]
    private void QueueUnitServerRpc(int unitType, ServerRpcParams rpcParams = default)
    {
        if (unitType < 0 || unitType > (int)UnitType.Minigun)
            return;

        TryQueueUnit((UnitType)unitType, rpcParams.Receive.SenderClientId);
    }

    private void TryQueueUnit(UnitType unitType, ulong requestingClientId)
    {
        if (!CanProduce)
            return;

        TeamManagerScript owner = TeamManagerScript.FindInParents(transform);
        TeamManagerScript.Team team = owner != null && owner.IsSpawned
            ? owner.CurrentTeam.Value
            : TeamManagerScript.Team.None;
        TeamManagerScript.Team requestingTeam = TeamManagerScript.GetTeamForClient(requestingClientId);

        if (team == TeamManagerScript.Team.None || team != requestingTeam ||
            GetPrefabForTeam(unitType, team) == null ||
            !ResourceManager.Instance.TrySpendGlaube(team, GetCost(unitType)))
            return;

        productionQueue.Enqueue(unitType);
        QueueCount.Value = productionQueue.Count;

        if (productionQueue.Count == 1)
        {
            CurrentUnitType.Value = (int)unitType;
            timer = productionSeconds;
            SecondsRemaining.Value = Mathf.Ceil(timer);
        }
    }

    private bool TryProduce(UnitType unitType)
    {
        TeamManagerScript owner = TeamManagerScript.FindInParents(transform);
        TeamManagerScript.Team team = owner != null && owner.IsSpawned
            ? owner.CurrentTeam.Value
            : TeamManagerScript.Team.None;
        GameObject prefab = GetPrefabForTeam(unitType, team);
        if (prefab == null)
        {
            Debug.LogError($"No {unitType} prefab is configured for team {team}.", this);
            return false;
        }

        if (!TryFindSpawnPoint(out NavMeshHit navHit, out Vector3 requestedPosition, false))
        {
            Debug.LogWarning($"No NavMesh spawn point found near barracks '{name}' at {requestedPosition}.", this);
            return false;
        }

        GameObject unit = Instantiate(prefab, navHit.position, prefab.transform.rotation);
        NetworkObject networkObject = unit.GetComponent<NetworkObject>();
        TeamManagerScript teamManager = unit.GetComponent<TeamManagerScript>();
        if (networkObject == null || teamManager == null)
        {
            Debug.LogError($"Unit prefab '{prefab.name}' needs a NetworkObject and TeamManagerScript.", prefab);
            Destroy(unit);
            return false;
        }

        teamManager.SetStartingTeam(team);
        networkObject.Spawn(true);
        return true;
    }

    private bool TryFindSpawnPoint(out NavMeshHit hit, out Vector3 requestedPosition, bool reportFallback)
    {
        requestedPosition = GetRequestedSpawnPosition();
        if (NavMesh.SamplePosition(requestedPosition, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            return true;

        float fallbackRadius = Mathf.Max(navMeshSampleRadius, navMeshFallbackRadius);
        if (fallbackRadius > navMeshSampleRadius &&
            NavMesh.SamplePosition(requestedPosition, out hit, fallbackRadius, NavMesh.AllAreas))
        {
            if (reportFallback)
                Debug.LogWarning($"Barracks '{name}' is using a farther NavMesh spawn point at {hit.position}.", this);
            return true;
        }

        hit = default;
        return false;
    }

    private Vector3 GetRequestedSpawnPosition()
    {
        BoxCollider buildingCollider = GetComponent<BoxCollider>();
        Vector3 buildingCenter = buildingCollider != null ? buildingCollider.bounds.center : transform.position;
        Vector3 mapCenter = GetMapCenter(buildingCenter);
        Vector3 inwardDirection = mapCenter - buildingCenter;
        inwardDirection.y = 0f;

        Vector3 horizontalOffset = new(spawnOffset.x, 0f, spawnOffset.z);
        float offsetDistance = horizontalOffset.magnitude;
        if (inwardDirection.sqrMagnitude < 0.001f || offsetDistance < 0.001f)
            return buildingCenter + horizontalOffset;

        inwardDirection.Normalize();
        return new Vector3(
            buildingCenter.x + inwardDirection.x * offsetDistance,
            mapCenter.y,
            buildingCenter.z + inwardDirection.z * offsetDistance);
    }

    private static Vector3 GetMapCenter(Vector3 fallback)
    {
        Tilemap tilemap = FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
            return new Vector3(fallback.x, 0f, fallback.z);

        BoundsInt bounds = tilemap.cellBounds;
        Vector3Int centerCell = new(
            bounds.xMin + bounds.size.x / 2,
            bounds.yMin + bounds.size.y / 2,
            bounds.zMin);
        return tilemap.GetCellCenterWorld(centerCell);
    }

    private GameObject GetPrefabForTeam(UnitType unitType, TeamManagerScript.Team team)
    {
        if (team == TeamManagerScript.Team.Azad)
        {
            return unitType switch
            {
                UnitType.Tank => azadTankPrefab,
                UnitType.RPG => azadRpgPrefab,
                UnitType.Minigun => azadMinigunPrefab,
                _ => null
            };
        }

        if (team == TeamManagerScript.Team.Kamo)
        {
            return unitType switch
            {
                UnitType.Tank => kamoTankPrefab,
                UnitType.RPG => kamoRpgPrefab,
                UnitType.Minigun => kamoMinigunPrefab,
                _ => null
            };
        }

        return null;
    }

    private int GetCost(UnitType unitType)
    {
        return unitType switch
        {
            UnitType.Tank => tankCost,
            UnitType.RPG => rpgCost,
            UnitType.Minigun => minigunCost,
            _ => int.MaxValue
        };
    }

    public int GetCost(int unitType)
    {
        if (unitType < 0 || unitType > (int)UnitType.Minigun)
            return int.MaxValue;
        return GetCost((UnitType)unitType);
    }

    public void CancelProduction()
    {
        if (!IsServer)
            return;

        cancelled = true;
        productionQueue.Clear();
        QueueCount.Value = 0;
        CurrentUnitType.Value = -1;
        SecondsRemaining.Value = 0f;
        timer = 0f;
    }

    private void OnQueueChanged(int previousValue, int newValue)
    {
        BuySystem.Instance?.RefreshProductionStatus();
    }

    private void OnSecondsChanged(float previousValue, float newValue)
    {
        BuySystem.Instance?.RefreshProductionStatus();
    }
}

