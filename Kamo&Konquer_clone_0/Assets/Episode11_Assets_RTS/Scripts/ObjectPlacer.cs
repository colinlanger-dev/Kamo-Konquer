using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> placedGameObjects = new();

    public int PlaceObject(GameObject prefab, Vector3 position, TeamManagerScript.Team owningTeam = TeamManagerScript.Team.None)
    {
        // We instantiate the prefab into the cell
        GameObject newObject = Instantiate(prefab);
        newObject.transform.position = position;

        // Enable different things for example activate the obstical
        Constructable constructable = newObject.GetComponent<Constructable>();
        if (constructable == null)
        {
            Debug.LogError($"Placed building prefab '{prefab.name}' is missing a Constructable component.", newObject);
            Destroy(newObject);
            return -1;
        }

        constructable.SetOwningTeam(owningTeam);
        constructable.ConstructableWasPlaced();

        // Storing the positions that are now occupied
        placedGameObjects.Add(newObject);

        return placedGameObjects.Count - 1;
    }

    internal void RemoveObjectAt(int gameObjectIndex)
    {
        if(placedGameObjects.Count <= gameObjectIndex 
            || placedGameObjects[gameObjectIndex] == null)
             return;
        Destroy(placedGameObjects[gameObjectIndex]);
        placedGameObjects[gameObjectIndex] = null;
    }
}
