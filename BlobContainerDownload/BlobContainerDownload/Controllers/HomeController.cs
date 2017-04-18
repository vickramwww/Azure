using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BlobContainerDownload.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public EmptyResult DownloadPDF(string blobName)
        {
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(
"<azure-storage-connectionstring>");
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("<blob-container-name>");

            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            MemoryStream ms = new MemoryStream();

            blob.DownloadToStream(ms);

            Response.ContentType = blob.Properties.ContentType;
            Response.AddHeader("Content-Length", blob.Properties.Length.ToString());
            Response.AddHeader("Content-Disposition", "Attachment;Filename=" + blobName.ToString());

            Response.BinaryWrite(ms.ToArray());

            return new EmptyResult();
        }

    }
}