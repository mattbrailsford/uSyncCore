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
    public class SyncStylesheets : SyncItemBase
    {

        public static void ImportAllFromDisk()
        {
            var path = Path.Combine(uSyncBackOfficeSettings.Folder, "Stylesheet");

            if (!Directory.Exists(path))
                return;

            foreach(var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);

                if ( node != null)
                {
                    _engine.Stylesheet.Import(node);
                }
            }
        }

        public static void SaveAllToDisk()
        {
            var _fileService = ApplicationContext.Current.Services.FileService;

            foreach(var stylesheet in _fileService.GetStylesheets())
            {
                SaveToDisk(stylesheet);
            }
        }

        public static void SaveToDisk(Stylesheet item)
        {
            if ( item != null )
            {
                XElement node = _engine.Stylesheet.Export(item);
                uSyncIO.SaveXmlToDisk(node, "Stylesheet", item.Alias);
            }
        }

        public static void AttachToEvents()
        {
            //Umbraco.Core.Services.FileService.SavedStylesheet += FileService_SavedStylesheet;
            //Umbraco.Core.Services.FileService.DeletedStylesheet += FileService_DeletedStylesheet;

            umbraco.cms.businesslogic.web.StyleSheet.AfterSave += StyleSheet_AfterSave;
            umbraco.cms.businesslogic.web.StyleSheet.BeforeDelete += StyleSheet_BeforeDelete;
        }

        static void StyleSheet_BeforeDelete(umbraco.cms.businesslogic.web.StyleSheet sender, umbraco.cms.businesslogic.DeleteEventArgs e)
        {
            uSyncIO.ArchiveFile("Stylesheet", sender.Text);
        }

        static void StyleSheet_AfterSave(umbraco.cms.businesslogic.web.StyleSheet sender, umbraco.cms.businesslogic.SaveEventArgs e)
        {
            var stylesheet = ApplicationContext.Current.Services.FileService.GetStylesheetByName(sender.Text);
            if (stylesheet != null)
                SaveToDisk(stylesheet);
        }

        /*
        static void FileService_DeletedStylesheet(Umbraco.Core.Services.IFileService sender, Umbraco.Core.Events.DeleteEventArgs<Stylesheet> e)
        {
            LogHelper.Info<SyncStylesheets>("Stylesheet Deleted");
            foreach (var stylesheet in e.DeletedEntities)
            {
                uSyncIO.ArchiveFile("Stylesheet", stylesheet.Alias);
            }
        }

        static void FileService_SavedStylesheet(Umbraco.Core.Services.IFileService sender, Umbraco.Core.Events.SaveEventArgs<Stylesheet> e)
        {
            LogHelper.Info<SyncStylesheets>("Stylesheet Saved");
            foreach (var stylesheet in e.SavedEntities)
            {
                SaveToDisk(stylesheet);
            }
        }
         */
    }
}
