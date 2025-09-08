using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Range(0, 100)] public float oxygen = 100f;
    [Range(0, 100)] public float energy = 100f;
    [Range(0, 100)] public float hunger = 0f;   // 0 jóllakott, 100 éhes, vagy fordítva – döntsd el

    // egyszerû példa fogyásra/költségre
    public float oxygenDrainPerSecExterior = 2f;
    public float oxygenDrainPerSecZeroG = 4f;

    public bool isZeroG; // MovementService-bõl is állíthatod
    void Update()
    {
        float drain = isZeroG ? oxygenDrainPerSecZeroG : oxygenDrainPerSecExterior;
        oxygen = Mathf.Clamp(oxygen - drain * Time.deltaTime, 0f, 100f);
    }
}
