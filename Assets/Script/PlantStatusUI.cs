using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantStatusUI : MonoBehaviour
{
    public static PlantStatusUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] GameObject root;          // panel gyökér (SetActive on/off)
    [SerializeField] TextMeshProUGUI titleText;     // növény neve
    [SerializeField] TextMeshProUGUI produceText;   // O2 / Food
    [SerializeField] TextMeshProUGUI wateredText;   // Watered today: Yes/No
    [SerializeField] Button destroyButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button harvestButton;      // opcionális, csak Fruitingnál látszik

    BedPlot boundPlot;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();

        if (closeButton) closeButton.onClick.AddListener(Hide);
        if (destroyButton) destroyButton.onClick.AddListener(OnDestroyClicked);
        if (harvestButton) harvestButton.onClick.AddListener(OnHarvestClicked);
    }

    public void ShowFor(BedPlot plot)
    {
        if (!plot || !plot.HasPlant)
        {
            Hide();
            return;
        }

        boundPlot = plot;
        var def = plot.CurrentDef;

        if (titleText) titleText.text = def ? def.displayName : "Unknown plant";
        if (produceText) produceText.text = def
            ? (def.produceType == ProduceType.Oxygen ? "Produces: O₂" : "Produces: Food")
            : "Produces: -";

        if (wateredText) wateredText.text = plot.IsWateredToday()
            ? "Watered today: Yes"
            : "Watered today: No";

        // Harvest gomb csak, ha termő állapotban van
        if (harvestButton) harvestButton.gameObject.SetActive(plot.IsFruiting);

        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
        boundPlot = null;
    }

    void OnDestroyClicked()
    {
        if (!boundPlot) { Hide(); return; }
        boundPlot.DestroyPlantFromUI();
        Hide();
    }

    void OnHarvestClicked()
    {
        if (!boundPlot) { Hide(); return; }
        boundPlot.HarvestFromUI();
        Hide();
    }
}
