using System.Collections.Generic;
using UnityEngine;

public class FlockManagerGrid : MonoBehaviour
{
    public static FlockManagerGrid Instance;

    [Header("Spawn Settings")]
    // IMPORTANT: Make sure this prefab has 'SheepAgentGrid' script attached!
    public GameObject sheepPrefab;
    public int initialSheepCount = 500;
    public Vector3 spawnArea = new Vector3(30, 0, 30);

    [Header("Flock Settings")]
    [Range(0.0f, 10.0f)] public float minSpeed = 2.0f;
    [Range(0.0f, 10.0f)] public float maxSpeed = 5.0f;
    [Range(1.0f, 15.0f)] public float neighborDistance = 5.0f;
    [Range(0.0f, 10.0f)] public float rotationSpeed = 5.0f;

    [Header("Grid Settings")]
    public float gridSize = 5.0f; // Cell size (Should be >= neighborDistance)

    [Header("References")]
    public Transform leader;

    [Header("Camera Settings")]
    public Camera tpsCamera;
    public Camera freeCamera;
    public bool isFreeCam = false;

    // --- THE SPATIAL GRID ---
    // Mapping: Grid Coordinate (x,y) -> List of Sheep in that cell
    public Dictionary<Vector2Int, List<SheepAgentGrid>> spatialGrid = new Dictionary<Vector2Int, List<SheepAgentGrid>>();

    [HideInInspector]
    public List<SheepAgentGrid> allSheep = new List<SheepAgentGrid>();

    void Awake()
    {
        Instance = this;
    }

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

            SheepAgentGrid agent = newSheep.GetComponent<SheepAgentGrid>();
            if (agent != null)
            {
                agent.manager = this;
                allSheep.Add(agent);
            }
        }
    }

    void Update()
    {
        // 1. Update the Grid Data Structure
        UpdateSpatialGrid();

        // 2. Camera Input
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCameras();
        }
    }

    // This runs every frame to sort sheep into grid cells
    void UpdateSpatialGrid()
    {
        // 1. ADIM: Listeleri silme, sadece temizle (Clear)
        foreach (var list in spatialGrid.Values)
        {
            list.Clear();
        }

        // 2. ADIM: Koyunlarý yerleþtir
        for (int i = 0; i < allSheep.Count; i++)
        {
            SheepAgentGrid sheep = allSheep[i];

            Vector2Int coord = new Vector2Int(
                Mathf.FloorToInt(sheep.transform.position.x / gridSize),
                Mathf.FloorToInt(sheep.transform.position.z / gridSize)
            );

            // Eðer bu koordinatta liste YOKSA, yeni oluþtur (Sadece oyun baþýnda birkaç kez çalýþýr)
            if (!spatialGrid.ContainsKey(coord))
            {
                spatialGrid[coord] = new List<SheepAgentGrid>(50); // Kapasiteyi baþtan ver (50)
            }

            // Listeye ekle (Hafýza tahsisi yapmaz, çünkü yer var)
            spatialGrid[coord].Add(sheep);
        }
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
    // FlockManagerGrid.cs'nin en altýna, son parantezden } önce ekle:

    void OnDrawGizmos()
    {
        // Grid ayarlarý yoksa çizme
        if (spatialGrid == null || gridSize <= 0) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f); // Yeþil renk

        // Dolu olan her kutuyu çiz
        foreach (var kvp in spatialGrid)
        {
            Vector2Int coord = kvp.Key;
            int sheepCount = kvp.Value.Count;

            if (sheepCount > 0)
            {
                // Kutunun dünya pozisyonunu bul
                Vector3 center = new Vector3(
                    (coord.x * gridSize) + (gridSize / 2),
                    2f, // Yerden biraz yukarýda
                    (coord.y * gridSize) + (gridSize / 2)
                );

                // Kutuyu çiz
                Gizmos.DrawWireCube(center, new Vector3(gridSize, 2f, gridSize));

                // Ýsteðe baðlý: Yoðunluða göre renk deðiþtir (Kýrmýzý = Kalabalýk)
                float density = Mathf.Clamp01(sheepCount / 10f);
                Gizmos.color = Color.Lerp(Color.green, Color.red, density);
                Gizmos.DrawCube(center, new Vector3(gridSize * 0.9f, 0.5f, gridSize * 0.9f));
            }
        }
    }
}