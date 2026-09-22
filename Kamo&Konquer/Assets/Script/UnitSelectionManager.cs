using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager instance {  get; set; }

    public List<GameObject> allUnitsList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    public LayerMask clickable;
    public LayerMask ground;
    public GameObject groundMarker;

    

    private Camera cam;

    private void Awake()
    {
        if(instance == null )
        {
            instance = this;
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
        if (Input.GetMouseButton(0))
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

        if (Input.GetMouseButton(1))
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
    }

    

    private void DeselectAll()
    {
        foreach (var unit in unitsSelected) 
        {
            EnableUnitMovement(unit, false);
            TriggerSelectorIndicator(unit,false);
        }

        unitsSelected.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();
        
        unitsSelected.Add(unit);
        TriggerSelectorIndicator(unit, true);

        EnableUnitMovement(unit, true);
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
            TriggerSelectorIndicator(unit, true);
            EnableUnitMovement(unit, true);

        }
        else
        {
            EnableUnitMovement(unit, false);
            TriggerSelectorIndicator(unit, false);
            unitsSelected.Remove(unit);
            
        }
    }

    private void TriggerSelectorIndicator(GameObject unit, bool isVisable)
    {
        unit.transform.Find("Indicator").gameObject.SetActive(isVisable);
    }
}
