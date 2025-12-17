using System.Collections.Generic;
using UnityEngine;

public class FlockManagerTemporal : MonoBehaviour
{
    public static FlockManagerTemporal Instance;

    [Header("Spawn Settings")]
    public GameObject sheepPrefab; // Prefab must have SheepAgentTemporal script!
    public int initialSheepCount = 500;
    public Vector3 spawnArea = new Vector3(30, 0, 30);

    [Header("Flock Settings")]
    [Range(0.0f, 10.0f)] public float minSpeed = 2.0f;
    [Range(0.0f, 10.0f)] public float maxSpeed = 5.0f;
    [Range(1.0f, 15.0f)] public float neighborDistance = 5.0f;
    [Range(0.0f, 10.0f)] public float rotationSpeed = 5.0f;

    [Header("Temporal Settings (Optimization)")]
    [Tooltip("Memory refresh rate in seconds. E.g., 0.2s")]
    public float updateFrequency = 0.2f;

    [Header("References")]
    public Transform leader;
    public LayerMask sheepLayer; // Critical for Physics/BVH checks

    [Header("Camera Settings")]
    public Camera tpsCamera;
    public Camera freeCamera;
    public bool isFreeCam = false;

    [HideInInspector]
    public List<GameObject> allSheep = new List<GameObject>();

    void Awake() { Instance = this; }

    void Start()
    {
        // Initialize Cameras
        isFreeCam = false;
        if (tpsCamera != null) tpsCamera.enabled = true;
        if (freeCamera != null) freeCamera.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Spawn Sheep
        for (int i = 0; i < initialSheepCount; i++)
        {
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-spawnArea.x, spawnArea.x),
                0,
                Random.Range(-spawnArea.z, spawnArea.z)
            );

            GameObject newSheep = Instantiate(sheepPrefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0));

            SheepAgentTemporal agent = newSheep.GetComponent<SheepAgentTemporal>();
            if (agent != null)
            {
                agent.manager = this;
                allSheep.Add(newSheep);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) ToggleCameras();
    }

    void ToggleCameras()
    {
        isFreeCam = !isFreeCam;
        if (isFreeCam)
        {
            if (tpsCamera != null) tpsCamera.enabled = false;
            if (freeCamera != null) freeCamera.enabled = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            if (freeCamera != null) freeCamera.enabled = false;
            if (tpsCamera != null) tpsCamera.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}