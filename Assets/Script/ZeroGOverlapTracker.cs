using UnityEngine;

[DisallowMultipleComponent]
public class ZeroGOverlapTracker : MonoBehaviour
{
    [SerializeField] float recheckInterval = 0.15f;

    int bubbleCount = 0;
    float recheckTimer;

    PlayerStats stats;
    PlayerMovementService move;

    public bool IsZeroG => bubbleCount <= 0;   // <<< ezt használja majd a guard

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        move = GetComponent<PlayerMovementService>();
    }

    void Update()
    {
        recheckTimer -= Time.deltaTime;
        if (recheckTimer <= 0f)
        {
            recheckTimer = recheckInterval;

            int contained = 0;
            var pos = (Vector2)transform.position;
            foreach (var b in GravityBubble.All)
                if (b && b.Contains(pos)) contained++;

            if (contained != bubbleCount)
            {
                bubbleCount = contained;
                Apply();
            }
        }
    }

    public void EnterBubble() { bubbleCount++; Apply(); }
    public void ExitBubble() { bubbleCount = Mathf.Max(0, bubbleCount - 1); Apply(); }

    void Apply()
    {
        bool zeroG = IsZeroG;
        if (stats) stats.isZeroG = zeroG;
        if (move) move.Apply(zeroG ? MoveMode.ZeroG : MoveMode.ExteriorTopDown);
    }
}
