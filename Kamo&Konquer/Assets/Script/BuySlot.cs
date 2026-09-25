using UnityEngine;
using UnityEngine.UI;

public class BuySlot : MonoBehaviour
{
    public Sprite availableSprite;
    public Sprite unAvailableSprite;
    public bool isAvailable;
    public BuySystem buySystem;
    public int databaseItemID;

    private ResourceManager resourceManager;

    private void OnEnable()
    {
        BindResourceManager();
    }

    private void Start()
    {
        BindResourceManager();
        HandleResourcesChanged();
    }

    private void OnDisable()
    {
        if (resourceManager != null)
            resourceManager.OnResourceChanged -= HandleResourcesChanged;
        resourceManager = null;
    }

    private void BindResourceManager()
    {
        ResourceManager activeManager = ResourceManager.Instance;
        if (resourceManager == activeManager)
            return;

        if (resourceManager != null)
            resourceManager.OnResourceChanged -= HandleResourcesChanged;

        resourceManager = activeManager;
        if (resourceManager != null)
            resourceManager.OnResourceChanged += HandleResourcesChanged;
    }

    public void ClickedOnSlots()
    {
        BindResourceManager();
        ObjectData objectData = GetObjectData();
        if (buySystem == null || buySystem.placementSystem == null || resourceManager == null ||
            objectData == null || !resourceManager.CanAfford(objectData))
            return;

        buySystem.placementSystem.StartPlacement(databaseItemID);
    }

    private void UpdateAvailabilityUI()
    {
        Image image = GetComponent<Image>();
        Button button = GetComponent<Button>();

        if (isAvailable)
        {
            if (image != null)
                image.sprite = availableSprite;
            if (button != null)
                button.interactable = true;
        }
        else
        {
            if (image != null)
                image.sprite = unAvailableSprite;
            if (button != null)
                button.interactable = false;
        }
    }

    private void HandleResourcesChanged()
    {
        BindResourceManager();
        ObjectData objectData = GetObjectData();
        isAvailable = resourceManager != null && objectData != null && resourceManager.CanAfford(objectData);
        UpdateAvailabilityUI();
    }

    private ObjectData GetObjectData()
    {
        if (DatabaseManager.Instance == null || DatabaseManager.Instance.databaseSO == null)
            return null;

        ObjectData objectData = DatabaseManager.Instance.databaseSO.GetObjectByID(databaseItemID);
        return objectData != null && objectData.ID == databaseItemID && objectData.Prefab != null
            ? objectData
            : null;
    }
}
