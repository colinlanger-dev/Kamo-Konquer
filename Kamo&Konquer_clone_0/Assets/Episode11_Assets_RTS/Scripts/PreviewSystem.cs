using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PreviewSystem : MonoBehaviour
{
    [SerializeField] 
    private float previewYOffset = 0.06f;

    private GameObject previewObject;

    [SerializeField] 
    private Material previewMaterialPrefab;
    private Material previewMaterialInstance;


    private void Start()
    {
        previewMaterialInstance = new Material(previewMaterialPrefab);
    }
    public void StartShowingPlacementPreview(GameObject prefab, Vector2Int size)
    {
        StopShowingPreview();
        previewObject = Instantiate(prefab);
        PreparePreviewForPlacement(previewObject);
        PreparePreview(previewObject);
    }

    private static void PreparePreviewForPlacement(GameObject preview)
    {
        foreach (Collider collider in preview.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (UnityEngine.AI.NavMeshAgent agent in preview.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            agent.enabled = false;

        foreach (NetworkBehaviour behaviour in preview.GetComponentsInChildren<NetworkBehaviour>(true))
            behaviour.enabled = false;

        NetworkObject networkObject = preview.GetComponent<NetworkObject>();
        if (networkObject != null)
            networkObject.enabled = false;

        foreach (Transform child in preview.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.tag = "Untagged";
            child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
    }

    internal void StartShowingRemovePreview()
    {
        ApplyFeedbackToCursor(false);
    }

    private void PreparePreview(GameObject previewObject)
    {
        // Change the materials of the prefab (and its children) to semi-transparent

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                // Getting the current material color
                Color color = materials[i].color;
     
                // changing its alpha
                color.a = 0.5f;

                // setting the modified color
                materials[i].color = color;


                materials[i] = previewMaterialInstance;
            }

            renderer.materials = materials;
        }
    }

    public void StopShowingPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
    }

    public void UpdatePosition(Vector3 position, bool validity)
    {
        if (previewObject != null)
        {
            MovePreview(position);
            ApplyFeedbackToPreview(validity);
        }
      
        ApplyFeedbackToCursor(validity);
    }

    private void ApplyFeedbackToPreview(bool validity)
    {
        Color c = validity ? Color.white : Color.red;
        c.a = 0.5f;
        previewMaterialInstance.color = c;
    }

    private void ApplyFeedbackToCursor(bool validity)
    {
        Color c = validity ? Color.white : Color.red;
        c.a = 1f;
     
    }

    private void MovePreview(Vector3 position)
    {
        previewObject.transform.position = new Vector3(position.x, position.y + previewYOffset, position.z);
    }

}
