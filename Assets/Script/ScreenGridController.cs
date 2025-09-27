using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class ScreenGridController : MonoBehaviour
{
    [Header("Rács")]
    [SerializeField] Vector2 gridOrigin = Vector2.zero;
    [SerializeField] float roomWidth = 16f;
    [SerializeField] float roomHeight = 9f;

    [Header("Kamera anchor (Cinemachine Follow ide mutat)")]
    [SerializeField] Transform cameraAnchor;
    [SerializeField] string cameraAnchorChildName = "CameraAnchor"; // ha hiányzik, így keressük
    [SerializeField] string cameraAnchorTag = "CameraAnchor";       // opcionális: tag alapján is keres

    [Header("Belépési puffer és stabilitás")]
    [SerializeField] float entryPadding = 0.5f;
    [SerializeField] float freezeTime = 0.05f;
    [SerializeField] float edgeEpsilon = 0.01f;

    Rigidbody2D rb;
    Vector2Int currentRoom;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ResolveCameraAnchor();
        RecomputeRoomAndSnap();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // biztos ami biztos: ha futás közben változtatod inspectorból a gridet/anchort
        if (!cameraAnchor) ResolveCameraAnchor();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        // Új scene → lehet más a gridOrigin/room méret, biztosan számoljunk újra
        ResolveCameraAnchor();
        RecomputeRoomAndSnap();
    }

    void RecomputeRoomAndSnap()
    {
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
#if UNITY_2022_3_OR_NEWER
        var oldVel = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
#else
        var oldVel = rb.velocity;
        rb.velocity = Vector2.zero;
#endif
        rb.isKinematic = true;
        yield return new WaitForSeconds(freezeTime);
        rb.isKinematic = false;
#if UNITY_2022_3_OR_NEWER
        rb.linearVelocity = oldVel;
#else
        rb.velocity = oldVel;
#endif
    }

    void SnapCameraToRoom(Vector2Int room)
    {
        if (!cameraAnchor) return;
        var c = RoomCenter(room);
        cameraAnchor.position = new Vector3(c.x, c.y, cameraAnchor.position.z);
    }

    // ---- Anchor felkutatása/létrehozása ----
    void ResolveCameraAnchor()
    {
        if (cameraAnchor && cameraAnchor.gameObject.scene.IsValid())
            return;

        // 1) próbáljuk megtalálni gyerekként név alapján
        if (!string.IsNullOrEmpty(cameraAnchorChildName))
        {
            var child = transform.Find(cameraAnchorChildName);
            if (child) { cameraAnchor = child; return; }
        }

        // 2) tag alapján bárhol a jelenetben
        if (!string.IsNullOrEmpty(cameraAnchorTag))
        {
            var tagged = GameObject.FindWithTag(cameraAnchorTag);
            if (tagged) { cameraAnchor = tagged.transform; return; }
        }

        // 3) ha nincs, hozzunk létre egy gyereket, hogy a Cinemachine is tudjon mire követni
        var go = new GameObject(string.IsNullOrEmpty(cameraAnchorChildName) ? "CameraAnchor" : cameraAnchorChildName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        cameraAnchor = go.transform;
    }

    // ---- Grid segédek ----
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

    // (ha szeretnéd kívülről is újrainicializálni scene betöltés után)
    public void ForceReinit() { ResolveCameraAnchor(); RecomputeRoomAndSnap(); }
}
