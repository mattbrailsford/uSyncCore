using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.Core;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;
using Umbraco.Core.Models;

using System.IO;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.BackOffice.Helpers;

namespace Jumoo.uSync.BackOffice
{
    public class SyncMediaTypes : SyncItemBase
    {
        static Dictionary<string, XElement> updated;

        public static void ImportAllFromDisk()
        {
            var path = Path.Combine(uSyncBackOfficeSettings.Folder, "MediaType");

            updated = new Dictionary<string, XElement>();

            ImportFromFolder(path);
            
        }

        private static void ImportFromFolder(string path)
        {
            if (!Directory.Exists(path))
                return;

            foreach(var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);

                if ( node != null)
                {
                    var item = _engine.MediaType.Import(node);
                    if (item != null && !updated.ContainsKey(item.Alias))
                    {
                        updated.Add(item.Alias, node);
                    }
                }
            }

            foreach(string folder in Directory.GetDirectories(path))
            {
                ImportFromFolder(folder);
            }
        }

        private static void SecondPassFitAndFix()
        {
            foreach (KeyValuePair<String, XElement> update in updated)
            {
                XElement node = update.Value;

                if (node != null)
                {
                    var item = ApplicationContext.Current.Services.ContentTypeService.GetMediaType(update.Key);

                    if ( item != null )
                    {
                        _engine.MediaType.ImportAgain(item, node);
                    }
                }
            }
        }


        public static void SaveAllToDisk()
        {
            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;

            foreach(var mediaType in _contentTypeService.GetAllMediaTypes())
            {
                SaveToDisk(mediaType);
            }

        }

        public static void SaveToDisk(IMediaType item)
        {
            if ( item != null )
            {
                XElement node = _engine.MediaType.Export(item);
                uSyncIO.SaveXmlToDisk(node, GetMediaPath(item), item.Alias, "MediaType");
            }
        }

        public static void AttachToEvents()
        {
            Umbraco.Core.Services.ContentTypeService.DeletedMediaType += ContentTypeService_DeletedMediaType;
            Umbraco.Core.Services.ContentTypeService.SavedMediaType += ContentTypeService_SavedMediaType;
        }

        static void ContentTypeService_SavedMediaType(Umbraco.Core.Services.IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<IMediaType> e)
        {
            LogHelper.Info<SyncMediaTypes>("MediaType Saved");
            foreach (var item in e.SavedEntities)
            {
                SaveToDisk(item);
            }
        }

        static void ContentTypeService_DeletedMediaType(Umbraco.Core.Services.IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IMediaType> e)
        {
            LogHelper.Info<SyncMediaTypes>("MediaType Deleted");
            foreach (var item in e.DeletedEntities)
            {
                uSyncIO.ArchiveFile(GetMediaPath(item), item.Alias, "MediaType");
            }
        }

        private static string GetMediaPath(IMediaType item)        
        {
            string path = "";

            if (item != null)
            {
                // does this documentType have a parent 
                if (item.ParentId > 0)
                {
                    var parent = ApplicationContext.Current.Services.ContentTypeService.GetMediaType(item.ParentId);

                    if ( parent != null )
                        path = GetMediaPath(parent);
                }

                // buld the final path (as path is "" to start with we always get
                // a preceeding '/' on the path, which is nice
                path = string.Format(@"{0}\{1}", path, uSyncIO.ScrubFileName(item.Alias));
            }

            return path;
        }

    }
}
