using Unity.Netcode;
using UnityEngine;

public class InGameBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject playerCameraPrefab;
    private static GameObject localPlayerCamera;

    private void Start()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening && !manager.IsClient)
            return;

        if (localPlayerCamera != null)
            return;

        if (playerCameraPrefab == null)
        {
            Debug.LogError("InGameBootstrap needs the PlayerCam prefab assigned.");
            return;
        }

        GameObject instance = Instantiate(playerCameraPrefab);
        Camera playerCamera = instance.GetComponentInChildren<Camera>(true);
        if (playerCamera == null)
        {
            Debug.LogError("The assigned PlayerCam prefab has no Camera component.", instance);
            Destroy(instance);
            return;
        }

        Camera existingCamera = GetComponent<Camera>();
        if (existingCamera != null)
        {
            instance.transform.rotation = existingCamera.transform.rotation *
                                         Quaternion.Inverse(playerCamera.transform.localRotation);
            instance.transform.position = existingCamera.transform.position -
                                          instance.transform.rotation * playerCamera.transform.localPosition;
            if (existingCamera.orthographic && playerCamera.orthographic)
                playerCamera.orthographicSize = existingCamera.orthographicSize;
        }

        playerCamera.enabled = true;
        playerCamera.tag = "MainCamera";
        AudioListener playerListener = instance.GetComponentInChildren<AudioListener>(true);
        if (playerListener != null)
            playerListener.enabled = true;

        if (existingCamera != null && existingCamera != playerCamera)
            existingCamera.enabled = false;

        AudioListener existingListener = GetComponent<AudioListener>();
        if (existingListener != null && existingListener != playerListener)
            existingListener.enabled = false;

        instance.name = $"PlayerCam_Client_{(manager != null ? manager.LocalClientId : 0)}";
        localPlayerCamera = instance;
        Debug.Log($"Spawned local player camera for client {(manager != null ? manager.LocalClientId : 0)}.");
    }
}
