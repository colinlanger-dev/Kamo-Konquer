using System.Collections;
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
        AudioListener playerListener = instance.GetComponentInChildren<AudioListener>(true);
        playerCamera.enabled = false;
        if (playerListener != null)
            playerListener.enabled = false;

        bool cameraPositioned = false;
        if (existingCamera != null)
        {
            instance.transform.rotation = existingCamera.transform.rotation *
                                         Quaternion.Inverse(playerCamera.transform.localRotation);
            instance.transform.position = existingCamera.transform.position -
                                          instance.transform.rotation * playerCamera.transform.localPosition;
            if (existingCamera.orthographic && playerCamera.orthographic)
                playerCamera.orthographicSize = existingCamera.orthographicSize;

            cameraPositioned = TryPositionCameraForLocalTeam(instance, existingCamera, manager);
        }

        AssignCameraToBuildingSystem(playerCamera);
        AudioListener existingListener = GetComponent<AudioListener>();
        if (cameraPositioned || existingCamera == null)
            ActivatePlayerCamera(playerCamera, existingCamera, playerListener, existingListener);

        instance.name = $"PlayerCam_Client_{(manager != null ? manager.LocalClientId : 0)}";
        localPlayerCamera = instance;
        if (!cameraPositioned && existingCamera != null)
            StartCoroutine(PositionCameraWhenTeamIsReady(instance, playerCamera, existingCamera,
                playerListener, existingListener, manager));
        Debug.Log($"Spawned local player camera for client {(manager != null ? manager.LocalClientId : 0)}.");
    }

    private static void AssignCameraToBuildingSystem(Camera playerCamera)
    {
        InputManager[] inputManagers = FindObjectsByType<InputManager>(FindObjectsSortMode.None);
        foreach (InputManager inputManager in inputManagers)
            inputManager.SetSceneCamera(playerCamera);

        if (UnitSelectionManager.Instance != null)
            UnitSelectionManager.Instance.SetSceneCamera(playerCamera);
    }

    private IEnumerator PositionCameraWhenTeamIsReady(GameObject cameraRig, Camera playerCamera,
        Camera sceneCamera, AudioListener playerListener, AudioListener existingListener,
        NetworkManager manager)
    {
        float timeout = Time.realtimeSinceStartup + 5f;
        while (cameraRig != null && Time.realtimeSinceStartup < timeout)
        {
            if (TryPositionCameraForLocalTeam(cameraRig, sceneCamera, manager))
            {
                ActivatePlayerCamera(playerCamera, sceneCamera, playerListener, existingListener);
                yield break;
            }

            yield return null;
        }

        if (cameraRig != null)
        {
            Debug.LogWarning("Die lokale Teamzuordnung war beim Kamerastart noch nicht verfügbar.");
            Destroy(cameraRig);
            localPlayerCamera = null;
            AssignCameraToBuildingSystem(sceneCamera);
        }
    }

    private static void ActivatePlayerCamera(Camera playerCamera, Camera existingCamera,
        AudioListener playerListener, AudioListener existingListener)
    {
        playerCamera.enabled = true;
        playerCamera.tag = "MainCamera";
        if (existingCamera != null && existingCamera != playerCamera)
        {
            existingCamera.enabled = false;
            existingCamera.tag = "Untagged";
        }

        if (playerListener != null)
            playerListener.enabled = true;
        if (existingListener != null && existingListener != playerListener)
            existingListener.enabled = false;
    }

    private static bool TryPositionCameraForLocalTeam(GameObject cameraRig, Camera sceneCamera,
        NetworkManager manager)
    {
        TeamManagerScript.Team localTeam = TeamManagerScript.LocalTeam;
        if (localTeam == TeamManagerScript.Team.None && manager != null && manager.IsClient)
            localTeam = TeamManagerScript.GetTeamForClient(manager.LocalClientId);
        if (localTeam == TeamManagerScript.Team.None && (manager == null || !manager.IsListening))
            localTeam = TeamManagerScript.Team.Azad;
        if (localTeam == TeamManagerScript.Team.None)
            return false;

        GameObject azadBase = GameObject.Find("KriegerBasis");
        string targetBaseName = localTeam == TeamManagerScript.Team.Azad ? "KriegerBasis" : "AtzenBasis";
        GameObject targetBase = GameObject.Find(targetBaseName);
        GameObject opposingBase = GameObject.Find(
            localTeam == TeamManagerScript.Team.Azad ? "AtzenBasis" : "KriegerBasis");
        if (targetBase == null || azadBase == null || opposingBase == null)
            return false;

        Vector3 offsetFromAzadBase = sceneCamera.transform.position - azadBase.transform.position;
        float inset = sceneCamera.orthographic ? Mathf.Min(sceneCamera.orthographicSize * 0.5f, 8f) : 0f;
        Vector3 inwardDirection = (opposingBase.transform.position - targetBase.transform.position).normalized;
        cameraRig.transform.position = targetBase.transform.position + offsetFromAzadBase + inwardDirection * inset;
        Debug.Log($"Positioned {localTeam} camera near {targetBase.name} at {cameraRig.transform.position}.");
        return true;
    }
}
