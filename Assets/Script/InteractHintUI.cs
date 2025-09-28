using UnityEngine;
using TMPro;

public class InteractHintUI : MonoBehaviour
{
    [SerializeField] RectTransform root;                 // a hint Image RectTransform (EZ AZ OBJEKTUM)
    [SerializeField] TextMeshProUGUI label;              // a TMP szöveg
    [SerializeField] Vector2 pixelOffset = new(0, 40);
    [SerializeField] Vector2 bgPadding = new(16, 8);

    [Header("Anchor Space (UI panel, ahol a játékkép van)")]
    [SerializeField] RectTransform anchorSpace;          // IDE TEDD: Canvas/Ship_area (NE a Canvas!)
    [SerializeField] bool reparentUnderAnchorSpace = true;
    [SerializeField] string anchorSpaceNameFallback = "Ship_area"; // ha elfelejted beállítani

    Canvas canvas;

    void Awake() { EnsureRefs(); Hide(); }
#if UNITY_EDITOR
    void OnValidate() { EnsureRefs(); }
#endif

    void EnsureRefs()
    {
        if (!root) root = GetComponent<RectTransform>();
        if (!canvas) canvas = GetComponentInParent<Canvas>(true);

        // ha nincs Anchor Space megadva, próbáljuk név alapján
        if (!anchorSpace && !string.IsNullOrEmpty(anchorSpaceNameFallback))
        {
            var go = GameObject.Find(anchorSpaceNameFallback);
            if (go) anchorSpace = go.GetComponent<RectTransform>();
        }

        // rögzített anchor/pivot
        if (root)
        {
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
        }

        // ha megvan az Anchor Space, tegyük gyereknek (így nem keveredik a koordinátarendszer)
        if (anchorSpace && reparentUnderAnchorSpace && root && root.parent != anchorSpace)
            root.SetParent(anchorSpace, worldPositionStays: false);
    }

    Camera ProjectionCam
    {
        get
        {
            // World→Screen mindig a renderelő főkamerával (viewport/letterbox miatt)
            var cm = Camera.main;
            if (!cm && canvas && canvas.worldCamera) cm = canvas.worldCamera;
            return cm;
        }
    }

    public void Show(Vector3 worldPos, string text)
    {
        EnsureRefs();
        if (!root) return;

        if (canvas && !canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);

        // szöveg + háttér auto-méret
        if (label)
        {
            label.text = text ?? string.Empty;
            label.ForceMeshUpdate();
            var pref = label.GetPreferredValues(label.text, 0, 0);
            root.sizeDelta = new Vector2(Mathf.Ceil(pref.x) + bgPadding.x,
                                         Mathf.Ceil(pref.y) + bgPadding.y);
        }

        // --- World -> Screen a főkamerával ---
        var projCam = ProjectionCam;
        Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(projCam, worldPos);

        // --- Screen -> ANCHOR SPACE local ---
        RectTransform space = anchorSpace
            ? anchorSpace
            : (root.parent as RectTransform) ?? (canvas.transform as RectTransform);

        Camera localCam = (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas ? canvas.worldCamera : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screenPt, localCam, out var local);
        root.anchoredPosition = local + pixelOffset;

        root.SetAsLastSibling();
    }

    public void Hide()
    {
        if (root) root.gameObject.SetActive(false);
    }
}
