using System.Collections.Generic;
using UnityEngine;

public class FlockManagerBVH : MonoBehaviour
{
    // Simple singleton access
    public static FlockManagerBVH Instance;

    [Header("Spawn Settings")]
    // Make sure this prefab has the SheepAgentBVH script
    public GameObject sheepPrefab;
    public int initialSheepCount = 500;
    public Vector3 spawnArea = new Vector3(30, 0, 30);

    [Header("Flock Settings")]
    [Range(0.0f, 10.0f)] public float minSpeed = 2.0f;
    [Range(0.0f, 10.0f)] public float maxSpeed = 5.0f;
    [Range(1.0f, 15.0f)] public float neighborDistance = 5.0f;
    [Range(0.0f, 10.0f)] public float rotationSpeed = 5.0f;

    [Header("References")]
    public Transform leader;

    // Used for BVH-based neighbor queries
    public LayerMask sheepLayer;

    [Header("Camera Settings")]
    public Camera tpsCamera;
    public Camera freeCamera;
    public bool isFreeCam = false;

    [HideInInspector]
    public List<GameObject> allSheep = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Camera init
        isFreeCam = false;
        if (tpsCamera != null) tpsCamera.enabled = true;
        if (freeCamera != null) freeCamera.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Spawn flock
        for (int i = 0; i < initialSheepCount; i++)
        {
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-spawnArea.x, spawnArea.x),
                0,
                Random.Range(-spawnArea.z, spawnArea.z)
            );

            GameObject newSheep = Instantiate(
                sheepPrefab,
                pos,
                Quaternion.Euler(0, Random.Range(0, 360), 0)
            );

            SheepAgentBVH agent = newSheep.GetComponent<SheepAgentBVH>();
            if (agent != null)
            {
                agent.manager = this;
                allSheep.Add(newSheep);
            }
            else
            {
                Debug.LogError("Prefab is missing SheepAgentBVH component.");
            }
        }
    }

    void Update()
    {
        // Switch cameras
        if (Input.GetKeyDown(KeyCode.C))
            ToggleCameras();
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
