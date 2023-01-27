//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
////using Microsoft.WindowsAzure.Storage;
////using Microsoft.WindowsAzure.Storage.Blob;
//using System.Threading.Tasks;
//using System.Linq;

//public class StorageAccount
//{
//    private string connectionString;

//    //private Dictionary<string, IListBlobItem> listBlobs;

//    private string CurrentcontainerName;

//    public StorageAccount(string connectionString)
//    {
//        this.connectionString = connectionString;
        
//    }

//    public string GetBlob(string containerName, string fileName)
//    {
        

//        //// Setup the connection to the storage account
//        //CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

//        //// Connect to the blob storage
//        //CloudBlobClient serviceClient = storageAccount.CreateCloudBlobClient();
//        //// Connect to the blob container
//        //CloudBlobContainer container = serviceClient.GetContainerReference($"{containerName}");
//        //// Connect to the blob file
//        //CloudBlockBlob blob = container.GetBlockBlobReference($"{fileName}");
//        //// Get the blob file as text
//        //string contents = blob.DownloadTextAsync().Result;

//        return listBlobs[fileName].StorageUri.SecondaryUri.ToString();
//    }


//    public async Task<List<string>> GetBlobNames(string containerName)
//    {
//        if(listBlobs == null || CurrentcontainerName == containerName)
//        {
//           await LoadBlob(containerName);
//        }
        
//        return listBlobs.Keys.ToList();
//    }

//    public async Task<List<string>> LoadBlob(string containerName)
//    {

//        // Setup the connection to the storage account
//        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
//        // Connect to the blob storage
//        CloudBlobClient serviceClient = storageAccount.CreateCloudBlobClient();
//        // Connect to the blob container
//        CloudBlobContainer container = serviceClient.GetContainerReference($"{containerName}");

//        var context = new OperationContext();
//        var options = new BlobRequestOptions();
//        BlobContinuationToken blobContinuationToken = null;
//        var list = new List<string>();
//        do
//        {
//            var results = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, context);
//            blobContinuationToken = results.ContinuationToken;
//            foreach (var item in results.Results)
//            {
//                listBlobs.Add(item.Uri.ToString().Split('/').Last(), item);
//               // list.Add(item.Uri.ToString().Split('/').Last());
//            }
//        } while (blobContinuationToken != null);
//        return list;
//    }
//}
