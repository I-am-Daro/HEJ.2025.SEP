using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GravityBubble : MonoBehaviour
{
    public static readonly List<GravityBubble> All = new();

    Collider2D col;

    void Reset() { GetComponent<Collider2D>().isTrigger = true; }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
        All.Add(this);
    }
    void OnDestroy() { All.Remove(this); }

    public bool Contains(Vector2 worldPos)
    {
        return col && col.OverlapPoint(worldPos);
    }

    // Kényelmi statikus lekérdezés:
    public static bool AnyContains(Vector2 worldPos)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var b = All[i];
            if (b && b.Contains(worldPos)) return true;
        }
        return false;
    }
}
