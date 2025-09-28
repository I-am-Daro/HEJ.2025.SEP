using UnityEngine;
using TMPro;

public class InteractWorldHint : MonoBehaviour
{
    public static InteractWorldHint I { get; private set; }

    [Header("Wiring (use your existing names)")]
    [SerializeField] RectTransform canvas;       // InteractHintWS/Canvas (RectTransform)
    [SerializeField] RectTransform bg;           // InteractHintWS/Canvas/Image (RectTransform)
    [SerializeField] TextMeshProUGUI label;      // InteractHintWS/Canvas/UGUILabel (TMP)

    [Header("Layout (World Space)")]
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 1.1f, 0f); // mennyivel legyen a fejed felett
    [SerializeField] Vector2 bgPadding = new Vector2(0.2f, 0.1f);     // háttér keret (világ egység)

    Transform anchor;      // hova rögzítjük (Player/HandAnchor)
    Camera cam;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!canvas) canvas = GetComponentInChildren<Canvas>(true)?.GetComponent<RectTransform>();
        cam = Camera.main;

        // induláskor rejtve
        if (canvas) canvas.gameObject.SetActive(false);
        gameObject.SetActive(true); // maga a GO élhet, csak a Canvas legyen rejtve

        DontDestroyOnLoad(gameObject); // hogy scene váltásnál is megmaradjon
    }

    void LateUpdate()
    {
        if (!anchor || !canvas || !canvas.gameObject.activeSelf) return;

        // pozíció: mindig az anchor fölött
        transform.position = anchor.position + worldOffset;

        // 2D: nézzen elõre (a kamera felé)
        if (cam) transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        // háttér auto-méret a szöveghez
        if (label && bg)
        {
            label.ForceMeshUpdate();
            var pref = label.GetPreferredValues(label.text, 0, 0);
            bg.sizeDelta = new Vector2(pref.x, pref.y) + bgPadding;
        }
    }

    public void Show(Transform worldAnchor, string text)
    {
        anchor = worldAnchor;
        if (label) label.text = text ?? "";
        if (canvas) canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvas) canvas.gameObject.SetActive(false);
        anchor = null;
    }
}
