using UnityEngine;

public class ExteriorBuildSpawner : MonoBehaviour
{
    void Start()
    {
        if (GameData.I == null) return;

        foreach (var pb in GameData.I.exteriorBuildings)
        {
            // NÉV: BuildingDef (nem DefById)
            var def = GameData.I.BuildingDef(pb.defId);
            if (def == null || !def.prefab) continue;

            var go = Instantiate(def.prefab, pb.pos, Quaternion.Euler(0, 0, pb.rotZ));
            go.name = def.displayName;

            // Stabil ID visszaállítása
            var sid = go.GetComponent<StableId>();
            if (!sid) sid = go.AddComponent<StableId>();
            sid.SetIdForRestore(pb.id);
        }

        // Ha kell, a háló újraépítése
        PowerGrid.I?.Rebuild();
    }
}
