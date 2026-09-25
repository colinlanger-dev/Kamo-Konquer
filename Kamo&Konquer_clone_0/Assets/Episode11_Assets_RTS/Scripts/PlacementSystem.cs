using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;

    [SerializeField] private ObjectsDatabseSO database;

    [SerializeField] private GridData floorData, furnitureData;

    [SerializeField] private PreviewSystem previewSystem;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;

    [SerializeField] private ObjectPlacer objectPlacer;

    int selectedID;

    IBuildingState buildingState;
    private bool removingMode;
    private bool pendingBuildingRequest;
    private int pendingBuildingId;
    private uint nextPlacementRequestId;
    private uint pendingPlacementRequestId;

    private void Awake()
    {
        Instance = this;
        floorData = new();
        furnitureData = new();
    }

    public void StartPlacement(int ID)
    {
        Debug.Log("Should Start Placement");

        selectedID = ID;

        Debug.Log("Placement ID: " + ID);

        StopPlacement();
        removingMode = false;

        buildingState = new PlacementState(
            ID,
            grid,
            previewSystem,
            database,
            floorData,
            furnitureData);
        pendingBuildingRequest = false;

        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    public void StartRemoving()
    {
        StopPlacement();
        removingMode = true;

        buildingState = new RemovingState(grid, previewSystem, floorData, furnitureData, objectPlacer);

        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    private void PlaceStructure()
    {
        if (pendingBuildingRequest)
            return;

        if (inputManager.IsPointerOverUI())
        {
            Debug.Log("Pointer was over UI - Returned");
            return;
        }
        // When we click on a cell, we get the cell
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        if (buildingState == null)
            return;

        lastDetectedPosition = gridPosition;
        if (!removingMode)
        {
            pendingBuildingId = selectedID;
            pendingPlacementRequestId = ++nextPlacementRequestId;
            if (pendingPlacementRequestId == 0)
                pendingPlacementRequestId = ++nextPlacementRequestId;
            pendingBuildingRequest = true;
        }

        uint requestId = pendingPlacementRequestId;
        if (!buildingState.OnAction(gridPosition, requestId))
        {
            pendingBuildingRequest = false;
            return;
        }

        // The host may answer immediately for the local player and close this state
        // from the result callback before OnAction returns.
        if (buildingState == null)
            return;

        if (removingMode)
            StopPlacement();
    }

    public void OnBuildingPlacementResult(bool accepted, int buildingId, uint requestId)
    {
        if (!pendingBuildingRequest || buildingId != pendingBuildingId ||
            requestId != pendingPlacementRequestId)
            return;

        pendingBuildingRequest = false;
        if (!accepted)
        {
            Debug.LogWarning("Der Host hat den Bauauftrag abgelehnt. Du kannst einen anderen Platz versuchen.");
            if (buildingState != null)
                buildingState.UpdateState(lastDetectedPosition);
            return;
        }

        ObjectData objectData = database.GetObjectByID(buildingId);
        if (objectData != null && objectData.benefits != null)
        {
            foreach (BuildBenefits benefit in objectData.benefits)
                CalculateAndAddBenefit(benefit);
        }

        StopPlacement();
    }

    private void CalculateAndAddBenefit(BuildBenefits bf)
    {
        switch (bf.benefitType)
        {
            case BuildBenefits.BenefitType.Housing:
                //   StatusManager.Instance.IncreaseHousing(bf.benefitAmount);
                break;
        }
    }

    private void StopPlacement()
    {
        if (buildingState == null)
            return;

        pendingBuildingRequest = false;
        buildingState.EndState();

        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;

        lastDetectedPosition = Vector3Int.zero;

        buildingState = null;
    }

    private void Update()
    {
        // We return because we did not selected an item to place (not in placement mode)
        // So there is no need to show cell indicator
        if (buildingState == null)
            return;

        if (pendingBuildingRequest)
            return;

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        if (lastDetectedPosition != gridPosition)
        {
            buildingState.UpdateState(gridPosition);
            lastDetectedPosition = gridPosition;
        }
    }

    public static Vector3 GetBuildingCenter(Grid placementGrid, Vector3Int cell, Vector2Int size)
    {
        if (placementGrid == null)
            return Vector3.zero;

        Vector3 firstCellCenter = placementGrid.GetCellCenterWorld(cell);
        Vector3 footprintOffset = placementGrid.transform.TransformVector(
            new Vector3((size.x - 1) * 0.5f, (size.y - 1) * 0.5f, 0f));
        return firstCellCenter + footprintOffset;
    }

    public static bool IsLocalFootprintClear(Grid placementGrid, Vector3Int cell, Vector2Int size)
    {
        if (placementGrid == null || size.x < 1 || size.y < 1)
            return false;

        Vector3 center = GetBuildingCenter(placementGrid, cell, size);
        Vector3 halfExtents = new(
            Mathf.Max(0.1f, size.x * 0.5f - 0.05f),
            1.5f,
            Mathf.Max(0.1f, size.y * 0.5f - 0.05f));
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider collider in colliders)
        {
            if (collider != null && (collider.CompareTag("Unit") || collider.CompareTag("Building") ||
                collider.transform.root.CompareTag("Unit") || collider.transform.root.CompareTag("Building")))
                return false;
        }

        return true;
    }

    public static bool IsFootprintClear(Grid placementGrid, Vector3Int cell, Vector2Int size)
    {
        return IsLocalFootprintClear(placementGrid, cell, size);
    }

    public void RegisterNetworkBuilding(Vector3Int cell, Vector2Int size, int buildingId)
    {
        if (grid == null || size.x < 1 || size.y < 1)
            return;

        GridData targetData = buildingId == 11 ? floorData : furnitureData;

        if (!targetData.CanPlaceObjectAt(cell, size))
            return;

        targetData.AddObjectAt(cell, size, buildingId, -1);
    }

    public void UnregisterNetworkBuilding(Vector3Int cell, Vector2Int size, int buildingId)
    {
        if (floorData.HasCell(cell))
            floorData.RemoveObjectAt(cell);
        else if (furnitureData.HasCell(cell))
            furnitureData.RemoveObjectAt(cell);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

}
