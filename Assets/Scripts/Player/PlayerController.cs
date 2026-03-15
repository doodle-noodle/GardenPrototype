using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform cameraTransform;

    private CharacterController cc;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        if (ShopUI.IsOpen) return;

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