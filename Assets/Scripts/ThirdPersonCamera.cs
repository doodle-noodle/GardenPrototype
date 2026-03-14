using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // drag your player here

    [Header("Orbit")]
    public float distance    = 8f;
    public float heightAngle = 45f;   // vertical tilt (Animal Crossing is steep)
    public float rotateSpeed = 120f;

    [Header("Zoom")]
    public float minDistance = 4f;
    public float maxDistance = 16f;
    public float zoomSpeed   = 4f;

    private float currentYaw = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        // Rotate with Q / E  (or Left/Right arrows while holding Alt)
        float rotInput = 0f;
        if (Input.GetKey(KeyCode.E)) rotInput =  1f;
        if (Input.GetKey(KeyCode.Q)) rotInput = -1f;
        currentYaw += rotInput * rotateSpeed * Time.deltaTime;

        // Zoom with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

        // Compute camera position
        Quaternion rotation = Quaternion.Euler(heightAngle, currentYaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}