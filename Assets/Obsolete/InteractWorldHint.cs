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
    [SerializeField] Vector2 bgPadding = new Vector2(0.2f, 0.1f);     // h�tt�r keret (vil�g egys�g)

    Transform anchor;      // hova r�gz�tj�k (Player/HandAnchor)
    Camera cam;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!canvas) canvas = GetComponentInChildren<Canvas>(true)?.GetComponent<RectTransform>();
        cam = Camera.main;

        // indul�skor rejtve
        if (canvas) canvas.gameObject.SetActive(false);
        gameObject.SetActive(true); // maga a GO �lhet, csak a Canvas legyen rejtve

        DontDestroyOnLoad(gameObject); // hogy scene v�lt�sn�l is megmaradjon
    }

    void LateUpdate()
    {
        if (!anchor || !canvas || !canvas.gameObject.activeSelf) return;

        // poz�ci�: mindig az anchor f�l�tt
        transform.position = anchor.position + worldOffset;

        // 2D: n�zzen el�re (a kamera fel�)
        if (cam) transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        // h�tt�r auto-m�ret a sz�veghez
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
