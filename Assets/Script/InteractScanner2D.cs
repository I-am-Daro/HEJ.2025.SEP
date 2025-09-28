using UnityEngine;
using System.Text.RegularExpressions;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InteractScanner2D : MonoBehaviour
{
    [Header("Scan")]
    public float scanRadius = 2.0f;
    public LayerMask candidateMask = ~0;

    [Header("Behavior")]
    public bool callInteractOnKey = false;

    [Header("Anchor")]
    public Transform worldAnchor;             // <<< Húzd be ide a Player/HandAnchor-t
    public string fallbackAnchorChildName = "HandAnchor";
    public float verticalWorldOffset = 0.0f;  // ha HandAnchor-t használsz, ez lehet 0

    [Header("Key")]
    public KeyCode legacyKey = KeyCode.E;
#if ENABLE_INPUT_SYSTEM
    public Key inputKey = Key.E;
#endif

    Camera cam;
    InteractHintUI hint;
    IInteractable current; Transform currentTransform;
    readonly Collider2D[] results = new Collider2D[24];

    void Start()
    {
        cam = Camera.main ? Camera.main : FindFirstObjectByType<Camera>();

#if UNITY_2022_3_OR_NEWER
        hint = FindFirstObjectByType<InteractHintUI>(FindObjectsInactive.Include);
#else
        hint = FindObjectOfType<InteractHintUI>(true);
#endif

        if (!worldAnchor)
        {
            var t = transform.Find(fallbackAnchorChildName);
            if (t) worldAnchor = t;
        }
    }

    void Update()
    {
        var hit = FindNearestInteractable();
        if (hit.i != null)
        {
            current = hit.i; currentTransform = hit.t;

            if (hint)
            {
                Vector3 anchorPos = worldAnchor ? worldAnchor.position : transform.position;
                anchorPos += Vector3.up * verticalWorldOffset;
                hint.Show(anchorPos, BuildPrompt(current));
            }

            if (callInteractOnKey && IsInteractPressed())
            {
                var stats = GetComponent<PlayerStats>();
                current.Interact(stats);
            }
        }
        else
        {
            current = null; currentTransform = null;
            if (hint) hint.Hide();
        }
    }

    (IInteractable i, Transform t) FindNearestInteractable()
    {
        int n = Physics2D.OverlapCircleNonAlloc(transform.position, scanRadius, results, candidateMask);
        float best = float.PositiveInfinity; IInteractable bestI = null; Transform bestT = null;

        for (int k = 0; k < n; k++)
        {
            var col = results[k]; if (!col) continue;
            if (col.attachedRigidbody && col.attachedRigidbody.gameObject == gameObject) continue;
            if (col.GetComponentInParent<GhostMarker>() != null) continue;

            var mbs = col.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var mb in mbs)
            {
                if (mb is IInteractable ii)
                {
                    float d = (mb.transform.position - transform.position).sqrMagnitude;
                    if (d < best) { best = d; bestI = ii; bestT = mb.transform; }
                    break;
                }
            }
        }
        return (bestI, bestT);
    }

    string BuildPrompt(IInteractable i)
    {
#if ENABLE_INPUT_SYSTEM
        string key = inputKey.ToString().ToUpper();
#else
        string key = legacyKey.ToString().ToUpper();
#endif
        string basePrompt = i.GetPrompt() ?? "";

        // távolítsuk el az "(E)" / "[E]" ismétlést
        string pattern = $@"\s*(\(|\[)\s*{Regex.Escape(key)}\s*(\)|\])|\s+\b{Regex.Escape(key)}\b";
        string cleaned = Regex.Replace(basePrompt, pattern, "", RegexOptions.IgnoreCase).Trim();

        return $"[{key}] {cleaned}";
    }

    bool IsInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[inputKey].wasPressedThisFrame;
#else
        return Input.GetKeyDown(legacyKey);
#endif
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}
