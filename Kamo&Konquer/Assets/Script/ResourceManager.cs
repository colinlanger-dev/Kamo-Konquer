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

    private int glaube = 300;

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

    public int GetGlaube()
    {
        return glaube;
    }

    public void IncreaseResource(ResourcesType resource, int amountToIncrease)
    {
        switch (resource)
        {
            case ResourcesType.Glaube:
                glaube += amountToIncrease;
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
                glaube -= amountToDecrease;
                break;
            default:
                break;
        }

        OnResourceChanged?.Invoke();
    }


    internal int GetResourceAmount(ResourcesType resource)
    {
        switch (resource)
        {
            case ResourcesType.Glaube:
                return glaube;
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

    private void UpdateUI()
    {
        GlaubeUI.text = $"{glaube}";
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
