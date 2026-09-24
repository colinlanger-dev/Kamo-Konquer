using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance {  get; set; }

    public List<GameObject> allUnitsList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    public LayerMask clickable;
    public LayerMask ground;
    public GameObject groundMarker;

    public LayerMask attackable;
    public bool attackCursorVisible;

    

    private Camera cam;

    private void Awake()
    {
        if(Instance == null )
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        cam = Camera.main;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickable))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    MultiSelect(hit.collider.gameObject);
                }
                else
                {
                    SelectByClicking(hit.collider.gameObject);
                }

            }
            else
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeselectAll();
                }

            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground))
            {
                
              
                    
                    groundMarker.transform.position = hit.point;

                    groundMarker.SetActive(false);
                    groundMarker.SetActive(true);

             
                

            }
        }

        if (unitsSelected.Count > 0 && AtleastOneOffensiveUnit(unitsSelected))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);


            if (Physics.Raycast(ray, out hit, Mathf.Infinity, attackable))
            {
                Debug.Log("Enemy Hovered with mouse");

                attackCursorVisible = true;

                if (Input.GetMouseButtonDown(1))
                {
                    // The ray can hit a child collider; the enemy controller usually
                    // lives on the root object and is needed by the damage code.
                    AttackControlerScript targetController = hit.collider.GetComponentInParent<AttackControlerScript>();
                    Transform target = targetController != null ? targetController.transform : hit.transform;

                    foreach (GameObject unit in unitsSelected)
                    {
                        if (unit.GetComponent<AttackControlerScript>())
                        {
                            unit.GetComponent<AttackControlerScript>().targetToAttack = target;
                            Debug.Log($"Attack target set: {unit.name} -> {target.name}");
                        }
                    }
                }


            }
        }

    }

    private bool AtleastOneOffensiveUnit(List<GameObject> unitsSelected)
    {
        foreach (GameObject units in unitsSelected)
        {
            return true;
        }

        return false;
    }

    public void DeselectAll()
    {
        foreach (var unit in unitsSelected) 
        {
            SelectUnit(unit, false);
        }

        unitsSelected.Clear();
    }

    

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();
        
        unitsSelected.Add(unit);
        SelectUnit(unit, true);
    }

    private void EnableUnitMovement(GameObject unit, bool enabled)
    {
        unit.GetComponent<UnitMovementScript>().enabled = enabled;
    }
    private void MultiSelect(GameObject unit)
    {
        if(!unitsSelected.Contains(unit))
        {
            unitsSelected.Add(unit);
            SelectUnit(unit, true);

        }
        else
        {
            SelectUnit(unit, false);
            unitsSelected.Remove(unit);
            
        }
    }

    private void TriggerSelectorIndicator(GameObject unit, bool isVisable)
    {
        unit.transform.Find("Indicator").gameObject.SetActive(isVisable);
    }

    public void DragSelect(GameObject unit)
    {
        if(!unitsSelected.Contains(unit))
        {
            unitsSelected.Add(unit);
            SelectUnit( unit, true);
        }
    }

    private void SelectUnit(GameObject unit, bool isSelected)
    {
        TriggerSelectorIndicator(unit, isSelected);
        EnableUnitMovement(unit, isSelected);
    }

}
