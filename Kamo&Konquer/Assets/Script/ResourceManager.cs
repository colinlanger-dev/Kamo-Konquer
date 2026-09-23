using System;
using TMPro;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

    }

    public int credits = 150;

    public event Action OnResourceChanged;

    public TextMeshProUGUI GlaubeUI;

    public enum ResourcesType
    {
        Glaube
    }

    private void Start()
    {
        UpdateUI();
    }

    public void IncreaseResource(ResourcesType resource, int amountToIncrease)
    {
        switch (resource)
        {
            case ResourcesType.Glaube:
                credits += amountToIncrease;
                break;
            default:
                break;
        }

        OnResourceChanged?.Invoke();
    }

    public void DecreaseResource(ResourcesType resource, int amountToDecrease)
    {
        switch (resource)
        {
            case ResourcesType.Glaube:
                credits -= amountToDecrease;
                break;
            default:
                break;
        }

        OnResourceChanged?.Invoke();
    }


    private void UpdateUI()
    {
        GlaubeUI.text = $"{credits}";
    }

    internal int GetResourceAmmount(ResourcesType resource)
    {
        switch (resource)
        {
            case ResourcesType.Glaube:
                return credits;
            default:
                break;
        }
        return 0;
    }

    internal void DecreaseResourcesBasedOnRequirement(ObjectData objectData)
    {
        foreach (BuildRequirement req in objectData.requirements)
        {
            DecreaseResource(req.resource, req.amount);
        }
    }
    private void OnEnable()
    {
        OnResourceChanged += UpdateUI;
    }

    private void OnDestroy()
    {
        OnResourceChanged -= UpdateUI;
    }
}
