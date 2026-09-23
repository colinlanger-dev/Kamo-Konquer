using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class BuySystem : MonoBehaviour
{
    public GameObject buildingsPanel;
    public GameObject unitsPanel;

    public Button buildingButton;
    public Button unitsButton;

    public PlacementSystem placementSystem;

    private void Start()
    {
        unitsButton.onClick.AddListener(UnitsCategorySelected);
        buildingButton.onClick.AddListener(BuildingsCategorySelected);

        unitsPanel.SetActive(false);
        buildingsPanel.SetActive(true);
    
}

    private void BuildingsCategorySelected()
    {
        unitsPanel.SetActive(false);
        buildingsPanel.SetActive(true);
    }

    private void UnitsCategorySelected()
    {
        unitsPanel.SetActive(true);
        buildingsPanel.SetActive(false);
    }
}
