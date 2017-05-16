using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Mvc;

namespace ConsumeMediaService.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string _mediaServicesAccountName =
         ConfigurationManager.AppSettings["MediaServicesAccountName"];
        private static readonly string _mediaServicesAccountKey =
            ConfigurationManager.AppSettings["MediaServicesAccountKey"];

        private static CloudMediaContext _context = null;
        private static MediaServicesCredentials _cachedCredentials = null;

        public ActionResult Index()
        {
            try
            {
                _cachedCredentials = new MediaServicesCredentials(
                                _mediaServicesAccountName,
                                _mediaServicesAccountKey);

                _context = new CloudMediaContext(_cachedCredentials);

                var accessToken = _context.Credentials.AccessToken;
                var tokenExpiration = _context.Credentials.TokenExpiration;

                IAsset inputAsset =
                UploadFile(@"C:\GitRepo\Azure\media1.mp4", AssetCreationOptions.None);

                IAsset encodedAsset =
                    EncodeToAdaptiveBitrateMP4s(inputAsset, AssetCreationOptions.None);

                PublishAssetGetURLs(encodedAsset);
            }
            catch (Exception exception)
            {
                // Parse the XML error message in the Media Services response and create a new
                // exception with its content.
                exception = MediaServicesExceptionParser.Parse(exception);

                Console.Error.WriteLine(exception.Message);
            }
            finally
            {
                Console.ReadLine();
            }


            // SaveTokenDataToExternalStorage(accessToken, tokenExpiration);
            return View();
        }
        static public IAsset UploadFile(string fileName, AssetCreationOptions options)
        {
            IAsset inputAsset = _context.Assets.CreateFromFile(
                fileName,
                options,
                (af, p) =>
                {
                    Debug.Write($"Uploading {af.Name} - Progress: 1:0.{p.Progress}%");
                });

            Debug.Write("Asset {0} created.", inputAsset.Id);

            return inputAsset;
        }

        static public IAsset EncodeToAdaptiveBitrateMP4s(IAsset asset, AssetCreationOptions options)
        {

            // Prepare a job with a single task to transcode the specified asset
            // into a multi-bitrate asset.

            IJob job = _context.Jobs.CreateWithSingleTask(
                "Media Encoder Standard",
                "Adaptive Streaming",
                asset,
                "Adaptive Bitrate MP4",
                options);

            Debug.Write("Submitting transcoding job...");


            // Submit the job and wait until it is completed.
            job.Submit();

            job = job.StartExecutionProgressTask(
                j =>
                {
                    Debug.Write($"Job state: {j.State}");
                    Debug.Write($"Job progress: 0:0.{j.GetOverallProgress()}");
                },
                CancellationToken.None).Result;

            Debug.Write("Transcoding job finished.");

            IAsset outputAsset = job.OutputMediaAssets[0];

            return outputAsset;
        }
        static public void PublishAssetGetURLs(IAsset asset)
        {
            // Publish the output asset by creating an Origin locator for adaptive streaming,
            // and a SAS locator for progressive download.

            _context.Locators.Create(
                LocatorType.OnDemandOrigin,
                asset,
                AccessPermissions.Read,
                TimeSpan.FromDays(30));

            _context.Locators.Create(
                LocatorType.Sas,
                asset,
                AccessPermissions.Read,
                TimeSpan.FromDays(30));


            IEnumerable<IAssetFile> mp4AssetFiles = asset
                    .AssetFiles
                    .ToList()
                    .Where(af => af.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

            // Get the Smooth Streaming, HLS and MPEG-DASH URLs for adaptive streaming,
            // and the Progressive Download URL.
            Uri smoothStreamingUri = asset.GetSmoothStreamingUri();
            Uri hlsUri = asset.GetHlsUri();
            Uri mpegDashUri = asset.GetMpegDashUri();

            // Get the URls for progressive download for each MP4 file that was generated as a result
            // of encoding.
            List<Uri> mp4ProgressiveDownloadUris = mp4AssetFiles.Select(af => af.GetSasUri()).ToList();

            
            // Display  the streaming URLs.
            Debug.Write("Use the following URLs for adaptive streaming: ");
            Debug.Write(smoothStreamingUri);
            Debug.Write(hlsUri);
            Debug.Write(mpegDashUri);

            // Display the URLs for progressive download.
            Debug.Write("Use the following URLs for progressive download.");
            mp4ProgressiveDownloadUris.ForEach(uri => Debug.Write(uri + "\n"));

            // Download the output asset to a local folder.
            string outputFolder = "job-output";
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            Debug.Write("Downloading output asset files to a local folder...");
            asset.DownloadToFolder(
                outputFolder,
                (af, p) =>
                {
                    Debug.Write($"Downloading {af.Name} - Progress: 1:0.{p.Progress}%");
                });

            Debug.Write("Output asset files available at '{0}'.", Path.GetFullPath(outputFolder));
        }

    }
}