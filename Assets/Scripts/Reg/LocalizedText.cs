using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string localizationKey;
    public string fontProfileName = "Default";
    
    [Header("Material Presets")]
    public Material englishMaterial;
    public Material sinhalaMaterial;

    private TextMeshProUGUI textMesh;

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        UpdateUI();
    }

    void OnEnable() // Changed from Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (LocalizationManager.Instance == null || textMesh == null) return;

        // 1. Update the String
        textMesh.text = LocalizationManager.Instance.GetTranslation(localizationKey);

        textMesh.font = LocalizationManager.Instance.GetFont(fontProfileName);

        // 2. Update the Font and Material Preset
        if (LocalizationManager.Instance.currentLanguage == Language.Sinhala)
        {
            if (sinhalaMaterial != null) textMesh.fontSharedMaterial = sinhalaMaterial;
        }
        else
        {
            if (englishMaterial != null) textMesh.fontSharedMaterial = englishMaterial;
        }
    }
}