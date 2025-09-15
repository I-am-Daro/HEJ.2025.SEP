using UnityEngine;

public enum SceneMovementKind { Exterior, Interior }

/// Tedd egy üres GO-ra minden scene-ben, és állítsd be a Scene Kind-ot!
public class MovementMarker : MonoBehaviour
{
    [SerializeField] SceneMovementKind sceneKind = SceneMovementKind.Exterior;
    public SceneMovementKind Kind => sceneKind;
}
