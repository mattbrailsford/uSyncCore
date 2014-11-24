using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using System.IO;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.Core;

using Jumoo.uSync.BackOffice.Helpers;

namespace Jumoo.uSync.BackOffice
{
    public class SyncContentTypes : SyncItemBase
    {
        static Dictionary<string, XElement> updated; 

        public static void ImportAllFromDisk()
        {
            var path = Path.Combine(uSyncBackOfficeSettings.Folder, "DocumentType");
            updated = new Dictionary<string, XElement>();

            ImportFromFolder(path);

            SecondPassFitAndFix();
        }

        private static void ImportFromFolder(string path)
        {
            if ( !Directory.Exists(path))
                return ;

            foreach (var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);
                if (node != null)
                {
                    LogHelper.Debug<SyncContentTypes>("Importing: {0}", ()=> file);
                    var item = _engine.ContentType.Import(node);
                    if ( item != null && !updated.ContainsKey(item.Alias) )
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
            foreach(KeyValuePair<String, XElement> update in updated)
            {
                XElement node = update.Value;

                if ( node != null)
                {
                    var item = ApplicationContext.Current.Services.ContentTypeService.GetContentType(update.Key);

                    if (item != null)
                    {
                        _engine.ContentType.ImportAgain((ContentType)item, node);
                    }
                }
            }
        }

        public static void SaveAllToDisk()
        {
            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;

            foreach(var item in _contentTypeService.GetAllContentTypes())
            {
                if (item != null)
                {
                    SaveToDisk(item);
                }
            }
        }

        public static void SaveToDisk(IContentType item, string root = null)
        {
            if (item != null)
            {
                try
                {
                    if (string.IsNullOrEmpty(root))
                        root = uSyncBackOfficeSettings.Folder;

                    var xml = _engine.ContentType.Export((ContentType)item);
                    uSyncIO.SaveXmlToDisk(xml, ItemSavePath(item), "def", "DocumentType", root); 
                }
                catch( Exception ex)
                {
                    LogHelper.Error<SyncContentTypes>("Error Saving Document type", ex);
                }
            }
        }

        /// <summary>
        ///  works out the save path for a contenttype.
        /// </summary>
        public static string ItemSavePath(IContentType item)
        {
            string path = "";
            if ( item != null)
            {
                if ( item.ParentId != 0 )
                {
                    var parent = ApplicationContext.Current.Services.ContentTypeService.GetContentType(item.ParentId);
                    if (parent != null)
                    {
                        path = ItemSavePath(parent);
                    }
                }
                path = Path.Combine(path, uSyncIO.ScrubFileName(item.Alias));
            }

            return path; 
        }

        public static void AttachToEvents()
        {
            ContentTypeService.DeletingContentType += ContentTypeService_DeletingContentType;
            ContentTypeService.SavedContentType += ContentTypeService_SavedContentType;
        }

        static void ContentTypeService_SavedContentType(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<IContentType> e)
        {
            LogHelper.Info<ContentType>("Content Type Saved");
            foreach (var doctype in e.SavedEntities)
            {
                SaveToDisk(doctype);
            }
        }

        static void ContentTypeService_DeletingContentType(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IContentType> e)
        {
            LogHelper.Info<ContentType>("Content Type Deleted");
            foreach (var doctype in e.DeletedEntities)
            {
                uSyncIO.ArchiveFile(ItemSavePath(doctype), "def", "DocumentType");
            }
        }
    }
}
