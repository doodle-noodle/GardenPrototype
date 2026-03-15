using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float     moveSpeed       = 5f;
    public Transform cameraTransform;

    private CharacterController cc;
    private Camera              mainCamera;

    void Awake()
    {
        cc         = GetComponent<CharacterController>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (ShopUI.IsOpen) return;

        HandlePlantingFeedback();
        HandleMovement();
    }

    // ── Click feedback ────────────────────────────────────────

    void HandlePlantingFeedback()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // Don't interfere while placement mode is active
        if (PlacementController.Instance != null && PlacementController.Instance.IsPlacing)
            return;

        // Don't fire if clicking on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return;

        bool hitFarmPlot = hit.collider.GetComponent<FarmPlot>() != null;
        bool hitGround   = hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground");

        if (hitGround && !hitFarmPlot && Inventory.Instance.SelectedSeed != null)
            TutorialConsole.Warn("Seeds can only be planted on a farm plot!");
    }

    // ── Movement ──────────────────────────────────────────────

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h == 0 && v == 0) return;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;
        camForward.y = 0; camForward.Normalize();
        camRight.y   = 0; camRight.Normalize();

        Vector3 move = (camForward * v + camRight * h).normalized;
        cc.Move(move * moveSpeed * Time.deltaTime);

        if (move != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(move), 10f * Time.deltaTime);
    }
}