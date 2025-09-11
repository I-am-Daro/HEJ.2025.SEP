using UnityEngine;

public static class TravelContext
{
    public static string returnScene;
    public static bool useWorldPosition;
    public static Vector3 returnWorldPos;
    public static string returnSpawnId;

    public static string interiorSpawnId = "FromExterior_GH";

    // ÚJ: aktuális üvegház azonosító (kinti Entrance StableId-ja)
    public static string currentGreenhouseId;
}
