using UnityEngine;

public enum SceneMovementKind { Exterior, Interior }

/// Tedd egy �res GO-ra minden scene-ben, �s �ll�tsd be a Scene Kind-ot!
public class MovementMarker : MonoBehaviour
{
    [SerializeField] SceneMovementKind sceneKind = SceneMovementKind.Exterior;
    public SceneMovementKind Kind => sceneKind;
}
