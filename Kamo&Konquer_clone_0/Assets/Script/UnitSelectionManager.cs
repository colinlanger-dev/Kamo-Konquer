using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }

    public List<GameObject> allUnitsList = new();
    public List<GameObject> unitsSelected = new();
    public LayerMask ground;
    public GameObject groundMarker;
    public bool attackCursorVisible;

    private Camera cam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        if (Input.GetMouseButtonDown(0))
            HandleSelection();

        if (Input.GetMouseButtonDown(1))
            HandleCommand();
    }

    private void HandleSelection()
    {
        UnitScript clickedUnit = FindOwnUnitAtMouse();
        bool addToSelection = Input.GetKey(KeyCode.LeftShift);

        if (clickedUnit == null)
        {
            if (!addToSelection)
                DeselectAll();
            return;
        }

        GameObject unit = clickedUnit.gameObject;
        if (!addToSelection)
            DeselectAll();

        if (unitsSelected.Contains(unit))
        {
            if (addToSelection)
                SelectUnit(unit, false);
            unitsSelected.Remove(unit);
        }
        else
        {
            unitsSelected.Add(unit);
            SelectUnit(unit, true);
        }
    }

    private void HandleCommand()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Transform target = FindAttackTarget(ray);
        if (target != null)
        {
            attackCursorVisible = true;
            foreach (GameObject unit in unitsSelected)
            {
                AttackControlerScript attack = unit.GetComponentInParent<AttackControlerScript>();
                if (attack != null)
                    attack.OrderAttack(target);
            }
            return;
        }

        attackCursorVisible = false;
        if (Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, ground))
        {
            if (groundMarker != null)
            {
                groundMarker.transform.position = groundHit.point;
                groundMarker.SetActive(false);
                groundMarker.SetActive(true);
            }

            foreach (GameObject unit in unitsSelected)
            {
                UnitMovementScript movement = unit.GetComponentInParent<UnitMovementScript>();
                if (movement != null)
                    movement.OrderMove(groundHit.point);
            }
        }
    }

    private UnitScript FindOwnUnitAtMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            UnitScript unit = hit.collider.GetComponentInParent<UnitScript>();
            TeamManagerScript team = TeamManagerScript.FindInParents(hit.collider.transform);
            if (unit != null && team != null && team.CurrentTeam.Value == TeamManagerScript.LocalTeam)
                return unit;
        }
        return null;
    }

    private Transform FindAttackTarget(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            TeamManagerScript team = TeamManagerScript.FindInParents(hit.collider.transform);
            if (team != null)
            {
                TeamManagerScript.Team targetTeam = team.CurrentTeam.Value;
                if (targetTeam != TeamManagerScript.Team.None && targetTeam != TeamManagerScript.LocalTeam)
                    return team.transform;

                if (targetTeam == TeamManagerScript.LocalTeam)
                    continue;
            }

            if (IsTaggedAttackTarget(hit.collider.transform))
            {
                AttackControlerScript controller = hit.collider.GetComponentInParent<AttackControlerScript>();
                if (controller != null)
                    return controller.transform;
            }
        }

        return null;
    }

    private static bool IsTaggedAttackTarget(Transform target)
    {
        return target.CompareTag("Unit") || target.CompareTag("Building") ||
               target.root.CompareTag("Unit") || target.root.CompareTag("Building");
    }

    public void DeselectAll()
    {
        foreach (GameObject unit in unitsSelected)
            SelectUnit(unit, false);
        unitsSelected.Clear();
    }

    public void DragSelect(GameObject unit)
    {
        if (unit == null)
            return;

        TeamManagerScript team = TeamManagerScript.FindInParents(unit.transform);
        if (team == null || team.CurrentTeam.Value != TeamManagerScript.LocalTeam || unitsSelected.Contains(unit))
            return;

        unitsSelected.Add(unit);
        SelectUnit(unit, true);
    }

    private void SelectUnit(GameObject unit, bool selected)
    {
        Transform indicator = unit.transform.Find("Indicator");
        if (indicator != null)
            indicator.gameObject.SetActive(selected);

        UnitMovementScript movement = unit.GetComponentInParent<UnitMovementScript>();
        if (movement != null)
            movement.SetSelected(selected);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
