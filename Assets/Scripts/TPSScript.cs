using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Orbit Settings")]
    public float distance = 10.0f;  // How far we stay from the target
    public float height = 5.0f;     // How high the camera sits
    public float rotationSpeed = 5.0f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    void Start()
    {
        // Get initial rotation angles
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Rotate camera only when not in Free Cam mode (TPS mode)
        if (!FlockManager.Instance.isFreeCam)
        {
            // Read mouse input
            currentX += Input.GetAxis("Mouse X") * rotationSpeed;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;

            // Prevent flipping or going below ground
            currentY = Mathf.Clamp(currentY, 10, 80);
        }

        // 1. Compute desired rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // 2. Compute position behind and above the target
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + target.position;
        position.y += height; // Raise the camera a bit

        // 3. Apply rotation and position
        transform.rotation = rotation;
        transform.position = position;

        // Ensure camera looks slightly above the target’s center
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
