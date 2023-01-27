using Dummiesman;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
//using UnityMeshImporter;
using System.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Text.RegularExpressions;
using Azure.StorageServices;
using System.Globalization;


public class Document_manager : MonoBehaviour
{
    [SerializeField]
    private string path = "./Objects";
    [SerializeField]
    private Transform _parent;

    [SerializeField]
    private Material _material;


    [Header("Explorer")]
    [SerializeField]
    private TextMeshPro _path_label;

    [SerializeField]
    private GameObject _file_item_prefab;

    [SerializeField]
    private Transform _explorer_items;


    [Header("Cloud")]

    [SerializeField]
    private Transform _cloud_items;
    [SerializeField]
    private TextMeshPro _container_label;
    private StorageServiceClient client;
    private BlobService blobService;
    private Blob[] blobs;
    private Container[] containers;
    private string cloudPath = "";

    private string cloud_save_path = "./Objects";

    [Header("Info")]
    [SerializeField]
    private GameObject yesnoScreen;

    [SerializeField]
    private GameObject setinfoscreen;

    private void Start()
    {
        path = System.Environment.GetEnvironmentVariable("USERPROFILE");

        refreshExplorer(path, new Func<DirectoryInfo, bool>(d => Regex.IsMatch(d.Name, @"\bOneDrive\b")));

        // var storageAccount = new StorageAccount("DefaultEndpointsProtocol=https;AccountName=researchprojectstorag;AccountKey=cEMqDAFdtl5eJ1rkaDlQ61ZWLZ+1VVX6ub2RqDMP55jqEnc94rBUfi+6UQvrZEqxCI3hAyFNqiu/+AStmYfqeQ==;EndpointSuffix=core.windows.net");
        //print(storageAccount.GetBlobNames("test"));
        client = StorageServiceClient.Create("researchprojectstorag", "cEMqDAFdtl5eJ1rkaDlQ61ZWLZ+1VVX6ub2RqDMP55jqEnc94rBUfi+6UQvrZEqxCI3hAyFNqiu/+AStmYfqeQ==");
        blobService = client.GetBlobService();
        refreshCloud("");




    }

    private void refreshExplorer(string dir_path, Func<DirectoryInfo, bool> filter = null)
    {
        _path_label.text = dir_path;
        path = dir_path;
        if (!Directory.Exists(dir_path))
        {
            Directory.CreateDirectory(dir_path);

        }
        var info = new DirectoryInfo(dir_path);
        var fileInfo = info.GetFiles().Where(c => c.Extension == ".obj" || c.Extension == ".obj_scene").ToArray();
        var DirInfo = info.GetDirectories();
        if (filter != null)
        {
            DirInfo = info.GetDirectories().Where(filter).ToArray();
        }

        //  .Where(d => Regex.IsMatch(d.Name, @"\bOneDrive\b")).ToArray();


        foreach (Transform child in _explorer_items)
        {
            Destroy(child.gameObject);
        }
        var Backbutton = Instantiate(_file_item_prefab, parent: _explorer_items);
        var Backbutton_text = Backbutton.transform.GetComponentInChildren<TextMeshPro>();
        Backbutton_text.text = "...";
        Backbutton.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { refreshExplorer(path.Remove(path.LastIndexOf('\\'))); });
        Backbutton.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("folder");
        foreach (var dir in DirInfo)
        {
            print(dir.FullName);

            var newDirName = Instantiate(_file_item_prefab, parent: _explorer_items);
            var text = newDirName.transform.GetComponentInChildren<TextMeshPro>();
            text.text = dir.Name;
            newDirName.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { refreshExplorer(dir.FullName); });
            newDirName.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("folder");

        }

        foreach (var file in fileInfo)
        {
            print(file.FullName);

            var newFileName = Instantiate(_file_item_prefab, parent: _explorer_items);
            var text = newFileName.transform.GetComponentInChildren<TextMeshPro>();
            text.text = file.Name;
            if (file.Extension == ".obj")
            {
                newFileName.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { fileNamePressed(file.FullName); });
                newFileName.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("obj");
            }
            else
            {
                newFileName.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { LoadScene(file.FullName, _explorer_items); });
                newFileName.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("web-programming");
            }

        }
        _explorer_items.gameObject.GetComponent<GridObjectCollection>().UpdateCollection();

        StartCoroutine("func");

        //testing zone______
        //  Open_file(fileInfo[0].FullName);
        //Download("https://researchprojectstorag.file.core.windows.net/objects/Gear_Spur_16T.obj");
        //_________________

    }

    private void OpenCloudFile(string name, string contrainer, bool isSceneFile = false)
    {
        // moet nog beveiligt worden tegen overschrijving (momenteel overschijft het de bestaande bestanden met de zelfde naam) done
        if (File.Exists(cloud_save_path + "/" + name))
        {
            var ynscreen = Instantiate(yesnoScreen);
            var par = ynscreen.GetComponent<yesnoParameters>();
            par.title.text = "Overwrite of file";
            par.content.text = "Do you want to overwrite the local saved file? \n say no to use the local file and ignore the one in the cloud.";

            if (!isSceneFile)
            {
                par.btn_yes.ButtonReleased.AddListener(() => { overWriteAndOpenCloudFile(name, contrainer, ynscreen); });
                par.btn_no.ButtonReleased.AddListener(() => { Open_file(cloud_save_path + "/" + name,_cloud_items); Destroy(ynscreen); });
                //par.btn_close.ButtonReleased.AddListener(() => {  });
            }
            else
            {
                par.btn_yes.ButtonReleased.AddListener(() => { overWriteAndOpenCloudFile(name, contrainer, ynscreen, true); }); // hier
                par.btn_no.ButtonReleased.AddListener(() => { LoadScene(cloud_save_path + "/" + name, _cloud_items); Destroy(ynscreen); });
            }
        }
        else
        {
            overWriteAndOpenCloudFile(name, contrainer);
        }


    }

    void overWriteAndOpenCloudFile(string name, string contrainer, GameObject ynscreen = null, bool isSceneFile = false)
    {
        StartCoroutine(blobService.GetTextBlob(response =>
        {
            if (response.IsError)
            {
                Debug.LogError($"Could not load text : {response.ErrorMessage}");
                return;
            }
            else
            {


                print(response.Url);
                print(response.Content);
                if (name.Contains("/")) 
                {
                    if (!Directory.Exists(cloud_save_path + "/" + name.Substring(0, name.LastIndexOf('/'))))
                    {
                        Directory.CreateDirectory(cloud_save_path + "/" + name.Substring(0, name.LastIndexOf('/')));
                    }
                }
                File.WriteAllText(cloud_save_path + "/" + name, response.Content);

                // the object info file
                StartCoroutine(blobService.GetTextBlob(response =>
                {
                    if (response.IsError)
                    {
                        Debug.LogError($"Could not load text : {response.ErrorMessage}");
                        // return;
                    }
                    else
                    {
                        print(response.Url);
                        print(response.Content);
                        File.WriteAllText(cloud_save_path + "/" + name.Replace(".obj", ".objectinfo"), response.Content);
                    }
                }, contrainer + "/" + name.Replace(".obj", ".objectinfo")));

                // the mtl file
                StartCoroutine(blobService.GetTextBlob(response =>
                {
                    if (response.IsError)
                    {
                        Debug.LogError($"Could not load text : {response.ErrorMessage}");
                        //return;
                    }
                    else
                    {
                        print(response.Url);
                        print(response.Content);
                        File.WriteAllText(cloud_save_path + "/" + name.Replace(".obj", ".mtl"), response.Content);
                    }
                }, contrainer + "/" + name.Replace(".obj", ".mtl")));

                if (!isSceneFile)
                {
                    Open_file(cloud_save_path + "/" + name, _cloud_items);
                }
                else
                {
                    LoadScene(cloud_save_path + "/" + name, _cloud_items);
                }
                if (ynscreen != null)
                {
                    Destroy(ynscreen);
                }
            }
        }, contrainer + "/" + name));

        _container_label.text = contrainer;
    }

    private void refreshCloud(string container)
    {
        foreach (Transform child in _cloud_items)
        {
            Destroy(child.gameObject);
        }
        //var Backbutton = Instantiate(_file_item_prefab, parent: _cloud_items);
        //var Backbutton_text = Backbutton.transform.GetComponentInChildren<TextMeshPro>();
        //Backbutton_text.text = "...";
        //Backbutton.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { refreshCloud(cloudPath.Remove(path.LastIndexOf('\\'))); });
        //Backbutton.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("folder");

        StartCoroutine(blobService.ListBlobs(response =>
        {
            if (response.IsError)
            {
                Debug.LogError($"Could not load text : {response.ErrorMessage}");
                return;
            }
            blobs = response.Data.Blobs.Where(c => c.Name.Substring(c.Name.LastIndexOf('.')) == ".obj" || c.Name.Substring(c.Name.LastIndexOf('.')) == ".obj_scene").ToArray();
            if (blobs != null)
            {
                Debug.Log($"Successfully loaded text : {response.Data.Blobs}");
                foreach (var file in blobs)
                {
                    //print(file.FullName);

                    var newFileName = Instantiate(_file_item_prefab, parent: _cloud_items);
                    var text = newFileName.transform.GetComponentInChildren<TextMeshPro>();
                    text.text = file.Name;

                    //newFileName.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { OpenCloudFile(file.Name, container); });
                    //newFileName.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("obj");

                    if (file.Name.Substring(file.Name.LastIndexOf('.')) == ".obj")
                    {
                        newFileName.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { OpenCloudFile(file.Name, container); });
                        newFileName.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("obj");
                    }
                    else
                    {
                        newFileName.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { OpenCloudFile(file.Name, container, true); });
                        newFileName.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("web-programming");
                    }

                }
            }

        }, container));

        StartCoroutine(blobService.ListContainers(response =>
       {
           if (response.IsError)
           {
               Debug.LogError($"Could not load text : {response.ErrorMessage}");
               return;
           }
           containers = response.Data.Containers;
           Debug.Log($"Successfully loaded text : {response.Data.Containers[0].Name}");
           foreach (var dir in containers)
           {
               //print(dir.FullName);

               var newDirName = Instantiate(_file_item_prefab, parent: _cloud_items);
               var text = newDirName.transform.GetComponentInChildren<TextMeshPro>();
               text.text = dir.Name;
               newDirName.transform.GetComponentInChildren<PressableButtonHoloLens2>().TouchEnd.AddListener(delegate { refreshCloud(dir.Name); });
               newDirName.GetComponent<ButtonConfigHelper>().SetSpriteIconByName("folder");
               StartCoroutine("func2");
           }
       }));





        //  .Where(d => Regex.IsMatch(d.Name, @"\bOneDrive\b")).ToArray();







        _cloud_items.gameObject.GetComponent<GridObjectCollection>().UpdateCollection();

        //StartCoroutine("func2");

        //testing zone______
        //  Open_file(fileInfo[0].FullName);
        //Download("https://researchprojectstorag.file.core.windows.net/objects/Gear_Spur_16T.obj");
        //_________________

    }

    IEnumerator func()
    {

        yield return new WaitForFixedUpdate();
        _explorer_items.gameObject.GetComponent<GridObjectCollection>().UpdateCollection();
    }
    IEnumerator func2()
    {

        yield return new WaitForFixedUpdate();
        _cloud_items.gameObject.GetComponent<GridObjectCollection>().UpdateCollection();
    }


    public void fileNamePressed(string file_path)
    {
        Open_file(file_path, _explorer_items);
    }

    public void Download(string key)
    {


    }

    //public void Download(string url, string name)
    //{
    //    using (var client = new WebClient())
    //    {
    //        client.DownloadFile(url, path + "/" + name + ".obj");
    //        try
    //        {
    //            client.DownloadFile(url.Replace(".obj", ".mtl"), path + "/" + name + ".mtl");
    //        }
    //        catch
    //        {

    //        }
    //    }
    //}
    //
    // for onedrive
    //%USERPROFILE%

    private void Create_new_Infofile(string filePath, ref ObjInfo objInfo, string hash, float scale,GameObject t_obj, GameObject obj, Transform window)
    {
        

        objInfo = new ObjInfo()
        {
            id = hash,
            scale = scale,
            colliderPrecision = 0.005f

        };
        //using (FileStream fs = File.Create(path.Replace(".obj", ".objectinfo"))) ;

        File.WriteAllText(filePath.Replace(".obj", ".objectinfo"), JsonConvert.SerializeObject(objInfo));

        Add_scripts(t_obj, obj, objInfo, window);
    }

    //private void askInfoObject()
    //{
    //    var infoscreen = Instantiate(setinfoscreen);
    //            var par = infoscreen.GetComponent<SetobjectinfoProperties>();

    //            par.btn_mm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.001f); });
    //            par.btn_cm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.01f); });
    //            par.btn_dm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.1f); });
    //            par.btn_m.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 1f); });
    //}

    private void Add_scripts(GameObject t_obj, GameObject obj, ObjInfo objInfo, Transform window)
    {
        t_obj.transform.parent = _parent;

        t_obj.transform.localScale = Vector3.one * objInfo.scale;
        t_obj.transform.position = window.position;
        t_obj.transform.localPosition += new Vector3(0.3f, -0.1f, 0);



        var colliderInterpolator = obj.AddComponent<UniColliderInterpolator.ColliderInterpolator>();
        colliderInterpolator._divisionUnitLength = objInfo.colliderPrecision;
        colliderInterpolator.Generate();

        //foreach (BoxCollider c in obj.GetComponentsInChildren<BoxCollider>())
        //{
        //    BoxCollider cc = (BoxCollider)obj.AddComponent(c.GetType());
        //    cc = c;
        //}
        //Destroy(obj.transform.GetChild(0).gameObject);
        var rig = obj.AddComponent<Rigidbody>();
        rig.useGravity = false;
        rig.drag = 0.2f;
        rig.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var man = obj.AddComponent<ObjectManipulator>();
        var ne = obj.AddComponent<NearInteractionGrabbable>();
        ne.ShowTetherWhenManipulating = false;


        var o = obj.AddComponent<ObjectTouch>();

        t_obj.layer = LayerMask.NameToLayer("Object");
        obj.layer = LayerMask.NameToLayer("Object");
        obj.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Object");
    }
    public void Open_file(string filePath, Transform window)
    {
        //file path
        if (!File.Exists(filePath))
        {
            Debug.LogError("File doesn't exist. file: " + filePath);
        }
        else
        {

            GameObject t_obj = null;
            GameObject obj = null;
            float size = 0.001f;
            if (File.Exists(filePath.Replace(".obj", ".mtl")))
            {
                t_obj = new OBJLoader().Load(filePath, filePath.Replace(".obj", ".mtl"));
                obj = t_obj.transform.GetChild(0).gameObject;
            }
            else
            {
                t_obj = new OBJLoader().Load(filePath);
                obj = t_obj.transform.GetChild(0).gameObject;
                obj.GetComponent<MeshRenderer>().material = _material;
            }

            string hash = "";
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    hash = Convert.ToBase64String(md5.ComputeHash(stream));

                }
            }

            // Is the materials (from the mtl file) doesn't work with the render pipeline
            obj.GetComponent<Renderer>().material = _material;
            //
               
            var objInfo = new ObjInfo();
            if (File.Exists(filePath.Replace(".obj", ".objectinfo")))
            {
                var file = File.ReadAllText(filePath.Replace(".obj", ".objectinfo")).Replace("\n", "");
                objInfo = JsonConvert.DeserializeObject<ObjInfo>(file.ToString());

                //if not correct file ...
                if (objInfo.id != hash)
                {
                    var infoscreen = Instantiate(setinfoscreen);
                    var par = infoscreen.GetComponent<SetobjectinfoProperties>();

                    par.btn_mm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.001f, t_obj, obj, window); Destroy(infoscreen); });
                    par.btn_cm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.01f, t_obj, obj, window); Destroy(infoscreen); });
                    par.btn_dm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.1f, t_obj, obj, window); Destroy(infoscreen); });
                    par.btn_m.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 1f, t_obj, obj, window); Destroy(infoscreen); });
                }
                else
                {
                    Add_scripts(t_obj, obj, objInfo, window);
                }
            }
            else
            {
                var infoscreen = Instantiate(setinfoscreen);
                var par = infoscreen.GetComponent<SetobjectinfoProperties>();

                par.btn_mm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.001f, t_obj, obj, window); Destroy(infoscreen); });
                par.btn_cm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.01f, t_obj, obj, window); Destroy(infoscreen); });
                par.btn_dm.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 0.1f, t_obj, obj, window); Destroy(infoscreen); });
                par.btn_m.ButtonReleased.AddListener(() => { Create_new_Infofile(filePath, ref objInfo, hash, 1f, t_obj, obj, window); Destroy(infoscreen); });

            }


            


            // DoublicateFaces(obj);
        }

    

        //____________________________



        //var objLoaderFactory = new ObjLoaderFactory();
        //var objLoader = objLoaderFactory.Create() ;


        //var fileStream = new FileStream(path, FileMode.Open);
        //var result = objLoader.Load(fileStream);

        //var mesh = new Mesh();

        //mesh.vertices = ToVector3(result.Vertices);
        //mesh.normals = ToVector3(result.Normals);
        //mesh.uv = ToVector2(result.Textures);


        //var obj = new GameObject("Empty", typeof(MeshFilter), typeof(MeshRenderer));
        //obj.GetComponent<MeshFilter>().mesh = mesh;
        //______________________________________________
        //string meshFile = path;
        //var tmp_ob = MeshImporter.Load(meshFile, 0.1f, 0.1f, 0.1f);

        //var ob = tmp_ob.transform.GetChild(0).GetChild(0).gameObject;
        //ob
        //ob.GetComponent<MeshRenderer>().material = _material;


    }



    //private Vector2[] ToVector2(IList<ObjLoader.Loader.Data.VertexData.Texture> textures)
    //{
    //    try
    //    {
    //        List<Vector2> list = new List<Vector2>();
    //        foreach (ObjLoader.Loader.Data.VertexData.Texture item in textures)
    //        {
    //            list.Add(new Vector2(item.X, item.Y));
    //        }
    //        return list.ToArray();
    //    }
    //    catch
    //    {
    //        Debug.LogError("this kapoet");
    //        return new Vector2[] { };
    //    }
    //}

    //private Vector3[] ToVector3(IList<Normal> normals)
    //{
    //    try
    //    {
    //        List<Vector3> list = new List<Vector3>();
    //        foreach (Normal item in normals)
    //        {
    //            list.Add(new Vector3(item.X, item.Y, item.Z));
    //        }
    //        return list.ToArray();
    //    }
    //    catch
    //    {
    //        Debug.LogError("this kapoet");
    //        return new Vector3[] { };
    //    }
    //}

    //private Vector3[] ToVector3(IList<Vertex> vertices)
    //{
    //    try
    //    {
    //        List<Vector3> list = new List<Vector3>();
    //        foreach (Vertex item in vertices)
    //        {
    //            list.Add(new Vector3(item.X, item.Y, item.Z));
    //        }
    //        return list.ToArray();
    //    }
    //    catch
    //    {
    //        Debug.LogError("this kapoet");
    //        return new Vector3[] { };
    //    }
    //}




    //private void DoublicateFaces(GameObject obj)
    //{
    //    for (int i = 0; i < obj.GetComponentsInChildren<MeshFilter>().Length; i++)
    //    {
    //        var mesh = obj.GetComponentsInChildren<MeshFilter>()[i].mesh;
    //        var vertices = mesh.vertices;
    //        var uv = mesh.uv;
    //        var normals = mesh.normals;
    //        var szV = vertices.Length;
    //        var newVerts = new Vector3[szV * 2];
    //        var newUv = new Vector2[szV * 2];
    //        var newNorms = new Vector3[szV * 2];
    //        for (var j = 0; j < szV; j++)
    //        {
    //            // duplicate vertices and uvs:
    //            newVerts[j] = newVerts[j + szV] = vertices[j];
    //            newUv[j] = newUv[j + szV] = uv[j];
    //            // copy the original normals...
    //            newNorms[j] = normals[j];
    //            // and revert the new ones
    //            newNorms[j + szV] = -normals[j];
    //        }
    //        var triangles = mesh.triangles;
    //        var szT = triangles.Length;
    //        var newTris = new int[szT * 2]; // double the triangles
    //        for (var x = 0; i < szT; x += 3)
    //        {
    //            // copy the original triangle
    //            newTris[i] = triangles[i];
    //            newTris[i + 1] = triangles[i + 1];
    //            newTris[i + 2] = triangles[i + 2];
    //            // save the new reversed triangle
    //            var j = i + szT;
    //            newTris[j] = triangles[i] + szV;
    //            newTris[j + 2] = triangles[i + 1] + szV;
    //            newTris[j + 1] = triangles[i + 2] + szV;
    //        }
    //        mesh.vertices = newVerts;
    //        mesh.uv = newUv;
    //        mesh.normals = newNorms;
    //        mesh.triangles = newTris; // assign triangles last!
    //    }
    //}

    public void LoadScene(string filePath,Transform window)
    {
        NumberFormatInfo nfi = new NumberFormatInfo();
        nfi.NumberDecimalSeparator = ".";

        //var filePath = "Assets/" + "fingerpart" + ".obj";
        // Load the OBJ file
        string objFile = File.ReadAllText(filePath);

        // Split the OBJ file into separate objects
        string[] objFileSplit = objFile.Split(new string[] { "g " }, System.StringSplitOptions.RemoveEmptyEntries);
        // Create a new GameObject
        GameObject parent = new GameObject(filePath.Substring(path.LastIndexOf('\\')+1));
        // for some reasen this is needed
        ////////parent.transform.position = Vector3.zero;
        ////////parent.transform.Rotate(new Vector3(0, 180, 0)); // = Quaternion.Euler(0, 180, 0);
        //parent.transform.parent = transform;
        // Loop through each object
        for (int i = 0; i < objFileSplit.Length; i++)
        {
            // Create a new GameObject
            GameObject newObject = new GameObject();

            // Set the name of the GameObject
            newObject.name = "Object " + i;

            // Add a MeshFilter component to the GameObject
            MeshFilter meshFilter = newObject.AddComponent<MeshFilter>();

            // Create a new Mesh and assign it to the MeshFilter
            meshFilter.mesh = new Mesh();

            // Split the object into its vertex, normal, and UV data
            string[] objectData = objFileSplit[i].Split(new string[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            // Create lists to hold the vertex, normal, and UV data
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();


            // Get the min and max values for the x, y, and z coordinates
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            // Loop through the object data
            for (int j = 0; j < objectData.Length; j++)
            {
                // Split the data into its components
                string[] dataComponents = objectData[j].Split(' ');

                var pos_x = 0;
                // Check the first component to see if it's a vertex, normal, or UV
                if (dataComponents[0] == "v")
                {
                    // Get the x, y, and z coordinates
                    float x = float.Parse(dataComponents[1], nfi);
                    float y = float.Parse(dataComponents[2], nfi);
                    float z = float.Parse(dataComponents[3], nfi);

                    // Update the min and max values
                    min.x = Mathf.Min(min.x, x);
                    min.y = Mathf.Min(min.y, y);
                    min.z = Mathf.Min(min.z, z);
                    max.x = Mathf.Max(max.x, x);
                    max.y = Mathf.Max(max.y, y);
                    max.z = Mathf.Max(max.z, z);

                    // It's a vertex, so add it to the vertices list
                    vertices.Add(new Vector3(float.Parse(dataComponents[1], nfi), float.Parse(dataComponents[2], nfi), float.Parse(dataComponents[3], nfi)));
                }
                else if (dataComponents[0] == "vn")
                {
                    // It's a normal, so add it to the normals list
                    normals.Add(new Vector3(float.Parse(dataComponents[1], nfi), float.Parse(dataComponents[2], nfi), float.Parse(dataComponents[3], nfi)));
                }
                else if (dataComponents[0] == "vt")
                {
                    // It's a UV, so add it to the uvs list
                    uvs.Add(new Vector2(float.Parse(dataComponents[1], nfi), float.Parse(dataComponents[2], nfi)));
                }
                else if (dataComponents[0] == "f")
                {
                    //It's a face
                    //Add the face indices to the triangles list
                    //string[] parts = dataComponents.Split(' ');
                    //int[] numbers = Array.ConvertAll(dataComponents, int.Parse);
                    // triangles.AddRange(numbers);

                    triangles.Add(int.Parse(dataComponents[1]) - (i * vertices.Count) - 1);
                    triangles.Add(int.Parse(dataComponents[2]) - (i * vertices.Count) - 1);
                    triangles.Add(int.Parse(dataComponents[3]) - (i * vertices.Count) - 1);
                }
            }

            // Calculate the object's size and position
            Vector3 size = max - min;
            Vector3 position = min + size / 2;
            position.z *= -1;
            // Set the object's transform
            newObject.transform.position = position;
           // parent.transform.localScale = size;

            for (int j = 0; j < vertices.Count; j++)
            {
                vertices[j] -= position;
            }

            // Assign the vertex, normal, and UV data to the Mesh
            meshFilter.mesh.vertices = vertices.ToArray();
            meshFilter.mesh.normals = normals.ToArray();

            
            meshFilter.mesh.uv = uvs.ToArray();
            meshFilter.mesh.triangles = triangles.ToArray();

            if (normals.Count == 0)
                meshFilter.mesh.RecalculateNormals();
            //meshFilter.mesh.set
            //meshFilter.mesh.SetTriangles(triangles.ToArray(),0);
            // Add a MeshRenderer component to the GameObject
            MeshRenderer meshRenderer = newObject.AddComponent<MeshRenderer>();
            newObject.transform.parent = parent.transform;
            // Assign a material to the MeshRenderer
            meshRenderer.material = _material;

            string hash = "sceneHash is not used for this file";

            var objInfo = new ObjInfo()
            {
                id = hash,
                scale = 1,
                colliderPrecision = 0.005f

            };

            Add_scripts(parent,newObject,objInfo, window);
            //Add the gameobject to the scene
            

            // newObject.transform.localScale = new Vector3( newObject.transform.localScale.x * size.x, newObject.transform.localScale.y * size.y, newObject.transform.localScale.z * size.z);
            // newObject.transform.position = position;
        }

       

    }
}
