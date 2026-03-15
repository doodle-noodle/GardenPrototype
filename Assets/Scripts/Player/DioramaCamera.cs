using UnityEngine;

public class DioramaCamera : MonoBehaviour
{
    [Header("Angle")]
    public float pitch       = 45f;
    public float yaw         = 45f;

    [Header("Zoom")]
    public float zoomHeight  = 12f;
    public float minZoom     = 4f;
    public float maxZoom     = 30f;
    public float zoomSpeed   = 4f;
    public float zoomSmooth  = 8f;

    [Header("Pan")]
    public float keyPanSpeed = 8f;
    public float panSmooth   = 10f;

    [Header("Rotation — hold right mouse button")]
    public float rotateSpeed = 240f;

    [Header("Bounds")]
    public Vector2 panMin    = new Vector2(-2f,  -2f);
    public Vector2 panMax    = new Vector2(14f,  14f);

    private Vector3 targetLookAt;
    private float   targetZoom;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        // Read everything from the Transform as-is — don't move the camera
        pitch      = transform.eulerAngles.x;
        yaw        = transform.eulerAngles.y;
        targetZoom = zoomHeight;

        // Derive the look-at point from where the camera is already pointing
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetLookAt = hit.point;
        }
        else
        {
            // If raycast misses, project forward until y=0
            if (transform.forward.y < 0f)
            {
                float t  = -transform.position.y / transform.forward.y;
                targetLookAt = transform.position + transform.forward * t;
            }
            else
            {
                // Fallback to grid center
                targetLookAt = GetGridCenter();
            }
        }

        targetLookAt.y = 0f;
    }

    // ── Update ────────────────────────────────────────────────

    void Update()
    {
        if (ShopUI.IsOpen) return;

        HandleZoom();
        HandleKeyPan();
        HandleRotation();
        UpdateCameraPosition(snap: false);
    }

    // ── Zoom ──────────────────────────────────────────────────

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        targetZoom   = Mathf.Clamp(targetZoom - scroll * zoomSpeed * 10f, minZoom, maxZoom);
        zoomHeight   = Mathf.Lerp(zoomHeight, targetZoom, zoomSmooth * Time.deltaTime);
    }

    // ── WASD pan ──────────────────────────────────────────────

    void HandleKeyPan()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h == 0 && v == 0) return;

        Vector3 right   = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;

        targetLookAt += (right * h + forward * v).normalized * keyPanSpeed * Time.deltaTime;
        ClampLookAt();
    }

    // ── Right mouse hold rotation ─────────────────────────────

    void HandleRotation()
    {
        if (!Input.GetMouseButton(1)) return;

        float delta = Input.GetAxis("Mouse X");
        yaw += delta * rotateSpeed * Time.deltaTime;
    }

    // ── Apply position ────────────────────────────────────────

    void UpdateCameraPosition(bool snap)
    {
        Quaternion rotation   = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    offset     = rotation * new Vector3(0f, 0f, -zoomHeight);
        Vector3    desiredPos = targetLookAt + offset;

        if (snap)
            transform.position = desiredPos;
        else
            transform.position = Vector3.Lerp(
                transform.position, desiredPos, panSmooth * Time.deltaTime);

        transform.LookAt(targetLookAt + Vector3.up * 0.5f);
    }

    // ── Helpers ───────────────────────────────────────────────

    void ClampLookAt()
    {
        targetLookAt.x = Mathf.Clamp(targetLookAt.x, panMin.x, panMax.x);
        targetLookAt.z = Mathf.Clamp(targetLookAt.z, panMin.y, panMax.y);
        targetLookAt.y = 0f;
    }

    Vector3 GetGridCenter()
    {
        if (GridManager.Instance != null)
        {
            int   w = GridManager.Instance.gridWidth;
            int   h = GridManager.Instance.gridHeight;
            float c = GridManager.Instance.cellSize;
            return new Vector3(w * c * 0.5f, 0f, h * c * 0.5f);
        }
        return Vector3.zero;
    }

    // ── Editor gizmo ─────────────────────────────────────────

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireCube(
            new Vector3((panMin.x + panMax.x) * 0.5f, 0f, (panMin.y + panMax.y) * 0.5f),
            new Vector3(panMax.x - panMin.x, 0.1f, panMax.y - panMin.y));
    }
}