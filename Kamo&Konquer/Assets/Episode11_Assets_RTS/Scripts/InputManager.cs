using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{

    [SerializeField] private Camera sceneCamera;
    [SerializeField] private LayerMask placementLayerMask;

    [SerializeField]  private Vector3 lastPosition;

    public event Action OnClicked, OnExit;

    public void SetSceneCamera(Camera camera)
    {
        if (camera != null)
            sceneCamera = camera;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
             OnClicked?.Invoke();
        if (Input.GetKeyDown(KeyCode.Escape))
             OnExit?.Invoke();
    }

    public bool IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();


    public Vector3 GetSelectedMapPosition()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;
        if (sceneCamera == null)
            return lastPosition;

        Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayerMask))
        {
            lastPosition = hit.point;
            return lastPosition;
        }

        // The current map is a flat Tilemap and has no 3D collider, so a
        // Physics.Raycast cannot hit it. Intersect the map's y=0 plane instead.
        Plane mapPlane = new Plane(Vector3.up, Vector3.zero);
        if (mapPlane.Raycast(ray, out float distance))
            lastPosition = ray.GetPoint(distance);

        return lastPosition;
    }
}
