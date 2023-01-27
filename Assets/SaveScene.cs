using Azure.StorageServices;
using System.IO;
using UnityEngine;

public class SaveScene : MonoBehaviour
{
    public GameObject parentObject;
    string objString = "";

    private StorageServiceClient client;
    private BlobService blobService;
    void Start()
    {
        client = StorageServiceClient.Create("researchprojectstorag", "cEMqDAFdtl5eJ1rkaDlQ61ZWLZ+1VVX6ub2RqDMP55jqEnc94rBUfi+6UQvrZEqxCI3hAyFNqiu/+AStmYfqeQ==");
        blobService = client.GetBlobService();

        
        //  ExportAllChildMeshes(parentObject);
    }

    public void ExportAllChildMeshes(GameObject parent)
    {
        string path = "./Objects/" + "scene_" + System.DateTime.Now.ToString("hh_mm_ss") + ".obj_scene";
        MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
        objString = "";
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter filter = meshFilters[i];
            ExportMesh(filter.gameObject.transform, filter.sharedMesh, filter.gameObject.name, i);
        }
        File.WriteAllText(path, objString.Replace(",", "."));

        //needs testing
        StartCoroutine(blobService.PutTextBlob(response => { }, objString.Replace(",", "."), "scene", "scene_" + System.DateTime.Now.ToString("hh_mm_ss") + ".obj_scene"));
    }

    void ExportMesh(Transform obj_pos, Mesh mesh, string name, int index)
    {


        //Get the vertex data of the mesh
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        //Create a new string to hold the OBJ file contents
        objString += "g " + name + "\n";

        //Add the vertex data to the OBJ file
        for (int i = 0; i < vertices.Length; i++)
        {
            // objString += "v " + (vertices[i].x + obj_pos.x) + " " + (vertices[i].y + obj_pos.y) + " " + (vertices[i].z + obj_pos.z) + "\n";
            var v = vertices[i];
            v = MultiplyVec3s(v, obj_pos.lossyScale);
            v = RotateAroundPoint(v, Vector3.zero, obj_pos.rotation);
            v += obj_pos.position;
            //v.x *= -1;
            objString += ("v " + v.x + " " + v.y + " " + v.z) + "\n";
        }

        //Add the UV data to the OBJ file
        for (int i = 0; i < uvs.Length; i++)
        {
            objString += "vt " + uvs[i].x + " " + uvs[i].y + "\n";
        }

        //Add the normal data to the OBJ file
        for (int i = 0; i < normals.Length; i++)
        {
            var v = normals[i];
            v = MultiplyVec3s(v, obj_pos.lossyScale);
            v = RotateAroundPoint(v, Vector3.zero, obj_pos.rotation);
            v += obj_pos.position;
            //v.x *= -1;
            objString += ("vn " + v.x + " " + v.y + " " + v.z) + "\n";
            // objString += "vn " + normals[i].x + " " + normals[i].y + " " + normals[i].z + "\n";
        }

        //Add the triangle data to the OBJ file
        for (int i = 0; i < triangles.Length; i += 3)
        {
            objString += "f " + ((triangles[i] + 1) + index * vertices.Length) + " " +
                         ((triangles[i + 1] + 1) + index * vertices.Length) + " " +
                         ((triangles[i + 2] + 1) + index * vertices.Length) + "\n";
        }

        //Write the OBJ file to disk

    }

    Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }
    Vector3 MultiplyVec3s(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
}
