using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class MapMaker : EditorWindow
{
    // Spawn edilecek prefablar
    private List<GameObject> prefabs = new List<GameObject>();

    // Spawn alaný ayarlarý
    private Vector3 spawnCenter = Vector3.zero;
    private Vector2 spawnAreaSize = new Vector2(50f, 50f);

    // Spawn yoðunluðu
    private int spawnCount = 100;

    // Rastgele rotasyon
    private bool randomRotation = true;
    private Vector3 minRotation = new Vector3(0, 0, 0);
    private Vector3 maxRotation = new Vector3(0, 360, 0);

    // Rastgele ölçek
    private bool randomScale = false;
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    // Yüzey yerleþtirme
    private bool placeOnSurface = true;
    private LayerMask groundLayer = -1;
    private float raycastHeight = 500f;
    private bool usePhysicsDrop = false;
    private int maxRaycastAttempts = 5;
    private float groundOffset = 0.1f;

    // Spawn edilen objeleri sakla
    private GameObject parentObject;

    private Vector2 scrollPos;

    [MenuItem("Tools/Map Maker")]
    public static void ShowWindow()
    {
        GetWindow<MapMaker>("Map Maker");
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Map Maker Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Prefab listesi
        GUILayout.Label("Prefabs", EditorStyles.boldLabel);

        int newCount = Mathf.Max(0, EditorGUILayout.IntField("Prefab Sayýsý", prefabs.Count));
        while (newCount < prefabs.Count)
            prefabs.RemoveAt(prefabs.Count - 1);
        while (newCount > prefabs.Count)
            prefabs.Add(null);

        for (int i = 0; i < prefabs.Count; i++)
        {
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"Prefab {i + 1}", prefabs[i], typeof(GameObject), false);
        }

        EditorGUILayout.Space();

        // Spawn alaný ayarlarý
        GUILayout.Label("Spawn Alaný", EditorStyles.boldLabel);
        spawnCenter = EditorGUILayout.Vector3Field("Merkez Nokta", spawnCenter);
        spawnAreaSize = EditorGUILayout.Vector2Field("Alan Boyutu (X, Z)", spawnAreaSize);
        spawnCount = EditorGUILayout.IntSlider("Spawn Sayýsý", spawnCount, 1, 1000);

        EditorGUILayout.Space();

        // Rotasyon ayarlarý
        GUILayout.Label("Rotasyon Ayarlarý", EditorStyles.boldLabel);
        randomRotation = EditorGUILayout.Toggle("Rastgele Rotasyon", randomRotation);
        if (randomRotation)
        {
            minRotation = EditorGUILayout.Vector3Field("Min Rotasyon", minRotation);
            maxRotation = EditorGUILayout.Vector3Field("Max Rotasyon", maxRotation);
        }

        EditorGUILayout.Space();

        // Ölçek ayarlarý
        GUILayout.Label("Ölçek Ayarlarý", EditorStyles.boldLabel);
        randomScale = EditorGUILayout.Toggle("Rastgele Ölçek", randomScale);
        if (randomScale)
        {
            scaleRange = EditorGUILayout.Vector2Field("Ölçek Aralýðý (Min, Max)", scaleRange);
        }

        EditorGUILayout.Space();

        // Yüzey yerleþtirme
        GUILayout.Label("Yüzey Ayarlarý", EditorStyles.boldLabel);
        placeOnSurface = EditorGUILayout.Toggle("Yüzeye Yerleþtir", placeOnSurface);
        if (placeOnSurface)
        {
            groundLayer = EditorGUILayout.LayerField("Zemin Layer", groundLayer);
            raycastHeight = EditorGUILayout.FloatField("Raycast Yüksekliði", raycastHeight);
            maxRaycastAttempts = EditorGUILayout.IntSlider("Raycast Deneme Sayýsý", maxRaycastAttempts, 1, 20);
            usePhysicsDrop = EditorGUILayout.Toggle("Fizikle Düþür (Play Mode)", usePhysicsDrop);

            if (usePhysicsDrop)
            {
                EditorGUILayout.HelpBox("Play modunda fiziðe göre düþecek. Prefablarda Rigidbody ve Collider olmalý!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Ýlk çarpan yüzeye yerleþtirir. Alt katmanlarý atlar!", MessageType.Info);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Spawn butonu
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("SPAWN", GUILayout.Height(40)))
        {
            SpawnObjects();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // Temizleme butonu
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Tüm Spawn Edilenleri Sil", GUILayout.Height(30)))
        {
            ClearSpawnedObjects();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // Görselleþtirme butonu
        if (GUILayout.Button("Spawn Alanýný Göster/Gizle"))
        {
            SceneView.RepaintAll();
        }

        EditorGUILayout.EndScrollView();
    }

    void SpawnObjects()
    {
        if (prefabs.Count == 0 || prefabs.TrueForAll(p => p == null))
        {
            EditorUtility.DisplayDialog("Hata", "En az bir prefab seçmelisiniz!", "Tamam");
            return;
        }

        // Parent obje oluþtur
        if (parentObject == null)
        {
            parentObject = new GameObject("Spawned Objects");
            Undo.RegisterCreatedObjectUndo(parentObject, "Create Parent Object");
        }

        int successCount = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            // Rastgele prefab seç
            GameObject prefab = GetRandomPrefab();
            if (prefab == null) continue;

            // Rastgele pozisyon
            Vector3 randomPos = GetRandomPosition();

            // Yüzeye yerleþtir
            if (placeOnSurface)
            {
                RaycastHit hit;
                if (Physics.Raycast(randomPos + Vector3.up * 100f, Vector3.down, out hit, 200f, groundLayer))
                {
                    randomPos = hit.point;
                }
            }

            // Objeyi spawn et
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            obj.transform.position = randomPos;
            obj.transform.parent = parentObject.transform;

            // Rastgele rotasyon
            if (randomRotation)
            {
                Vector3 rotation = new Vector3(
                    Random.Range(minRotation.x, maxRotation.x),
                    Random.Range(minRotation.y, maxRotation.y),
                    Random.Range(minRotation.z, maxRotation.z)
                );
                obj.transform.rotation = Quaternion.Euler(rotation);
            }

            // Rastgele ölçek
            if (randomScale)
            {
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                obj.transform.localScale = Vector3.one * scale;
            }

            Undo.RegisterCreatedObjectUndo(obj, "Spawn Object");
            successCount++;
        }

        EditorUtility.DisplayDialog("Baþarýlý", $"{successCount} obje spawn edildi!", "Tamam");
    }

    GameObject GetRandomPrefab()
    {
        List<GameObject> validPrefabs = prefabs.FindAll(p => p != null);
        if (validPrefabs.Count == 0) return null;
        return validPrefabs[Random.Range(0, validPrefabs.Count)];
    }

    Vector3 GetRandomPosition()
    {
        float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float z = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        return spawnCenter + new Vector3(x, 0, z);
    }

    void ClearSpawnedObjects()
    {
        if (parentObject != null)
        {
            Undo.DestroyObjectImmediate(parentObject);
            parentObject = null;
            EditorUtility.DisplayDialog("Temizlendi", "Tüm spawn edilen objeler silindi!", "Tamam");
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        // Spawn alanýný çiz
        Handles.color = Color.cyan;
        Vector3 corner1 = spawnCenter + new Vector3(-spawnAreaSize.x / 2f, 0, -spawnAreaSize.y / 2f);
        Vector3 corner2 = spawnCenter + new Vector3(spawnAreaSize.x / 2f, 0, -spawnAreaSize.y / 2f);
        Vector3 corner3 = spawnCenter + new Vector3(spawnAreaSize.x / 2f, 0, spawnAreaSize.y / 2f);
        Vector3 corner4 = spawnCenter + new Vector3(-spawnAreaSize.x / 2f, 0, spawnAreaSize.y / 2f);

        Handles.DrawLine(corner1, corner2);
        Handles.DrawLine(corner2, corner3);
        Handles.DrawLine(corner3, corner4);
        Handles.DrawLine(corner4, corner1);
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}