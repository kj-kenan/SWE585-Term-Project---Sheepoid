using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class BoidPerformanceMetrics : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private string algorithmName = "Base"; // Base, BVH, Temporal, Grid
    [SerializeField] private int sheepCount = 100;
    [SerializeField] private float testDuration = 23f; // 3s warmup + 20s recording
    [SerializeField] private float warmupDuration = 3f;

    [Header("Metrics Display")]
    [SerializeField] private bool showDebugUI = true;

    // Performance Metrics
    private List<float> fpsHistory = new List<float>();
    private List<float> frameTimeHistory = new List<float>();
    private List<long> gcAllocHistory = new List<long>();
    private List<float> scriptsTimeHistory = new List<float>();

    // FixedUpdate tracking
    private int fixedUpdateCount = 0;

    // References (auto-detected)
    private GameObject[] allSheep;
    private bool sheepFound = false;

    // Timing
    private float testStartTime;
    private float currentTestTime;
    private bool isRecording = false;
    private bool testCompleted = false;

    // Real-time values
    private float currentFPS;
    private float currentFrameTime;
    private float currentScriptsTime;
    private long lastGCAlloc;

    void Start()
    {
        testStartTime = Time.time;
        Debug.Log($"[Metrics] Test started: {algorithmName}");
        Debug.Log($"[Metrics] Waiting for sheep to spawn...");
    }

    void Update()
    {
        // Try to find sheep if not found yet
        if (!sheepFound)
        {
            TryFindSheep();
            if (!sheepFound) return;
        }

        currentTestTime = Time.time - testStartTime;

        // Start recording after warmup
        if (currentTestTime >= warmupDuration && !isRecording)
        {
            isRecording = true;
            Debug.Log("[Metrics] Warmup complete, recording started!");
        }

        // Calculate FPS
        currentFPS = 1f / Time.deltaTime;
        currentFrameTime = Time.deltaTime * 1000f;

        // Measure GC allocations
        long currentGCAlloc = Profiler.GetTotalAllocatedMemoryLong();
        long gcAllocThisFrame = currentGCAlloc - lastGCAlloc;
        lastGCAlloc = currentGCAlloc;

        // Record metrics if in recording phase
        if (isRecording && !testCompleted)
        {
            fpsHistory.Add(currentFPS);
            frameTimeHistory.Add(currentFrameTime);
            gcAllocHistory.Add(gcAllocThisFrame);

            // Collect scripts time from all sheep
            MeasureScriptsTime();
        }

        // End test
        if (currentTestTime >= testDuration && !testCompleted)
        {
            testCompleted = true;
            Debug.Log("[Metrics] Test completed!");
            PrintResults();
        }
    }

    void FixedUpdate()
    {
        if (isRecording && !testCompleted)
        {
            fixedUpdateCount++;
        }
    }

    void TryFindSheep()
    {
        allSheep = GameObject.FindGameObjectsWithTag("Sheep");

        if (allSheep.Length > 0)
        {
            Debug.Log($"[Metrics] Found {allSheep.Length} sheep!");
            sheepFound = true;
            testStartTime = Time.time;
            sheepCount = allSheep.Length;
            Debug.Log($"[Metrics] Starting test with {sheepCount} sheep - Warmup: {warmupDuration}s, Recording: {testDuration - warmupDuration}s");
        }
    }

    void MeasureScriptsTime()
    {
        if (allSheep == null || allSheep.Length == 0) return;

        float totalScriptsTime = 0f;
        int validCount = 0;

        foreach (GameObject sheepObj in allSheep)
        {
            if (sheepObj == null) continue;

            // Try different agent types
            var baseAgent = sheepObj.GetComponent<SheepAgent>();
            if (baseAgent != null)
            {
                totalScriptsTime += baseAgent.lastCalcTime;
                validCount++;
                continue;
            }

            var bvhAgent = sheepObj.GetComponent<SheepAgentBVH>();
            if (bvhAgent != null)
            {
                totalScriptsTime += bvhAgent.lastCalcTime;
                validCount++;
                continue;
            }

            var tempAgent = sheepObj.GetComponent<SheepAgentTemporal>();
            if (tempAgent != null)
            {
                totalScriptsTime += tempAgent.lastCalcTime;
                validCount++;
                continue;
            }

            var gridAgent = sheepObj.GetComponent<SheepAgentGrid>();
            if (gridAgent != null)
            {
                totalScriptsTime += gridAgent.lastCalcTime;
                validCount++;
            }
        }

        if (validCount > 0)
        {
            currentScriptsTime = totalScriptsTime;
            scriptsTimeHistory.Add(currentScriptsTime);
        }
    }

    void PrintResults()
    {
        if (fpsHistory.Count == 0)
        {
            Debug.LogError("[Metrics] No data recorded!");
            return;
        }

        Debug.Log("========================================");
        Debug.Log("COPY THIS TO EXCEL (Tab-separated):");
        Debug.Log("========================================");

        // Calculate metrics
        float avgFps = fpsHistory.Average();
        float minFps = fpsHistory.Min();

        // 1% Low FPS
        var sortedFps = fpsHistory.OrderBy(x => x).ToList();
        int onePercentIndex = Mathf.Max(1, (int)(sortedFps.Count * 0.01f));
        float onePercentLowFps = sortedFps.Take(onePercentIndex).Average();

        float maxFrameTime = frameTimeHistory.Max();

        // Scripts time per FixedUpdate (all sheep combined)
        float avgScriptsTimePerFixedUpdate = scriptsTimeHistory.Average();
        float maxScriptsTimePerFixedUpdate = scriptsTimeHistory.Max();

        // Time per individual sheep
        float avgTimePerSheep = avgScriptsTimePerFixedUpdate / sheepCount;

        // GC Alloc
        long avgGcAlloc = (long)gcAllocHistory.Average();

        // Print data row
        Debug.Log($"{algorithmName}\t{sheepCount}\t{avgFps:F2}\t{minFps:F2}\t{onePercentLowFps:F2}\t{maxFrameTime:F2}\t{avgScriptsTimePerFixedUpdate:F2}\t{maxScriptsTimePerFixedUpdate:F2}\t{avgTimePerSheep:F4}\t{avgGcAlloc}");

        Debug.Log("");
        Debug.Log("========================================");
        Debug.Log("Column Headers (copy to Excel first row):");
        Debug.Log("Algorithm\tSheep Count\tAvg FPS\tMin FPS\t1% Low FPS\tMax Frame Time (ms)\tTime per FixedUpdate (ms)\tMax FixedUpdate Time (ms)\tTime per Sheep (ms)\tGC Alloc (bytes/frame)");
        Debug.Log("========================================");

        Debug.Log("");
        Debug.Log("DETAILED BREAKDOWN:");
        Debug.Log($"Total frames recorded: {fpsHistory.Count}");
        Debug.Log($"Total FixedUpdates: {fixedUpdateCount}");
        Debug.Log($"FixedUpdate/Frame ratio: {((float)fixedUpdateCount / fpsHistory.Count):F2}");
        Debug.Log($"");
        Debug.Log($"Time per FixedUpdate: {avgScriptsTimePerFixedUpdate:F2} ms (for {sheepCount} sheep)");
        Debug.Log($"Time per Sheep: {avgTimePerSheep:F4} ms");
        Debug.Log($"");
        Debug.Log($"Expected FixedUpdates per second: 50");
        Debug.Log($"Actual FixedUpdates per second: {(1000f / avgScriptsTimePerFixedUpdate):F1}");
        Debug.Log($"Performance ratio: {(avgScriptsTimePerFixedUpdate / 20f):F2}x (1.0x = ideal, higher = slower)");
    }

    void OnGUI()
    {
        if (!showDebugUI) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        GUI.Box(new Rect(10, 10, 400, 200), "");

        float y = 20;
        GUI.Label(new Rect(20, y, 350, 30), $"Algorithm: {algorithmName} | Sheep: {sheepCount}", style);
        y += 25;

        if (!sheepFound)
        {
            GUI.Label(new Rect(20, y, 350, 30), "Waiting for sheep to spawn...", style);
            return;
        }

        string status = !isRecording ? $"Warmup: {(warmupDuration - currentTestTime):F1}s" :
                       !testCompleted ? $"Recording: {(testDuration - currentTestTime):F1}s" :
                       "COMPLETED - Check Console";

        GUIStyle statusStyle = new GUIStyle(style);
        statusStyle.normal.textColor = !isRecording ? Color.yellow : !testCompleted ? Color.green : Color.cyan;
        GUI.Label(new Rect(20, y, 350, 30), status, statusStyle);
        y += 30;

        GUI.Label(new Rect(20, y, 350, 30), $"FPS: {currentFPS:F1} (Avg: {(fpsHistory.Count > 0 ? fpsHistory.Average() : 0):F1})", style);
        y += 25;
        GUI.Label(new Rect(20, y, 350, 30), $"Frame Time: {currentFrameTime:F2} ms", style);
        y += 25;
        GUI.Label(new Rect(20, y, 350, 30), $"Scripts Time: {currentScriptsTime:F2} ms", style);
        y += 25;

        if (fpsHistory.Count > 0)
        {
            GUI.Label(new Rect(20, y, 350, 30), $"Min FPS: {fpsHistory.Min():F1}", style);
        }
    }
}