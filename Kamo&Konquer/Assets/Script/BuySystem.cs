using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuySystem : MonoBehaviour
{
    public static BuySystem Instance { get; private set; }

    public GameObject buildingsPanel;
    public GameObject unitsPanel;
    public Button buildingButton;
    public Button unitsButton;
    public PlacementSystem placementSystem;

    private readonly List<Button> productionButtons = new();
    private readonly List<TextMeshProUGUI> productionLabels = new();
    private Spawner selectedSpawner;
    private bool unitsTabSelected;
    private ResourceManager boundResourceManager;

    private void Awake()
    {
        Instance = this;
        CreateProductionChoices();
    }

    private void Start()
    {
        if (unitsButton != null)
            unitsButton.onClick.AddListener(UnitsCategorySelected);
        if (buildingButton != null)
            buildingButton.onClick.AddListener(BuildingsCategorySelected);

        unitsTabSelected = false;
        BindResourceManager();
        UpdatePanels();
    }

    private void OnEnable() => BindResourceManager();

    private void Update()
    {
        if (boundResourceManager == null)
            BindResourceManager();
        if (selectedSpawner != null && !selectedSpawner.CanProduce)
            HideProduction();
    }

    private void CreateProductionChoices()
    {
        if (unitsPanel == null)
            return;

        for (int i = 0; i < unitsPanel.transform.childCount; i++)
            unitsPanel.transform.GetChild(i).gameObject.SetActive(false);

        string[] unitNames = { "Tank", "RPG", "Minigun" };
        for (int unitType = 0; unitType < unitNames.Length; unitType++)
        {
            GameObject buttonObject = new(unitNames[unitType] + "ProductionButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(unitsPanel.transform, false);
            buttonObject.layer = unitsPanel.layer;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.2f, 0.28f, 0.36f, 0.96f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            int capturedUnitType = unitType;
            button.onClick.AddListener(() => QueueUnit(capturedUnitType));
            productionButtons.Add(button);

            GameObject labelObject = new(unitNames[unitType] + "Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            labelObject.layer = unitsPanel.layer;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(5f, 4f);
            labelRect.offsetMax = new Vector2(-5f, -4f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.color = Color.white;
            label.raycastTarget = false;
            productionLabels.Add(label);
        }
    }

    private void BuildingsCategorySelected()
    {
        unitsTabSelected = false;
        UpdatePanels();
    }

    private void UnitsCategorySelected()
    {
        unitsTabSelected = true;
        UpdatePanels();
    }

    public void ShowProductionFor(Spawner spawner)
    {
        if (spawner == null || !spawner.CanProduce)
            return;

        selectedSpawner = spawner;
        unitsTabSelected = true;
        UpdatePanels();
        RefreshProductionStatus();
    }

    public void HideProduction()
    {
        selectedSpawner = null;
        if (unitsTabSelected)
            unitsTabSelected = false;
        UpdatePanels();
    }

    private void UpdatePanels()
    {
        if (unitsPanel != null)
            unitsPanel.SetActive(unitsTabSelected && selectedSpawner != null);
        if (buildingsPanel != null)
            buildingsPanel.SetActive(!unitsTabSelected || selectedSpawner == null);
    }

    private void QueueUnit(int unitType)
    {
        if (selectedSpawner == null || ResourceManager.Instance == null ||
            ResourceManager.Instance.GetGlaube(TeamManagerScript.LocalTeam) < selectedSpawner.GetCost(unitType))
            return;

        selectedSpawner.RequestUnit(unitType);
        RefreshProductionStatus();
    }

    private void BindResourceManager()
    {
        ResourceManager manager = ResourceManager.Instance;
        if (manager == boundResourceManager)
            return;

        if (boundResourceManager != null)
            boundResourceManager.OnResourceChanged -= RefreshProductionStatus;
        boundResourceManager = manager;
        if (boundResourceManager != null)
            boundResourceManager.OnResourceChanged += RefreshProductionStatus;
    }

    public void RefreshProductionStatus()
    {
        if (selectedSpawner == null || ResourceManager.Instance == null)
            return;

        string[] unitNames = { "Tank", "RPG", "Minigun" };
        for (int i = 0; i < productionButtons.Count; i++)
        {
            int cost = selectedSpawner.GetCost(i);
            int faith = ResourceManager.Instance.GetGlaube(TeamManagerScript.LocalTeam);
            productionButtons[i].interactable = selectedSpawner.CanProduce && faith >= cost;

            string status = $"{unitNames[i]}\n{cost} Glaube\nWarteschlange: {selectedSpawner.QueueCount.Value}";
            if (selectedSpawner.CurrentUnitType.Value == i && selectedSpawner.QueueCount.Value > 0)
                status += $"\nFertig in {selectedSpawner.SecondsRemaining.Value:0}s";
            productionLabels[i].text = status;
        }
    }

    private void OnDestroy()
    {
        if (boundResourceManager != null)
            boundResourceManager.OnResourceChanged -= RefreshProductionStatus;
        if (Instance == this)
            Instance = null;
    }
}

