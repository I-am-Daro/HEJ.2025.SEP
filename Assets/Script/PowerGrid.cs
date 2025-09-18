using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PowerGrid : MonoBehaviour
{
    public static PowerGrid I { get; private set; }

    readonly List<PowerNode> nodes = new();
    readonly List<PowerConsumer> consumers = new();

    [SerializeField] bool debugLog = false;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // >>> COLD SCAN <<<  — ha a Node-ok előbb enable-ődtek, itt felvesszük őket
        ColdScan();
        Rebuild();

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    void OnDestroy()
    {
        if (I == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            I = null;
        }
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        ColdScan();   // új scene → keressünk minden aktuális Node-ot/Consumert
        Rebuild();
    }

    void OnSceneUnloaded(Scene s)
    {
        ColdScan();   // biztonság kedvéért
        Rebuild();
    }

    // --- COLD SCAN: beolvassa a jelenetben lévő aktív példányokat ---
    void ColdScan()
    {
        nodes.Clear();
        consumers.Clear();

        var foundNodes = FindObjectsByType<PowerNode>(FindObjectsSortMode.None);
        foreach (var n in foundNodes)
            if (n && n.isActiveAndEnabled && !nodes.Contains(n))
                nodes.Add(n);

        var foundConsumers = FindObjectsByType<PowerConsumer>(FindObjectsSortMode.None);
        foreach (var c in foundConsumers)
            if (c && c.isActiveAndEnabled && !consumers.Contains(c))
                consumers.Add(c);

       
    }

    public void Register(PowerNode n)
    {
        if (n && !nodes.Contains(n)) nodes.Add(n);
        Rebuild();
    }
    public void Unregister(PowerNode n)
    {
        if (n) nodes.Remove(n);
        Rebuild();
    }

    public void Register(PowerConsumer c)
    {
        if (c && !consumers.Contains(c)) consumers.Add(c);
        Rebuild();
    }
    public void Unregister(PowerConsumer c)
    {
        if (c) consumers.Remove(c);
        Rebuild();
    }

    public void Rebuild()
    {
        // 0) TAKARÍTÁS – törölt referenciák eltávolítása (különösen ghost destroy után fontos)
        nodes.RemoveAll(x => x == null);
        consumers.RemoveAll(x => x == null);

        // 1) reset
        foreach (var n in nodes) n.connectedToSource = false;

        // 2) BFS a forrásoktól
        var q = new Queue<PowerNode>();
        foreach (var n in nodes)
        {
            if (n != null && n.isSource)
            {
                n.connectedToSource = true;
                q.Enqueue(n);
            }
        }

        while (q.Count > 0)
        {
            var a = q.Dequeue();
            if (a == null) continue;

            foreach (var b in nodes)
            {
                if (b == null || b == a || b.connectedToSource) continue;

                float need = a.linkRadius + b.linkRadius;
                float sqr = ((Vector2)b.transform.position - (Vector2)a.transform.position).sqrMagnitude;
                if (sqr <= need * need)
                {
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
            if (c == null) continue;
            bool powered = c.node && c.node.connectedToSource;
            c.SetPowered(powered);
        }
    }
}
