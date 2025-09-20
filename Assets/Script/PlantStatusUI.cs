using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantStatusUI : MonoBehaviour
{
    public static PlantStatusUI Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] GameObject root;               // panel gyökér (SetActive on/off)
    [SerializeField] TextMeshProUGUI titleText;     // növény neve
    [SerializeField] TextMeshProUGUI produceText;   // Produces: O2 / Food / Iron
    [SerializeField] TextMeshProUGUI wateredText;   // Watered today: Yes/No
    [SerializeField] TextMeshProUGUI stageText;     // <<< ÚJ: Stage: Seed/Sapling/Mature/Fruiting/Withered
    [SerializeField] Button destroyButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button harvestButton;          // csak Fruitingnál látszik

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

        // Cím
        if (titleText) titleText.text = def ? def.displayName : "Unknown plant";

        // Termék típus (Oxygen / Food / Iron)
        if (produceText)
        {
            if (!def)
            {
                produceText.text = "Produces: -";
            }
            else
            {
                string prod = def.produceType switch
                {
                    ProduceType.Oxygen => "O₂",
                    ProduceType.Food => "Food",
                    ProduceType.Iron => "Iron",
                    _ => "Unknown"
                };
                produceText.text = $"Produces: {prod}";
            }
        }

        // Napi locsolás
        if (wateredText)
            wateredText.text = plot.IsWateredToday()
                ? "Watered today: Yes"
                : "Watered today: No";

        // Fázis kiírás: közvetlenül megpróbáljuk kiolvasni a PlantActor-ból
        if (stageText)
        {
            var actor = plot.GetComponentInChildren<PlantActor>(true);
            string stageStr = actor ? PrettyStage(actor.stage) : "Unknown";
            stageText.text = $"Stage: {stageStr}";
        }

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

    // --- helpers ---
    string PrettyStage(PlantStage s)
    {
        return s switch
        {
            PlantStage.Seed => "Seed",
            PlantStage.Sapling => "Sapling",
            PlantStage.Mature => "Mature",
            PlantStage.Fruiting => "Fruiting",
            PlantStage.Withered => "Withered",
            _ => s.ToString()
        };
    }
}
