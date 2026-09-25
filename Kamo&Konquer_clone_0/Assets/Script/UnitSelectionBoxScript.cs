using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitSelectionBoxScript : MonoBehaviour
{
    [SerializeField] private RectTransform boxVisual;
    [SerializeField] private float dragThreshold = 8f;

    private Camera myCam;
    private UnitSelectionManager selectionManager;
    private readonly List<GameObject> unitsInBox = new();
    private GameObject[] initialSelection;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private Rect selectionBox;
    private bool isSelecting;
    private bool isDragging;
    private bool additiveSelection;
    private bool pointerOverUi;

    private void Start()
    {
        myCam = GetComponent<Camera>();
        if (myCam == null)
            myCam = GetComponentInParent<Camera>();
        if (myCam == null)
            myCam = Camera.main;

        if (boxVisual == null)
            CreateSelectionVisual();

        if (boxVisual != null)
            boxVisual.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (myCam == null)
            myCam = GetComponent<Camera>() ?? GetComponentInParent<Camera>() ?? Camera.main;
        if (selectionManager == null)
            selectionManager = UnitSelectionManager.Instance;

        if (Input.GetMouseButtonDown(0))
            BeginSelection();

        if (!isSelecting)
            return;

        endPosition = Input.mousePosition;
        if (!isDragging && (endPosition - startPosition).sqrMagnitude >= dragThreshold * dragThreshold)
        {
            isDragging = true;
            additiveSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        if (isDragging && !pointerOverUi)
        {
            UpdateSelectionRect();
            SelectUnitsInsideBox();
            DrawVisual();
        }

        if (Input.GetMouseButtonUp(0))
            FinishSelection();
    }

    private void BeginSelection()
    {
        if (selectionManager == null)
            selectionManager = UnitSelectionManager.Instance;

        isSelecting = true;
        isDragging = false;
        startPosition = Input.mousePosition;
        endPosition = startPosition;
        selectionBox = new Rect(startPosition, Vector2.zero);
        pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        additiveSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        initialSelection = selectionManager != null ? selectionManager.unitsSelected.ToArray() : new GameObject[0];

        if (boxVisual != null)
            boxVisual.gameObject.SetActive(false);
    }

    private void FinishSelection()
    {
        if (!pointerOverUi && selectionManager != null)
        {
            if (isDragging)
            {
                endPosition = Input.mousePosition;
                UpdateSelectionRect();
                SelectUnitsInsideBox();
            }
            else
            {
                selectionManager.SelectUnitAtMouse(additiveSelection);
            }
        }

        isSelecting = false;
        isDragging = false;
        pointerOverUi = false;
        initialSelection = null;

        if (boxVisual != null)
            boxVisual.gameObject.SetActive(false);
    }

    private void UpdateSelectionRect()
    {
        float minX = Mathf.Min(startPosition.x, endPosition.x);
        float maxX = Mathf.Max(startPosition.x, endPosition.x);
        float minY = Mathf.Min(startPosition.y, endPosition.y);
        float maxY = Mathf.Max(startPosition.y, endPosition.y);
        selectionBox = Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private void SelectUnitsInsideBox()
    {
        if (selectionManager == null || myCam == null)
            return;

        unitsInBox.Clear();
        foreach (GameObject unit in selectionManager.allUnitsList)
        {
            if (unit == null || !selectionManager.IsSelectableUnit(unit))
                continue;

            Vector3 screenPosition = myCam.WorldToScreenPoint(unit.transform.position);
            if (screenPosition.z > 0f && selectionBox.Contains(screenPosition))
                unitsInBox.Add(unit);
        }

        selectionManager.SetDragSelection(unitsInBox, initialSelection, additiveSelection);
    }

    private void DrawVisual()
    {
        if (boxVisual == null)
            return;

        boxVisual.gameObject.SetActive(true);
        boxVisual.position = (startPosition + endPosition) * 0.5f;
        boxVisual.sizeDelta = new Vector2(
            Mathf.Abs(startPosition.x - endPosition.x),
            Mathf.Abs(startPosition.y - endPosition.y));
    }

    private void CreateSelectionVisual()
    {
        GameObject canvasObject = new GameObject("SelectionBoxCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        GameObject visualObject = new GameObject("SelectionBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        visualObject.transform.SetParent(canvasObject.transform, false);
        boxVisual = visualObject.GetComponent<RectTransform>();
        boxVisual.anchorMin = Vector2.zero;
        boxVisual.anchorMax = Vector2.zero;
        boxVisual.pivot = new Vector2(0.5f, 0.5f);

        Image image = visualObject.GetComponent<Image>();
        image.color = new Color(0.2f, 0.75f, 1f, 0.2f);
        image.raycastTarget = false;

        Outline outline = visualObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.75f, 1f, 0.95f);
        outline.effectDistance = new Vector2(1f, 1f);
    }
}
