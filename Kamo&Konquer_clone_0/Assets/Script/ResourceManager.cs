using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class ResourceManager : NetworkBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [SerializeField] private int startingGlaube = 300;
    [SerializeField] private int headquartersIncomePerSecond = 1;
    [SerializeField] private string lobbySceneName = "Lobby";
    public TextMeshProUGUI GlaubeUI;

    private readonly NetworkVariable<int> azadGlaube = new(300,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> kamoGlaube = new(300,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> MatchFinished = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<TeamManagerScript.Team> WinningTeam = new(TeamManagerScript.Team.None,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly HashSet<Vector3Int> reservedCells = new();
    private readonly Dictionary<ulong, BuildingPlacementRecord> placedBuildings = new();
    private Coroutine incomeRoutine;
    private GameObject matchEndOverlay;
    private TextMeshProUGUI matchResultText;

    public event Action OnResourceChanged;
    public bool IsMatchOver => MatchFinished.Value;

    public enum ResourcesType
    {
        Glaube
    }

    private sealed class BuildingPlacementRecord
    {
        public Vector3Int Cell;
        public Vector2Int Size;
        public int BuildingId;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        azadGlaube.OnValueChanged += OnFaithChanged;
        kamoGlaube.OnValueChanged += OnFaithChanged;
        MatchFinished.OnValueChanged += OnMatchFinishedChanged;
        WinningTeam.OnValueChanged += OnWinningTeamChanged;

        if (IsServer)
        {
            azadGlaube.Value = startingGlaube;
            kamoGlaube.Value = startingGlaube;
            MatchFinished.Value = false;
            WinningTeam.Value = TeamManagerScript.Team.None;
            BuildAndValidateNavMesh();
            incomeRoutine = StartCoroutine(PassiveIncomeLoop());
        }

        UpdateUI();
        if (MatchFinished.Value)
            ShowMatchEndOverlay();
    }

    public override void OnNetworkDespawn()
    {
        azadGlaube.OnValueChanged -= OnFaithChanged;
        kamoGlaube.OnValueChanged -= OnFaithChanged;
        MatchFinished.OnValueChanged -= OnMatchFinishedChanged;
        WinningTeam.OnValueChanged -= OnWinningTeamChanged;

        if (incomeRoutine != null)
            StopCoroutine(incomeRoutine);
        incomeRoutine = null;
    }

    public int GetGlaube() => GetGlaube(TeamManagerScript.LocalTeam);

    public int GetGlaube(TeamManagerScript.Team team)
    {
        return team switch
        {
            TeamManagerScript.Team.Azad => azadGlaube.Value,
            TeamManagerScript.Team.Kamo => kamoGlaube.Value,
            _ => 0
        };
    }

    public void IncreaseResource(ResourcesType resource, int amountToIncrease)
    {
        if (!IsServer || resource != ResourcesType.Glaube || amountToIncrease <= 0 || MatchFinished.Value)
            return;

        TryIncreaseGlaube(TeamManagerScript.LocalTeam, amountToIncrease);
    }

    public void DecreaseResource(ResourcesType resource, int amountToDecrease)
    {
        if (!IsServer || resource != ResourcesType.Glaube || amountToDecrease <= 0)
            return;

        TrySpendGlaube(TeamManagerScript.LocalTeam, amountToDecrease);
    }

    internal int GetResourceAmount(ResourcesType resource)
    {
        return GetResourceAmount(resource, TeamManagerScript.LocalTeam);
    }

    internal int GetResourceAmount(ResourcesType resource, TeamManagerScript.Team team)
    {
        return resource == ResourcesType.Glaube ? GetGlaube(team) : 0;
    }

    public bool CanAfford(ObjectData objectData)
    {
        return CanAfford(objectData, TeamManagerScript.LocalTeam);
    }

    internal bool CanAfford(ObjectData objectData, TeamManagerScript.Team team)
    {
        if (objectData == null || team == TeamManagerScript.Team.None)
            return false;

        return GetGlaube(team) >= GetGlaubeCost(objectData);
    }

    internal bool TryDecreaseResourcesBasedOnRequirement(ObjectData objectData)
    {
        return TryDecreaseResourcesBasedOnRequirement(objectData, TeamManagerScript.LocalTeam);
    }

    internal bool TryDecreaseResourcesBasedOnRequirement(ObjectData objectData, TeamManagerScript.Team team)
    {
        return objectData != null && TrySpendGlaube(team, GetGlaubeCost(objectData));
    }

    private static int GetGlaubeCost(ObjectData objectData)
    {
        int total = 0;
        if (objectData.requirements == null)
            return total;

        foreach (BuildRequirement requirement in objectData.requirements)
        {
            if (requirement != null && requirement.resource == ResourcesType.Glaube && requirement.amount > 0)
                total += requirement.amount;
        }

        return total;
    }

    public bool TrySpendGlaube(TeamManagerScript.Team team, int amount)
    {
        if (!IsServer || MatchFinished.Value || amount < 0 || team == TeamManagerScript.Team.None)
            return false;

        int current = GetGlaube(team);
        if (current < amount)
            return false;

        SetGlaube(team, current - amount);
        return true;
    }

    private void TryIncreaseGlaube(TeamManagerScript.Team team, int amount)
    {
        if (!IsServer || amount <= 0 || team == TeamManagerScript.Team.None)
            return;

        SetGlaube(team, GetGlaube(team) + amount);
    }

    private void SetGlaube(TeamManagerScript.Team team, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (team == TeamManagerScript.Team.Azad)
            azadGlaube.Value = amount;
        else if (team == TeamManagerScript.Team.Kamo)
            kamoGlaube.Value = amount;
        UpdateUI();
        OnResourceChanged?.Invoke();
    }

    public bool RequestBuildingPlacement(int buildingId, Vector3Int cell, uint requestId)
    {
        if (!IsSpawned || MatchFinished.Value)
            return false;

        if (IsServer)
            TryPlaceBuilding(buildingId, cell, NetworkManager.ServerClientId, requestId);
        else
            PlaceBuildingServerRpc(buildingId, cell.x, cell.y, cell.z, requestId);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceBuildingServerRpc(int buildingId, int cellX, int cellY, int cellZ, uint requestId,
        ServerRpcParams rpcParams = default)
    {
        TryPlaceBuilding(buildingId, new Vector3Int(cellX, cellY, cellZ),
            rpcParams.Receive.SenderClientId, requestId);
    }

    private void TryPlaceBuilding(int buildingId, Vector3Int cell, ulong requestingClientId, uint requestId)
    {
        TeamManagerScript.Team team = TeamManagerScript.GetTeamForClient(requestingClientId);
        DatabaseManager databaseManager = DatabaseManager.Instance;
        ObjectData objectData = databaseManager != null && databaseManager.databaseSO != null
            ? databaseManager.databaseSO.GetObjectByID(buildingId)
            : null;

        if (MatchFinished.Value || team == TeamManagerScript.Team.None || objectData == null ||
            objectData.ID != buildingId || objectData.Prefab == null || !CanAfford(objectData, team))
        {
            ReportPlacementResultClientRpc(requestingClientId, false, buildingId,
                cell.x, cell.y, cell.z, 0, 0, requestId);
            return;
        }

        Grid grid = FindFirstObjectByType<Grid>();
        Vector2Int size = objectData.Size;
        if (grid == null || size.x < 1 || size.y < 1 || !HasGroundForFootprint(grid, cell, size) ||
            !IsFootprintUnoccupied(grid, cell, size))
        {
            ReportPlacementResultClientRpc(requestingClientId, false, buildingId,
                cell.x, cell.y, cell.z, size.x, size.y, requestId);
            return;
        }

        GameObject instance = Instantiate(objectData.Prefab,
            grid.CellToWorld(cell), objectData.Prefab.transform.rotation);
        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        TeamManagerScript teamManager = instance.GetComponent<TeamManagerScript>();
        Constructable constructable = instance.GetComponent<Constructable>();
        if (networkObject == null || teamManager == null || constructable == null ||
            !TryDecreaseResourcesBasedOnRequirement(objectData, team))
        {
            Destroy(instance);
            ReportPlacementResultClientRpc(requestingClientId, false, buildingId,
                cell.x, cell.y, cell.z, size.x, size.y, requestId);
            return;
        }

        constructable.PrepareForSpawn(team);
        teamManager.SetStartingTeam(team);
        networkObject.Spawn(true);

        ReserveCells(cell, size);
        placedBuildings[networkObject.NetworkObjectId] = new BuildingPlacementRecord
        {
            Cell = cell,
            Size = size,
            BuildingId = buildingId
        };
        ReportPlacementResultClientRpc(requestingClientId, true, buildingId,
            cell.x, cell.y, cell.z, size.x, size.y, requestId);
    }

    private bool HasGroundForFootprint(Grid grid, Vector3Int cell, Vector2Int size)
    {
        Tilemap tilemap = FindFirstObjectByType<Tilemap>();
        if (tilemap != null)
        {
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                Vector3 worldCellCenter = grid.GetCellCenterWorld(cell + new Vector3Int(x, y, 0));
                if (!tilemap.HasTile(tilemap.WorldToCell(worldCellCenter)))
                    return false;
            }

            return true;
        }

        Vector3 center = PlacementSystem.GetBuildingCenter(grid, cell, size);
        return NavMesh.SamplePosition(center, out _, 0.75f, NavMesh.AllAreas);
    }

    private bool IsFootprintUnoccupied(Grid grid, Vector3Int cell, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            if (reservedCells.Contains(cell + new Vector3Int(x, y, 0)))
                return false;
        }

        return PlacementSystem.IsFootprintClear(grid, cell, size);
    }

    private void ReserveCells(Vector3Int cell, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
            reservedCells.Add(cell + new Vector3Int(x, y, 0));
    }

    [ClientRpc]
    private void ReportPlacementResultClientRpc(ulong requestingClientId, bool accepted, int buildingId,
        int cellX, int cellY, int cellZ, int sizeX, int sizeY, uint requestId)
    {
        if (!accepted)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == requestingClientId)
            {
                Debug.LogWarning("Der Host hat den Bauauftrag abgelehnt (Platz belegt, außerhalb des Spielfelds oder nicht genug Glaube).");
                PlacementSystem.Instance?.OnBuildingPlacementResult(false, buildingId, requestId);
            }
            return;
        }

        PlacementSystem.Instance?.RegisterNetworkBuilding(
            new Vector3Int(cellX, cellY, cellZ), new Vector2Int(sizeX, sizeY), buildingId);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == requestingClientId)
            PlacementSystem.Instance?.OnBuildingPlacementResult(true, buildingId, requestId);
    }

    public void NotifyBuildingDestroyed(ulong networkObjectId)
    {
        if (!IsServer || !placedBuildings.TryGetValue(networkObjectId, out BuildingPlacementRecord record))
            return;

        placedBuildings.Remove(networkObjectId);
        for (int x = 0; x < record.Size.x; x++)
        for (int y = 0; y < record.Size.y; y++)
            reservedCells.Remove(record.Cell + new Vector3Int(x, y, 0));

        RemovePlacementClientRpc(record.Cell.x, record.Cell.y, record.Cell.z,
            record.Size.x, record.Size.y, record.BuildingId);
    }

    [ClientRpc]
    private void RemovePlacementClientRpc(int cellX, int cellY, int cellZ, int sizeX, int sizeY, int buildingId)
    {
        PlacementSystem.Instance?.UnregisterNetworkBuilding(
            new Vector3Int(cellX, cellY, cellZ), new Vector2Int(sizeX, sizeY), buildingId);
    }

    private IEnumerator PassiveIncomeLoop()
    {
        WaitForSeconds wait = new(1f);
        while (!MatchFinished.Value)
        {
            yield return wait;
            if (MatchFinished.Value)
                yield break;

            GrantPassiveIncome(TeamManagerScript.Team.Azad);
            GrantPassiveIncome(TeamManagerScript.Team.Kamo);
        }
    }

    private void GrantPassiveIncome(TeamManagerScript.Team team)
    {
        bool hasHeadquarters = false;
        int income = 0;
        AttackControlerScript[] combatObjects = FindObjectsByType<AttackControlerScript>(FindObjectsSortMode.None);
        foreach (AttackControlerScript combatObject in combatObjects)
        {
            TeamManagerScript owner = TeamManagerScript.FindInParents(combatObject.transform);
            if (owner == null || owner.CurrentTeam.Value != team || combatObject.NetworkHealth.Value <= 0)
                continue;

            if (combatObject.IsHeadquarters)
            {
                hasHeadquarters = true;
                income += headquartersIncomePerSecond;
            }
        }

        if (!hasHeadquarters)
            return;

        Constructable[] buildings = FindObjectsByType<Constructable>(FindObjectsSortMode.None);
        foreach (Constructable building in buildings)
        {
            TeamManagerScript owner = TeamManagerScript.FindInParents(building.transform);
            if (owner != null && owner.CurrentTeam.Value == team && building.NetworkHealth.Value > 0)
                income += building.FaithIncomePerSecond;
        }

        TryIncreaseGlaube(team, income);
    }

    private void BuildAndValidateNavMesh()
    {
        NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        NavMeshSurface groundSurface = null;
        foreach (NavMeshSurface surface in surfaces)
        {
            if (surface != null && surface.enabled && surface.gameObject.name == "Grid")
            {
                groundSurface = surface;
                break;
            }
        }

        if (groundSurface == null)
        {
            Debug.LogError("No active NavMeshSurface was found on Grid; units cannot spawn or move.");
            return;
        }

        groundSurface.BuildNavMesh();
        foreach (AttackControlerScript headquarters in FindObjectsByType<AttackControlerScript>(FindObjectsSortMode.None))
        {
            if (!headquarters.IsHeadquarters)
                continue;

            if (!NavMesh.SamplePosition(headquarters.transform.position, out _, 3f, NavMesh.AllAreas))
                Debug.LogError($"Headquarters '{headquarters.name}' is not close to the baked NavMesh.", headquarters);
        }
    }

    public void ReportHeadquartersDestroyed(TeamManagerScript.Team defeatedTeam)
    {
        if (!IsServer || MatchFinished.Value || defeatedTeam == TeamManagerScript.Team.None)
            return;

        WinningTeam.Value = defeatedTeam == TeamManagerScript.Team.Azad
            ? TeamManagerScript.Team.Kamo
            : TeamManagerScript.Team.Azad;
        MatchFinished.Value = true;
        if (incomeRoutine != null)
            StopCoroutine(incomeRoutine);
        incomeRoutine = null;

        foreach (Spawner spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))
            spawner.CancelProduction();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReturnToLobbyServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer || !MatchFinished.Value || NetworkManager.Singleton == null ||
            NetworkManager.Singleton.SceneManager == null)
            return;

        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    private void OnFaithChanged(int previousValue, int newValue)
    {
        UpdateUI();
        OnResourceChanged?.Invoke();
    }

    private void OnMatchFinishedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
            ShowMatchEndOverlay();
    }

    private void OnWinningTeamChanged(TeamManagerScript.Team previousValue, TeamManagerScript.Team newValue)
    {
        if (MatchFinished.Value)
        {
            ShowMatchEndOverlay();
            UpdateMatchResultText();
        }
    }

    private void ShowMatchEndOverlay()
    {
        if (!IsClient || matchEndOverlay != null)
            return;

        matchEndOverlay = new GameObject("MatchEndOverlay", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = matchEndOverlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        GameObject panelObject = new("MatchEndPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(matchEndOverlay.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(440f, 230f);
        panelObject.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);

        GameObject titleObject = new("Result", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObject.transform.SetParent(panelObject.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.08f, 0.48f);
        titleRect.anchorMax = new Vector2(0.92f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        matchResultText = titleObject.GetComponent<TextMeshProUGUI>();
        matchResultText.alignment = TextAlignmentOptions.Center;
        matchResultText.fontSize = 32f;
        matchResultText.color = Color.white;
        UpdateMatchResultText();

        GameObject buttonObject = new("ReturnToLobbyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panelObject.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.2f, 0.12f);
        buttonRect.anchorMax = new Vector2(0.8f, 0.4f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.48f, 0.72f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(RequestReturnToLobby);

        GameObject buttonTextObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI buttonText = buttonTextObject.GetComponent<TextMeshProUGUI>();
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 23f;
        buttonText.color = Color.white;
        buttonText.text = "Zur Lobby";
    }

    private void RequestReturnToLobby()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
            return;
        }

        ReturnToLobbyServerRpc();
    }

    private void UpdateMatchResultText()
    {
        if (matchResultText != null)
            matchResultText.text = WinningTeam.Value == TeamManagerScript.LocalTeam ? "Sieg!" : "Niederlage";
    }

    private void UpdateUI()
    {
        if (GlaubeUI != null)
            GlaubeUI.text = GetGlaube().ToString();
    }

    public override void OnDestroy()
    {
        OnResourceChanged = null;
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }
}
