using UnityEngine;
using System.Collections.Generic; // List support

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SheepMeshCombiner : MonoBehaviour
{
    void Start()
    {
        CombineMeshes();
    }

    void CombineMeshes()
    {
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
                meshFilters[i].gameObject.SetActive(false); // Hide original
            }
        }

        // Build merged mesh
        Mesh mesh = new Mesh();
        // mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // For large meshes
        mesh.CombineMeshes(combine.ToArray());

        // Assign and restore transform
        GetComponent<MeshFilter>().mesh = mesh;
        transform.position = oldPos;
        transform.rotation = oldRot;
    }
}
