using UnityEngine;

public class SheepCounter : MonoBehaviour
{
    void Start()
    {
        Debug.Log("========== SHEEP COUNTER ==========");

        // Try to find sheep by tag
        try
        {
            GameObject[] sheep = GameObject.FindGameObjectsWithTag("Sheep");
            Debug.Log($"Found {sheep.Length} objects with 'Sheep' tag");

            if (sheep.Length > 0)
            {
                Debug.Log("Sheep objects:");
                foreach (GameObject s in sheep)
                {
                    Debug.Log($"  - {s.name} at position {s.transform.position}");
                }
            }
        }
        catch (UnityException e)
        {
            Debug.LogError($"Tag 'Sheep' is not defined! Error: {e.Message}");
        }

        Debug.Log("===================================");
    }
}