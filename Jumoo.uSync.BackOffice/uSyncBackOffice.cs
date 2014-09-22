using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;

using Jumoo.uSync.Core;

using System.Diagnostics;

namespace Jumoo.uSync.BackOffice
{
    /// <summary>
    ///  uSync - all the back office stuff (the traditional usync)
    ///  
    ///  mainly a wrapper for core. kicks it off, does the watching...
    /// </summary>
    public class uSyncBackOffice : ApplicationEventHandler
    {
        private static bool _synced = false;
        private static object _syncObj = new object();

        /// <summary>
        ///  this is theumbraco hook - umbraco calls this when
        ///  it starts up.
        /// </summary>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (!_synced)
            {
                lock (_syncObj)
                {
                    if (!_synced)
                    {
                        // everything we do here is blocking
                        // the application from starting
                        InitilizeSync();

                        _synced = true;
                    }
                }
            }
        }

        private void InitilizeSync()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                LogHelper.Info<uSyncBackOffice>("uSync[BackOffice] Starting: For more logging set priority to 'Debug' in log4net.config");

                if ( !ApplicationContext.Current.IsConfigured )
                {
                    LogHelper.Info<uSyncBackOffice>("Umbraco is not configured: usync aborting");
                    return; 
                }

                if (uSyncBackOfficeSettings.Import)
                {
                    LogHelper.Info<uSyncBackOffice>("Importing from Disk {0}", ()=> uSyncBackOfficeSettings.Folder);
                    ImportAllFromDisk();
                }

                if (!System.IO.Directory.Exists(uSyncBackOfficeSettings.Folder) || uSyncBackOfficeSettings.ExportAtStartup)
                {
                    LogHelper.Info<uSyncBackOffice>("Exporting all settings to disk");
                    ExportAllToDisk();
                }

                if (uSyncBackOfficeSettings.ExportOnSave)
                {
                    LogHelper.Info<uSyncBackOffice>("Attaching to Save Events");
                    AttachToEvents();
                }

                if (uSyncBackOfficeSettings.WatchFolderForChanges)
                {
                    LogHelper.Info<uSyncBackOffice>("Watching Folder for changes");
                }

                sw.Stop();
                LogHelper.Info<uSyncBackOffice>("uSync[BackOffice] Initilized ({0}ms)", () => sw.ElapsedMilliseconds);
            }
            catch( Exception ex)
            {
                // don't throw settings...?
                LogHelper.Error<uSyncBackOffice>("Error starting usync", ex);
                throw ex;
            }
        }

        public void ImportAllFromDisk()
        {
            Stopwatch importStopWatch = new Stopwatch();
            importStopWatch.Start();

            long start = importStopWatch.ElapsedMilliseconds;
            LogHelper.Info<uSyncBackOffice>(">>>>>>>> Importing All items from disk");

            var e = uSyncBackOfficeSettings.Elements;

            if (e.Templates)
            {
                start = importStopWatch.ElapsedMilliseconds;
                LogHelper.Debug<uSyncBackOffice>("Importing: Templates");
                SyncTemplates.ImportAllFromDisk();
                LogHelper.Info<uSyncBackOffice>("Imported Templates ({0} ms)",
                    () => (importStopWatch.ElapsedMilliseconds - start));
            }

            if (e.Stylesheets)
            {
                start = importStopWatch.ElapsedMilliseconds;
                LogHelper.Debug<uSyncBackOffice>("Importing: Stylesheets");
                SyncStylesheets.ImportAllFromDisk();
                LogHelper.Info<uSyncBackOffice>("Imported Stylesheets ({0} ms)",
                    () => (importStopWatch.ElapsedMilliseconds - start));
            }

            if (e.DataTypes)
            {
                start = importStopWatch.ElapsedMilliseconds;
                LogHelper.Debug<uSyncBackOffice>("Importing: DataTypes");
                SyncDataTypes.ImportAllFromDisk();
                LogHelper.Info<uSyncBackOffice>("Imported DataTypes ({0} ms)",
                    () => (importStopWatch.ElapsedMilliseconds - start));
            }


            if (e.DocumentTypes)
            {
                start = importStopWatch.ElapsedMilliseconds;
                LogHelper.Debug<uSyncBackOffice>("Importing: DocumentTypes");
                SyncContentTypes.ImportAllFromDisk();
                LogHelper.Info<uSyncBackOffice>("Imported DocumentTypes ({0} ms)",
                    () => (importStopWatch.ElapsedMilliseconds - start));
            }

            if (e.Macros)
            {
                start = importStopWatch.ElapsedMilliseconds;
                LogHelper.Debug<uSyncBackOffice>("Importing: Macros");
                SyncMacros.ImportAllFromDisk();
                LogHelper.Info<uSyncBackOffice>("Imported Macros ({0} ms)",
                    () => (importStopWatch.ElapsedMilliseconds - start));
            }

            if (e.MediaTypes)
            {
                start = importStopWatch.ElapsedMilliseconds;
                LogHelper.Debug<uSyncBackOffice>("Importing: MediaTypes");
                SyncMediaTypes.ImportAllFromDisk();
                LogHelper.Info<uSyncBackOffice>("Imported MediaTypes ({0} ms)",
                    () => (importStopWatch.ElapsedMilliseconds - start));
            }

            if (e.Dictionary)
            {
                start = importStopWatch.ElapsedMilliseconds;
                LogHelper.Debug<uSyncBackOffice>("Importing: Languages");
                SyncLanguage.ImportAllFromDisk();
                LogHelper.Debug<uSyncBackOffice>("Importing: Dictionary items");
                SyncDictionaryItems.ImportAllFromDisk();
                LogHelper.Info<uSyncBackOffice>("Imported Dictionary & Languages ({0} ms)",
                    () => (importStopWatch.ElapsedMilliseconds - start));
            }

            importStopWatch.Stop();
            LogHelper.Info<uSyncBackOffice>("<<<<<<<< Import Complete {0} ms", () => importStopWatch.ElapsedMilliseconds);
        }

        public void AttachToEvents()
        {
            var e = uSyncBackOfficeSettings.Elements;

            if ( e.Templates ) 
                SyncTemplates.AttachToEvents();

            if (e.Stylesheets)
                SyncStylesheets.AttachToEvents();

            if (e.DataTypes) 
                SyncDataTypes.AttachToEvents();

            if (e.DocumentTypes) 
                SyncContentTypes.AttachToEvents();

            if (e.Macros) 
                SyncMacros.AttachToEvents();

            if (e.MediaTypes)
                SyncMediaTypes.AttachToEvents();

            if (e.Dictionary) {
                SyncLanguage.AttachToEvents();
                SyncDictionaryItems.AttachToEvents();
            }
        }

        public void ExportAllToDisk()
        {
            var e = uSyncBackOfficeSettings.Elements;

            if (e.Templates)
                SyncTemplates.SaveAllToDisk();

            if (e.Stylesheets)
                SyncStylesheets.SaveAllToDisk();

            if (e.DataTypes)
                SyncDataTypes.SaveAllToDisk();

            if (e.DocumentTypes)
                SyncContentTypes.SaveAllToDisk();

            if (e.Macros)
                SyncMacros.SaveAllToDisk();

            if (e.MediaTypes)
                SyncMediaTypes.SaveAllToDisk();

            if (e.Dictionary)
            {
                SyncLanguage.SaveAllToDisk();
                SyncDictionaryItems.SaveAllToDisk();
            }
        }


    }
}
