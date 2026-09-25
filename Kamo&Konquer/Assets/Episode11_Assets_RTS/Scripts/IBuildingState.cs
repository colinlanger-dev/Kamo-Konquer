using UnityEngine;

public interface IBuildingState
{
    void EndState();
    bool OnAction(Vector3Int gridPosition, uint requestId = 0);
    void UpdateState(Vector3Int gridPosition);
}
