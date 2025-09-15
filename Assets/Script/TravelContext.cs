using UnityEngine;

public static class TravelContext
{
    public static string returnScene;
    public static bool useWorldPosition;
    public static Vector3 returnWorldPos;
    public static string returnSpawnId;

    public static string interiorSpawnId = "FromExterior_GH";

    // melyik üvegházba léptünk (Entrance StableId)
    public static string currentGreenhouseId;
}
