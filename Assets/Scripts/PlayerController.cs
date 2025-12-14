using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 10f;
    public Transform cameraTransform;

    private Rigidbody rb;
    private Vector2 inputVector;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Block input if Free Cam is active
        if (FlockManager.Instance != null && FlockManager.Instance.isFreeCam)
        {
            inputVector = Vector2.zero;
            return;
        }

        inputVector = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    void FixedUpdate()
    {
        if (cameraTransform == null) return;

        // 1. Rotation - Always face camera direction
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;

        if (cameraForward.sqrMagnitude > 0.01f)
        {
            rb.MoveRotation(Quaternion.LookRotation(cameraForward));
        }

        // 2. Movement - Relative to camera
        Vector3 moveDir = (transform.right * inputVector.x) + (transform.forward * inputVector.y);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Vector3 targetVelocity = moveDir.normalized * moveSpeed;
            rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
        }
        else
        {
            // Stop horizontal movement
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }
}