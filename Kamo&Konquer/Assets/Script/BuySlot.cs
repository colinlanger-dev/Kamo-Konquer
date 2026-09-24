using System;
using UnityEngine;
using UnityEngine.UI;

public class BuySlot : MonoBehaviour
{
    public Sprite availableSprite;
    public Sprite unAvailableSprite;


    public bool isAvailable;

    public BuySystem buySystem;

    public int databaseItemID;

    private void Start()
    {
        HandleResourcesChanged();
    }

    private void OnEnable()
    {
        ResourceManager.Instance.OnResourceChanged += HandleResourcesChanged;
    }

    private void OnDisable()
    {
        ResourceManager.Instance.OnResourceChanged -= HandleResourcesChanged;
    }

    public void ClickedOnSlots()
    {
        Debug.Log($"Slot {databaseItemID} geklickt, verfügbar: {isAvailable}");
        if (isAvailable)
        {
            buySystem.placementSystem.StartPlacement(databaseItemID);
        }
    }

    private void UpdateAvailabilityUI()
    {
        if (isAvailable)
        {
            GetComponent<Image>().sprite = availableSprite;
            GetComponent<Button>().interactable = true;

        }
        else
        {
            GetComponent<Image>().sprite = unAvailableSprite;
            GetComponent<Button>().interactable = false;
        }
    }
    private void HandleResourcesChanged()
    {
        ObjectData objectData = DatabaseManager.Instance.databaseSO.objectsData[databaseItemID];

        bool requiremtMet = true;

        foreach (BuildRequirement req in objectData.requirements)
        {
            int have = ResourceManager.Instance.GetResourceAmount(req.resource);
            Debug.Log($"Slot {databaseItemID}: braucht {req.amount} {req.resource}, vorhanden {have}");

            if (have < req.amount)
            {
                requiremtMet = false;
                break;
            }
        }

        isAvailable = requiremtMet;
        Debug.Log($"Slot {databaseItemID}: isAvailable = {isAvailable}");

        UpdateAvailabilityUI();
    }
}
