using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using System.IO;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class StorageHelper
    {
        public const string StorageConnectionString = "xxx";

        public const string Container_Feedback = "feedback";
        public const string Container_TopicAudio = "topicaudio";

        public static async Task<BlobContainerClient> CreateContainer(string containerName)
        {
            var container = new BlobContainerClient(StorageConnectionString, containerName);
            await container.CreateIfNotExistsAsync();
            await container.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

            return container;
        }
        

        public static async Task UploadFile(BlobContainerClient container, string fileName, Stream stream)
        {
            var mimeProvider = new FileExtensionContentTypeProvider();
            string contentType;

            if (!mimeProvider.TryGetContentType(fileName, out contentType))
            {
                contentType = "application/octet-stream"; // Pokud se nepovede prevod
            }

            var blobHttpHeader = new BlobHttpHeaders();
            blobHttpHeader.ContentType = contentType;

            
            await container.CreateIfNotExistsAsync();

            BlobClient blob = container.GetBlobClient(fileName);
            await blob.UploadAsync(stream, blobHttpHeader);
        }

        public static async Task DeleteFile(BlobContainerClient container, string fileName)
        {
            await container.CreateIfNotExistsAsync();

            BlobClient blob = container.GetBlobClient(fileName);
            await blob.DeleteIfExistsAsync();
        }
    }
}
