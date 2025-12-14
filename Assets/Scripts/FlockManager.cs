using UnityEngine;

public class FlockManager : MonoBehaviour
{
    // Simple singleton for global access
    public static FlockManager Instance;

    [Header("Spawn Settings")]
    public GameObject sheepPrefab; // Prefab to spawn
    public int sheepCount = 20;
    public Vector3 spawnArea = new Vector3(10, 0, 10); // Spawn radius around the manager

    [Header("Flock Settings")]
    [Range(0.0f, 10.0f)] public float minSpeed = 2.0f;
    [Range(0.0f, 10.0f)] public float maxSpeed = 5.0f;
    [Range(1.0f, 15.0f)] public float neighborDistance = 5.0f; // Detection range
    [Range(0.0f, 5.0f)] public float rotationSpeed = 2.5f;

    [Header("Target")]
    public Transform leader; // Follow target

    // Holds all spawned sheep for global access
    [HideInInspector] public GameObject[] allSheep;

    public Camera tpsCamera;
    public Camera freeCamera;
    public bool isFreeCam = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Allocate array
        allSheep = new GameObject[sheepCount];

        isFreeCam = false;
        tpsCamera.enabled = true;
        freeCamera.enabled = false;
        Cursor.lockState = CursorLockMode.Locked; // For smoother TPS rotation

        for (int i = 0; i < sheepCount; i++)
        {
            // Randomized spawn position around the manager
            Vector3 pos = this.transform.position + new Vector3(
                Random.Range(-spawnArea.x, spawnArea.x),
                0,
                Random.Range(-spawnArea.z, spawnArea.z)
            );

            // Spawn sheep and store reference
            allSheep[i] = Instantiate(sheepPrefab, pos, Quaternion.identity);

            // Pass manager reference to each sheep
            allSheep[i].GetComponent<SheepAgent>().manager = this;
        }
    }

    void Update()
    {
        // Switch camera on "C"
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCameras();
        }
    }

    void ToggleCameras()
    {
        isFreeCam = !isFreeCam;

        if (isFreeCam)
        {
            tpsCamera.enabled = false;
            freeCamera.enabled = true;
            Cursor.lockState = CursorLockMode.None; // Free look
        }
        else
        {
            freeCamera.enabled = false;
            tpsCamera.enabled = true;
            Cursor.lockState = CursorLockMode.Locked; // TPS mode
        }
    }
}
