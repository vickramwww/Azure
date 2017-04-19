using Sitecore.Data.Items;
using Sitecore.Events.Hooks;
using Sitecore.Resources.Media;

namespace AzureMediaStorage.Custom.Pipelines
{
    public class AzureMediaProvider: MediaProvider, IHook
    {
        public void Initialize()
        {
            MediaManager.Provider = this;
        }

        public override string GetMediaUrl(MediaItem item)
        {
            string mediaUrl = base.GetMediaUrl(item);
            return mediaUrl;
        }

        public override string GetMediaUrl(MediaItem item, MediaUrlOptions options)
        {
            string mediaUrl = base.GetMediaUrl(item, options);
            return GetCstmMediaUrl(mediaUrl, item);
        }

        /// <summary>
        /// Determines if media should be pulling from the CDN or not
        /// </summary>
        /// <param name="mediaUrl"></param>
        /// <param name="item"></param>
        /// <returns></returns>               
        public string GetCstmMediaUrl(string mediaUrl, MediaItem item)
        {
            //verify the domain was set in the config
            if (string.IsNullOrEmpty(OriginPrefix))
            {
                return mediaUrl;
            }

            //Condition to fetch the Azure url only if the site is rendered and not for Preview or editing mode.            
            if (Sitecore.Context.GetSiteName().ToLower() != "website" || Sitecore.Context.PageMode.IsExperienceEditor || Sitecore.Context.PageMode.IsPreview || Sitecore.Context.PageMode.IsSimulatedDevicePreviewing)
            {
                return mediaUrl;
            }
            mediaUrl = mediaUrl.Replace("~/media/", "/");
            //this happens while indexing unless the proper site is set
            mediaUrl = mediaUrl.Replace("/sitecore/shell/", "/");
            mediaUrl = mediaUrl.Replace("//", "/");
            //reference the file in the cdn by the actual extension
            mediaUrl = mediaUrl.Replace(".ashx", "." + item.Extension);
            mediaUrl = string.Format("{0}{1}", OriginPrefix, mediaUrl);
            string Language = string.Empty;
            if (mediaUrl.Contains("la="))
            {
                Language = mediaUrl.Substring(mediaUrl.IndexOf("la=") + 3);
                if (Language.Contains("&"))
                    Language = Language.Substring(0, Language.IndexOf("&"));
            }
            else
            {
                Language = Sitecore.Configuration.Settings.DefaultLanguage;
            }
            //create the media url accoriding to the naming convention of Azure-media-file name
            mediaUrl = mediaUrl.Replace(item.DisplayName + "." + item.Extension, item.ID.ToString().Replace("{", "").Replace("}", "").Replace("-", "") + "-" + Language + "." + item.Extension);
            //if (HttpContext.Current != null && HttpContext.Current.Request.IsSecureConnection)
            //{
            //    //if we are on a secure connection, make sure we are making an https url over to the cdn
            //    mediaUrl = mediaUrl.Replace("http://", "https://");
            //}
            return mediaUrl;
        }


        public string OriginPrefix
        {
            get
            {
                return System.Configuration.ConfigurationManager.AppSettings["OrignalPrefix"];

            }
        }
    }
}
