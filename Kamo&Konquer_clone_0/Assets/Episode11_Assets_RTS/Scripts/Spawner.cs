using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject spawnPrefab;    // Prefab, das gespawnt wird
    [SerializeField] private float spawnInterval = 10f; // Sekunden zwischen den Spawns
    [SerializeField] private bool startOnAwake = false; // true = startet sofort, ohne Aufruf von auﬂen

    private Coroutine spawnRoutine;

    void Start()
    {
        if (startOnAwake)
            StartSpawning();
    }

    public void StartSpawning()
    {
        if (spawnPrefab == null)
        {
            Debug.LogWarning("PrefabSpawner: spawnPrefab ist nicht zugewiesen.", this);
            return;
        }

        // Nur einmal starten, auch bei mehrfachem Aufruf
        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine == null) return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private IEnumerator SpawnLoop()
    {
        var warten = new WaitForSeconds(spawnInterval);

        while (true)
        {
            yield return warten;
            Spawn();
        }
    }

    private void Spawn()
    {
        Vector3 position = new Vector3(transform.position.x, transform.position.y, transform.position.z - 5);
        
        Instantiate(spawnPrefab, position, Quaternion.identity);
    }
}