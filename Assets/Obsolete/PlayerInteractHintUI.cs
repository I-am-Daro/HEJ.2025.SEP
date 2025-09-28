using TMPro;
using UnityEngine;

public class PlayerInteractHintUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] Canvas worldCanvas;              // InteractHint/Canvas (World Space)
    [SerializeField] RectTransform bg;                // InteractHint/Canvas/BG
    [SerializeField] TextMeshProUGUI label;           // InteractHint/Canvas/Text (TMP)

    [Header("Layout")]
    [SerializeField] Vector3 worldOffset = new(0f, 1.05f, 0f);
    [SerializeField] Vector2 bgPaddingPx = new(16f, 8f);    // **PÍXELBEN** – egyszerűbb így

    Camera cam;
    float PPU => worldCanvas ? worldCanvas.referencePixelsPerUnit : 100f; // UI px → UI egység

    void Awake()
    {
        if (!worldCanvas) worldCanvas = GetComponentInChildren<Canvas>(true);
        cam = Camera.main;
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!gameObject.activeSelf) return;

        if (cam == null) cam = Camera.main;
        if (cam) transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        // a GO a HandAnchor gyereke → csak lokális offset kell
        transform.localPosition = worldOffset;

        // ---- háttér auto-méretezés helyes egységgel ----
        if (label && bg)
        {
            label.ForceMeshUpdate();
            Vector2 prefPx = label.GetPreferredValues(label.text, 0, 0); // **PX**
            Vector2 sizePx = prefPx + bgPaddingPx;                       // **PX**
            Vector2 sizeUi = sizePx / Mathf.Max(1f, PPU);                // **UI egység**
            bg.sizeDelta = sizeUi;
        }
    }

    public void Show(string text)
    {
        if (label) label.text = text ?? "";
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
