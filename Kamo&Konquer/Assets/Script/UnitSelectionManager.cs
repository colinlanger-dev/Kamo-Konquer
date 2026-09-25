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
    private GameObject markerInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (Instance.groundMarker == null && groundMarker != null)
            {
                Instance.groundMarker = groundMarker;
                Instance.InitializeMarker();
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeMarker();
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

        if (Input.GetMouseButtonDown(1))
            HandleCommand();
    }

    private void InitializeMarker()
    {
        if (groundMarker == null)
            return;

        // Scene objects can be used directly; a prefab asset needs one world-space instance.
        markerInstance = groundMarker.scene.IsValid()
            ? groundMarker
            : Instantiate(groundMarker);

        if (markerInstance.transform.parent != null)
            markerInstance.transform.SetParent(null, true);

        markerInstance.SetActive(false);
    }

    private void HandleCommand()
    {
        if (unitsSelected.Count == 0)
        {
            attackCursorVisible = false;
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Transform target = FindAttackTarget(ray);
        if (target != null)
        {
            attackCursorVisible = true;
            foreach (GameObject unit in unitsSelected)
            {
                if (unit == null)
                    continue;

                AttackControlerScript attack = unit.GetComponentInParent<AttackControlerScript>();
                if (attack != null)
                    attack.OrderAttack(target);
            }
            return;
        }

        attackCursorVisible = false;
        if (!Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, ground))
            return;

        if (markerInstance != null)
        {
            markerInstance.transform.position = groundHit.point;
            markerInstance.SetActive(true);
        }

        foreach (GameObject unit in unitsSelected)
        {
            if (unit == null)
                continue;

            UnitMovementScript movement = unit.GetComponentInParent<UnitMovementScript>();
            if (movement != null)
                movement.OrderMove(groundHit.point);
        }
    }

    public void SelectUnitAtMouse(bool additive)
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        UnitScript clickedUnit = FindOwnUnitAtMouse();
        if (clickedUnit == null)
        {
            if (!additive)
                DeselectAll();
            return;
        }

        GameObject unit = clickedUnit.gameObject;
        if (additive && unitsSelected.Contains(unit))
        {
            SelectUnit(unit, false);
            unitsSelected.Remove(unit);
            return;
        }

        if (!additive)
            DeselectAll();

        AddUnitToSelection(unit);
    }

    private UnitScript FindOwnUnitAtMouse()
    {
        int ownLayer = GetLayerForTeam(TeamManagerScript.LocalTeam);
        if (ownLayer < 0)
            return null;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, 1 << ownLayer, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            UnitScript unit = hit.collider.GetComponentInParent<UnitScript>();
            if (unit != null && IsSelectableUnit(unit.gameObject))
                return unit;
        }

        return null;
    }

    private Transform FindAttackTarget(Ray ray)
    {
        TeamManagerScript.Team enemyTeam = GetEnemyTeam(TeamManagerScript.LocalTeam);
        int enemyLayer = GetLayerForTeam(enemyTeam);
        if (enemyLayer < 0)
            return null;

        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, 1 << enemyLayer, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            AttackControlerScript controller = hit.collider.GetComponentInParent<AttackControlerScript>();
            if (controller != null)
                return controller.transform;
        }

        return null;
    }

    private static TeamManagerScript.Team GetEnemyTeam(TeamManagerScript.Team team)
    {
        if (team == TeamManagerScript.Team.Kamo)
            return TeamManagerScript.Team.Azad;
        if (team == TeamManagerScript.Team.Azad)
            return TeamManagerScript.Team.Kamo;
        return TeamManagerScript.Team.None;
    }

    public static int GetLayerForTeam(TeamManagerScript.Team team)
    {
        string layerName = team switch
        {
            TeamManagerScript.Team.Kamo => "TeamKamo",
            TeamManagerScript.Team.Azad => "TeamAzad",
            _ => null
        };

        return string.IsNullOrEmpty(layerName) ? -1 : LayerMask.NameToLayer(layerName);
    }

    public bool IsSelectableUnit(GameObject unit)
    {
        if (unit == null || unit.GetComponentInParent<UnitScript>() == null)
            return false;

        int ownLayer = GetLayerForTeam(TeamManagerScript.LocalTeam);
        return ownLayer >= 0 && unit.layer == ownLayer;
    }

    public void DeselectAll()
    {
        foreach (GameObject unit in unitsSelected)
        {
            if (unit != null)
                SelectUnit(unit, false);
        }
        unitsSelected.Clear();
    }

    public void DragSelect(GameObject unit)
    {
        if (IsSelectableUnit(unit))
            AddUnitToSelection(unit);
    }

    public void SetDragSelection(IList<GameObject> unitsInBox, IReadOnlyCollection<GameObject> initialSelection, bool additive)
    {
        HashSet<GameObject> desiredSelection = new();

        if (additive && initialSelection != null)
        {
            foreach (GameObject unit in initialSelection)
            {
                if (IsSelectableUnit(unit))
                    desiredSelection.Add(unit);
            }
        }

        if (unitsInBox != null)
        {
            foreach (GameObject unit in unitsInBox)
            {
                if (IsSelectableUnit(unit))
                    desiredSelection.Add(unit);
            }
        }

        for (int i = unitsSelected.Count - 1; i >= 0; i--)
        {
            GameObject unit = unitsSelected[i];
            if (unit == null || !desiredSelection.Contains(unit))
            {
                if (unit != null)
                    SelectUnit(unit, false);
                unitsSelected.RemoveAt(i);
            }
        }

        foreach (GameObject unit in desiredSelection)
            AddUnitToSelection(unit);
    }

    private void AddUnitToSelection(GameObject unit)
    {
        if (unit == null || unitsSelected.Contains(unit))
            return;

        unitsSelected.Add(unit);
        SelectUnit(unit, true);
    }

    private void SelectUnit(GameObject unit, bool selected)
    {
        if (unit == null)
            return;

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
