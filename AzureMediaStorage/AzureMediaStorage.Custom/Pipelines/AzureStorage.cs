using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Sitecore.Data.Items;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureMediaStorage.Custom.Pipelines
{
    public class AzureStorage
    {
        CloudStorageAccount storageAccount;
        CloudBlobClient blobClient;
        CloudBlobContainer container;

        public AzureStorage()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"].ToString());
            blobClient = storageAccount.CreateCloudBlobClient();
            container = blobClient.GetContainerReference(ConfigurationManager.AppSettings["AzureContainer"]);
            container.CreateIfNotExists();

            container.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
        }

        public void UploadMediaToAzure(MediaItem mediaItem, string extension = "", string language = "" )
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(mediaItem.MediaPath.TrimStart('/').Replace(mediaItem.DisplayName, mediaItem.ID.ToString().Replace("{", "").Replace("}", "").Replace("-", "")) + "-" + language + "." + extension);
            blockBlob.DeleteIfExists();

            if (string.IsNullOrEmpty(mediaItem.Extension))
                return;
            if(mediaItem.HasMediaStream("Media"))
            {
                using (var fs = (FileStream)mediaItem.GetMediaStream())
                {
                    blockBlob.UploadFromStream(fs);
                }
            }
            else
            {
                blockBlob.DeleteIfExists();
            }
        }

        public void DeleteMediaFromAzure(MediaItem mediaItem, string extension = "", string language = "")
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(mediaItem.MediaPath.TrimStart('/').Replace(mediaItem.DisplayName, mediaItem.ID.ToString().Replace("{", "").Replace("}", "").Replace("-", "")) + "-" + language + "." + extension);

            blockBlob.DeleteIfExists();
        }

        public void ReplaceMediaFromAzure(MediaItem mediaItem, string extension="", string language="")
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(mediaItem.MediaPath.TrimStart('/').Replace(mediaItem.DisplayName, mediaItem.ID.ToString().Replace("{", "").Replace("}", "").Replace("-", "")) + "-" + language + "." + extension);

            blockBlob.DeleteIfExists();

            if (string.IsNullOrEmpty(mediaItem.Extension))
                return;
            using (var fs = mediaItem.GetMediaStream())
            {
                blockBlob.UploadFromStream(fs);
            }

        }



    }
}
