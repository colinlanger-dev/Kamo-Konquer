using System;
using UnityEngine;
using UnityEngine.UI;

public class BuySlot : MonoBehaviour
{
    public Sprite availableSprite;
    public Sprite unAvailableSprite;


    private bool isAvailable;

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
            if (ResourceManager.Instance.GetResourceAmount(req.resource) < req.amount)
            {
                requiremtMet = false;
                break;
            }
        }

        isAvailable = requiremtMet;

        UpdateAvailabilityUI();
    }
}
