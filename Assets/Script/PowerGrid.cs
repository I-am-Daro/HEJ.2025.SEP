using System.Collections.Generic;
using UnityEngine;

public class PowerGrid : MonoBehaviour
{
    public static PowerGrid I { get; private set; }

    readonly List<PowerNode> nodes = new();
    readonly List<PowerConsumer> consumers = new();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Register(PowerNode n)
    {
        if (!nodes.Contains(n)) nodes.Add(n);
        Rebuild();
    }
    public void Unregister(PowerNode n)
    {
        nodes.Remove(n);
        Rebuild();
    }

    public void Register(PowerConsumer c)
    {
        if (!consumers.Contains(c)) consumers.Add(c);
        Rebuild();
    }
    public void Unregister(PowerConsumer c)
    {
        consumers.Remove(c);
        Rebuild();
    }

    public void Rebuild()
    {
        // 1) reset
        foreach (var n in nodes) n.connectedToSource = false;

        // 2) BFS forrásoktól
        Queue<PowerNode> q = new();
        foreach (var n in nodes) if (n.isSource) { n.connectedToSource = true; q.Enqueue(n); }

        while (q.Count > 0)
        {
            var a = q.Dequeue();
            // keressünk szomszédokat sugár alapján
            foreach (var b in nodes)
            {
                if (b.connectedToSource) continue;
                float r = Mathf.Max(a.linkRadius, b.linkRadius);
                if (Vector2.SqrMagnitude((Vector2)b.transform.position - (Vector2)a.transform.position) <= r * r * 4f) // kicsit megengedõbb
                {
                    // csak akkor terjedjen tovább, ha A vagy B conduit (csõ) vagy source
                    if (a.isSource || a.isConduit || b.isConduit)
                    {
                        b.connectedToSource = true;
                        q.Enqueue(b);
                    }
                }
            }
        }

        // 3) fogyasztók értesítése
        foreach (var c in consumers)
        {
            bool powered = false;
            if (c.node != null)
                powered = c.node.connectedToSource;
            c.SetPowered(powered);
        }
    }
}
