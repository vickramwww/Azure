﻿using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureMediaStorage.Custom.Pipelines
{
    public class AzurePublishing: PublishItemProcessor
    {
        public string Enabled { get; set; }
        public override void Process(PublishItemContext context)
        {
            //Check the configuration to run the processor or not
            if (this.Enabled.ToLower() != "yes")
                return;
            Log.Debug("Performing CDN validations", (object)this);
            Assert.ArgumentNotNull((object)context, "context");
            //Get the Context Item
            Item sourceItem = context.PublishHelper.GetSourceItem(context.ItemId);
            //If the source item is null, get the target item (specifically used for deleted item)
            if (sourceItem == null || !sourceItem.Paths.IsMediaItem)
            {
                Item webSourceItem = context.PublishHelper.GetTargetItem(context.ItemId);
                if (webSourceItem == null || !webSourceItem.Paths.IsMediaItem)
                {
                    return;
                }
                else
                {
                    sourceItem = webSourceItem;
                }

            }
            MediaItem mediaItem = (MediaItem)sourceItem;
            string mediaExtension = mediaItem.Extension;
            //Get the Media Stream
            Stream mediaStream = mediaItem.GetMediaStream();
            if (mediaStream == null || mediaStream.Length == 0L)
            {
                if (((MediaItem)context.PublishHelper.GetTargetItem(context.ItemId)).GetMediaStream() == null || ((MediaItem)context.PublishHelper.GetTargetItem(context.ItemId)).GetMediaStream().Length == 0L)
                    return;
                else
                    mediaExtension = ((MediaItem)context.PublishHelper.GetTargetItem(context.ItemId)).Extension;
            }
            AzureStorage azureStorageUpload = new AzureStorage();
            Log.Debug("Starting CDN synchonization", (object)this);
            try
            {
                //Get Version Information
                Item versionToPublish = context.VersionToPublish;
                if (versionToPublish == null)
                {
                    if (context.PublishHelper.GetTargetItemInLanguage(context.ItemId, sourceItem.Language) != null)
                        versionToPublish = context.PublishHelper.GetTargetItemInLanguage(context.ItemId, sourceItem.Language);
                }

                if (versionToPublish != null)
                {
                    //Parameters to upload/replace/delete from on Azure
                    object[] args = new object[] { mediaItem, mediaExtension, versionToPublish.Language.Name };
                    Sitecore.Jobs.JobOptions jobOptions = null;
                    Context.Job.Status.State = JobState.Initializing;
                    if (context.Action == PublishAction.None)
                    {

                        jobOptions = new Sitecore.Jobs.JobOptions(
                            mediaItem.ID.ToString(),                     // identifies the job
                            "CDN Upload",                 // categoriezes jobs
                            Sitecore.Context.Site.Name,         // context site for job
                            azureStorageUpload,                  // object containing method
                            "uploadMediaToAzure",                  // method to invoke
                            args)                               // arguments to method
                        {
                            AfterLife = TimeSpan.FromSeconds(5),  // keep job data for one hour
                            EnableSecurity = false,             // run without a security context
                        };
                        Context.Job.Status.State = JobState.Finished;
                        Sitecore.Jobs.Job pub = Sitecore.Jobs.JobManager.Start(jobOptions);
                    }
                    if (context.Action == PublishAction.PublishSharedFields || context.Action == PublishAction.PublishVersion)
                    {
                        jobOptions = new Sitecore.Jobs.JobOptions(mediaItem.ID.ToString(), "CDN Upload", Sitecore.Context.Site.Name, azureStorageUpload, "replaceMediaFromAzure", args) { AfterLife = TimeSpan.FromSeconds(5), EnableSecurity = false, };
                        Context.Job.Status.State = JobState.Finished;
                        Sitecore.Jobs.Job pub = Sitecore.Jobs.JobManager.Start(jobOptions);
                    }
                    //If the publish action is delete target item, get all the language versions of the item and delete it from Azure
                    if (context.Action == PublishAction.DeleteTargetItem)
                    {
                        foreach (Language lang in context.PublishOptions.TargetDatabase.GetLanguages())
                        {
                            mediaItem = context.PublishHelper.GetTargetItemInLanguage(mediaItem.ID, lang);
                            args = new object[] { mediaItem, mediaItem.Extension, lang.Name };
                            jobOptions = new Sitecore.Jobs.JobOptions(mediaItem.ID.ToString(), "CDN Upload", Sitecore.Context.Site.Name, azureStorageUpload, "deleteMediaFromAzure", args)
                            {
                                AfterLife = TimeSpan.FromSeconds(5),
                                EnableSecurity = false,
                            };
                            Context.Job.Status.State = JobState.Finished;
                            Sitecore.Jobs.Job pub = Sitecore.Jobs.JobManager.Start(jobOptions);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception exception = new Exception(string.Format("CDN Processing failed for {1} ({0} version: {2}). {3}", (object)sourceItem.ID, (object)sourceItem.Name, (object)context.VersionToPublish.Language.Name, (object)ex.Message));
                Log.Error(exception.Message, exception, (object)this);
                context.Job.Status.Failed = true;
                context.Job.Status.Messages.Add(exception.Message);
            }
            Log.Debug(" CDN synchronization finished ", (object)this);
        }
    }
}

