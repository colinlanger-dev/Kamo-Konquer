using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacementState : IBuildingState
{
    private readonly int buildingId;
    private readonly int selectedObjectIndex;
    private readonly Grid grid;
    private readonly PreviewSystem previewSystem;
    private readonly ObjectsDatabseSO database;
    private readonly GridData floorData;
    private readonly GridData furnitureData;

    public PlacementState(int buildingId, Grid grid, PreviewSystem previewSystem,
        ObjectsDatabseSO database, GridData floorData, GridData furnitureData)
    {
        this.buildingId = buildingId;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.database = database;
        this.floorData = floorData;
        this.furnitureData = furnitureData;
        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == buildingId);

        if (selectedObjectIndex < 0)
            throw new System.Exception($"No object with ID {buildingId}");

        ObjectData objectData = database.objectsData[selectedObjectIndex];
        previewSystem.StartShowingPlacementPreview(objectData.Prefab, objectData.Size);
    }

    public void EndState() => previewSystem.StopShowingPreview();

    public bool OnAction(Vector3Int gridPosition, uint requestId = 0)
    {
        if (!CheckPlacementValidity(gridPosition))
            return false;

        ResourceManager resources = ResourceManager.Instance;
        if (resources == null || !resources.RequestBuildingPlacement(buildingId, gridPosition, requestId))
            return false;

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), true);
        return true;
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition)
    {
        ObjectData objectData = database.objectsData[selectedObjectIndex];
        GridData data = GetAllFloorIDs().Contains(buildingId) ? floorData : furnitureData;
        return data.CanPlaceObjectAt(gridPosition, objectData.Size) &&
               HasGroundForFootprint(gridPosition, objectData.Size) &&
               PlacementSystem.IsLocalFootprintClear(grid, gridPosition, objectData.Size);
    }

    private bool HasGroundForFootprint(Vector3Int gridPosition, Vector2Int size)
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
            return true;

        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            Vector3 cellCenter = grid.GetCellCenterWorld(gridPosition + new Vector3Int(x, y, 0));
            if (!tilemap.HasTile(tilemap.WorldToCell(cellCenter)))
                return false;
        }

        return true;
    }

    private static List<int> GetAllFloorIDs() => new() { 11 };

    public void UpdateState(Vector3Int gridPosition)
    {
        ObjectData objectData = database.objectsData[selectedObjectIndex];
        bool valid = CheckPlacementValidity(gridPosition);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), valid);
    }
}
