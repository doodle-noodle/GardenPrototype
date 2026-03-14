using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float targetHeightOffset = 1.5f;

    [Header("Rotation — hold right mouse button to rotate")]
    public float mouseSensitivity = 3f;
    public float verticalMinAngle = 10f;
    public float verticalMaxAngle = 70f;

    [Header("Distance")]
    public float distance    = 8f;
    public float minDistance = 3f;
    public float maxDistance = 16f;
    public float zoomSpeed   = 4f;

    [Header("Collision")]
    public LayerMask collisionLayers;

    private float yaw   = 0f;
    private float pitch = 40f;

    void Start()
    {
        if (target == null) { Debug.LogError("ThirdPersonCamera: no target assigned!"); return; }
        yaw   = target.eulerAngles.y;
        pitch = 40f;

        // Cursor always visible — never lock it
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Only rotate camera while right mouse button is held
        if (Input.GetMouseButton(1))
        {
            yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch  = Mathf.Clamp(pitch, verticalMinAngle, verticalMaxAngle);
        }

        // Scroll to zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

        // Orbit around a point at head height
        Vector3 pivotPoint      = target.position + Vector3.up * targetHeightOffset;
        Quaternion rotation     = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredOffset   = rotation * new Vector3(0f, 0f, -distance);
        Vector3 desiredPosition = pivotPoint + desiredOffset;

        // Push camera forward if it clips into geometry
        float finalDistance = distance;
        if (Physics.SphereCast(pivotPoint, 0.3f,
            (desiredPosition - pivotPoint).normalized,
            out RaycastHit hit, distance, collisionLayers))
        {
            finalDistance = Mathf.Clamp(hit.distance - 0.1f, minDistance, distance);
        }

        transform.position = pivotPoint + rotation * new Vector3(0f, 0f, -finalDistance);
        transform.LookAt(pivotPoint);
    }
}