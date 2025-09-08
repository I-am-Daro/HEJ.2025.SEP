using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ScreenGridController : MonoBehaviour
{
    [Header("Rács")]
    [SerializeField] Vector2 gridOrigin = Vector2.zero;
    [SerializeField] float roomWidth = 16f;
    [SerializeField] float roomHeight = 9f;

    [Header("Kamera anchor (Cinemachine Follow ide mutat)")]
    [SerializeField] Transform cameraAnchor;

    [Header("Belépési puffer és stabilitás")]
    [SerializeField] float entryPadding = 0.5f;
    [SerializeField] float freezeTime = 0.05f;
    [SerializeField] float edgeEpsilon = 0.01f; // lebegőpontos védelem

    Rigidbody2D rb;
    Vector2Int currentRoom;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentRoom = WorldToRoomIndex(transform.position);
        SnapCameraToRoom(currentRoom);
    }

    void FixedUpdate()
    {
        Vector2 pos = transform.position;
        var min = RoomMin(currentRoom);
        var max = RoomMax(currentRoom);

        if (pos.x > max.x + edgeEpsilon)
        {
            var target = currentRoom + Vector2Int.right;
            var tmin = RoomMin(target);
            var tmax = RoomMax(target);
            // új szoba bal szélén jelenünk meg
            Vector2 newPos = new Vector2(
                tmin.x + entryPadding,
                Mathf.Clamp(pos.y, tmin.y + entryPadding, tmax.y - entryPadding)
            );
            MoveToRoom(target, newPos);
        }
        else if (pos.x < min.x - edgeEpsilon)
        {
            var target = currentRoom + Vector2Int.left;
            var tmin = RoomMin(target);
            var tmax = RoomMax(target);
            // új szoba jobb szélén jelenünk meg
            Vector2 newPos = new Vector2(
                tmax.x - entryPadding,
                Mathf.Clamp(pos.y, tmin.y + entryPadding, tmax.y - entryPadding)
            );
            MoveToRoom(target, newPos);
        }
        else if (pos.y > max.y + edgeEpsilon)
        {
            var target = currentRoom + Vector2Int.up;
            var tmin = RoomMin(target);
            var tmax = RoomMax(target);
            // új szoba alsó szélén jelenünk meg
            Vector2 newPos = new Vector2(
                Mathf.Clamp(pos.x, tmin.x + entryPadding, tmax.x - entryPadding),
                tmin.y + entryPadding
            );
            MoveToRoom(target, newPos);
        }
        else if (pos.y < min.y - edgeEpsilon)
        {
            var target = currentRoom + Vector2Int.down;
            var tmin = RoomMin(target);
            var tmax = RoomMax(target);
            // új szoba felső szélén jelenünk meg
            Vector2 newPos = new Vector2(
                Mathf.Clamp(pos.x, tmin.x + entryPadding, tmax.x - entryPadding),
                tmax.y - entryPadding
            );
            MoveToRoom(target, newPos);
        }
    }

    void MoveToRoom(Vector2Int targetRoom, Vector2 playerNewPos)
    {
        currentRoom = targetRoom;
        SnapCameraToRoom(currentRoom);
        transform.position = playerNewPos;
        if (freezeTime > 0f) StartCoroutine(FreezeBriefly());
    }

    System.Collections.IEnumerator FreezeBriefly()
    {
        var oldVel = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        yield return new WaitForSeconds(freezeTime);
        rb.isKinematic = false;
        rb.linearVelocity = oldVel;
    }

    void SnapCameraToRoom(Vector2Int room)
    {
        if (!cameraAnchor) return;
        var c = RoomCenter(room);
        cameraAnchor.position = new Vector3(c.x, c.y, cameraAnchor.position.z);
    }

    Vector2 RoomMin(Vector2Int r) => gridOrigin + new Vector2(r.x * roomWidth, r.y * roomHeight);
    Vector2 RoomMax(Vector2Int r) => RoomMin(r) + new Vector2(roomWidth, roomHeight);
    Vector2 RoomCenter(Vector2Int r) => RoomMin(r) + new Vector2(roomWidth * 0.5f, roomHeight * 0.5f);

    Vector2Int WorldToRoomIndex(Vector2 p)
    {
        Vector2 rel = p - gridOrigin;
        int rx = Mathf.FloorToInt(rel.x / roomWidth);
        int ry = Mathf.FloorToInt(rel.y / roomHeight);
        return new Vector2Int(rx, ry);
    }
}
