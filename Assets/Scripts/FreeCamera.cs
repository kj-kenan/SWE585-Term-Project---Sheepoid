using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCameraMove : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 25, -15);
    public float smoothSpeed = 5f;

    [Header("Free Roam Settings")]
    public float movementSpeed = 20f;
    public float mouseSensitivity = 2f;

    private bool isManualControl = false;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        isManualControl = false;

        if (target != null)
        {
            // Align rotation to target immediately to prevent snapping
            transform.rotation = Quaternion.LookRotation(target.position - (target.position + offset));

            Vector3 currentEuler = transform.eulerAngles;
            rotationX = currentEuler.y;
            rotationY = currentEuler.x;
        }
    }

    void Update()
    {
        // FIX: If this camera is not the active view, do not process input
        if (!cam.enabled) return;

        // 1. Detect Input to switch to Manual Mode
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0 || Input.GetMouseButton(1))
        {
            isManualControl = true;
        }

        if (isManualControl)
        {
            // --- MANUAL MODE ---

            // Rotation (Right Mouse Button)
            if (Input.GetMouseButton(1))
            {
                rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
                rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                rotationY = Mathf.Clamp(rotationY, -90, 90);
                transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);
            }

            // Movement (WASD + Q/E)
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float y = 0;
            if (Input.GetKey(KeyCode.E)) y = 1; // Up
            if (Input.GetKey(KeyCode.Q)) y = -1; // Down

            Vector3 moveDir = transform.right * h + transform.forward * v + transform.up * y;
            transform.position += moveDir * movementSpeed * Time.deltaTime;
        }
        else
        {
            // --- FOLLOW MODE ---
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.LookAt(target);

            // Keep rotation variables updated to prevent snapping when switching to manual
            rotationX = transform.eulerAngles.y;
            rotationY = transform.eulerAngles.x;
        }
    }
}