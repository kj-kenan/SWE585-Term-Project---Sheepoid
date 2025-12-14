using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor; // Needed for saving meshes in the editor
#endif

public class SheepMeshBaker : MonoBehaviour
{
    // Adds this action to the right-click menu
    [ContextMenu("Bake Sheep Mesh")]
    void BakeMesh()
    {
#if UNITY_EDITOR
        // Reset transform to avoid offsets
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Collect all child meshes
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        List<CombineInstance> combine = new List<CombineInstance>();

        for (int i = 0; i < meshFilters.Length; i++)
        {
            // Skip root and null meshes
            if (meshFilters[i].sharedMesh != null && meshFilters[i].gameObject != this.gameObject)
            {
                CombineInstance ci = new CombineInstance();
                ci.mesh = meshFilters[i].sharedMesh;
                ci.transform = meshFilters[i].transform.localToWorldMatrix;
                combine.Add(ci);
            }
        }

        // Merge meshes
        Mesh newMesh = new Mesh();
        // newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // For large meshes
        newMesh.CombineMeshes(combine.ToArray());

        // Save as an asset
        string path = "Assets/BakedSheep.asset";
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();

        Debug.Log("Mesh baked and saved to: " + path);

        // Restore transform
        transform.position = oldPos;
        transform.rotation = oldRot;
#endif
    }
}
