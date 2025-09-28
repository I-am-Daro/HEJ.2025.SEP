using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractHintUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] RectTransform root;          // ← InteractableHint_Image (SAJÁT RT!)
    [SerializeField] TextMeshProUGUI label;       // ← InteractableHint_text
    [SerializeField] Canvas canvas;               // ← HUD Canvas (auto-find ok)

    [Header("Layout")]
    [SerializeField] Vector2 pixelOffset = Vector2.zero;
    [SerializeField] Vector2 bgPaddingPx = new(16f, 8f); // ha a BG Image méretét állítod

    Image bg;                                     // opcionális: ha a rooton van Image

    void Awake()
    {
        if (!root) root = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>(true);
        bg = GetComponent<Image>();
        Hide();
    }

    /// LÁTHATÓ + a helyes World→Screen konverzió KAMERÁVAL
    public void Show(Vector3 worldPos, string text)
    {
        if (!root) root = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>(true);

        // garantáltan látszódjon
        if (canvas && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);

        if (label) label.text = text ?? string.Empty;

        // --- LÉNYEG: World→Screen a VALÓDI proj. kamerával (letterbox miatt) ---
        Camera projCam = Camera.main;
        if (!projCam && canvas && canvas.worldCamera) projCam = canvas.worldCamera;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(projCam, worldPos);

        // Overlayben a local konverzió kamerája null; Camera/WorldSpace-ben a Canvas kamerája
        Camera localCam = (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                            ? null
                            : (canvas ? canvas.worldCamera : null);

        var canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, localCam, out var local);

        root.anchoredPosition = local + pixelOffset;
        root.SetAsLastSibling();

        // --- Opcionális: BG méretezése a szöveghez ---
        if (label && bg)
        {
            label.ForceMeshUpdate();
            Vector2 pref = label.GetPreferredValues(label.text, 0, 0); // px
            root.sizeDelta = pref + bgPaddingPx;                       // px
        }
    }

    public void Hide()
    {
        if (root) root.gameObject.SetActive(false);
    }
}
