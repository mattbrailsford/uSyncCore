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
    public class SyncTemplates : SyncItemBase 
    {
        public static void ImportAllFromDisk()
        {
            string path = Path.Combine(uSyncBackOfficeSettings.Folder, "Template");
            ImportFromFolder(path);
        }

        private static void ImportFromFolder(string path)
        {
            if (!Directory.Exists(path))
                return;
            

            foreach (var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);
                if (node != null)
                {
                    var item = _engine.Template.Import(node);
                }
            }

            foreach (string folder in Directory.GetDirectories(path))
            {
                ImportFromFolder(folder);
            }

        }

        public static void SaveAllToDisk()
        {
            var fileService = ApplicationContext.Current.Services.FileService;

            try
            {
                foreach (Template item in fileService.GetTemplates())
                {
                    SaveToDisk(item);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info<SyncTemplates>("uSync: Error saving all templates {0}", () => ex.ToString());
            }
        }

        public static void SaveToDisk(Template item)
        {
            if ( item != null )
            {
                XElement node = _engine.Template.Export(item);
                uSyncIO.SaveXmlToDisk(node, GetTemplatePath(item), item.Alias, "Template");
            }
        }

        public static void AttachToEvents()
        {
            global::umbraco.cms.businesslogic.template.Template.AfterDelete += Template_AfterDelete;
            global::umbraco.cms.businesslogic.template.Template.AfterSave += Template_AfterSave;
            global::umbraco.cms.businesslogic.template.Template.New += Template_New; 
        }

        private static void Template_New(global::umbraco.cms.businesslogic.template.Template sender, global::umbraco.cms.businesslogic.NewEventArgs e)
        {
            LogHelper.Info<SyncTemplates>("Template New");
            var template = ApplicationContext.Current.Services.FileService.GetTemplate(sender.Alias);
            if (template != null)
                SaveToDisk((Template)template);
        }

        private static void Template_AfterSave(global::umbraco.cms.businesslogic.template.Template sender, global::umbraco.cms.businesslogic.SaveEventArgs e)
        {
            LogHelper.Info<SyncTemplates>("Template Saved");
            var template = ApplicationContext.Current.Services.FileService.GetTemplate(sender.Alias);
            if (template != null)
                SaveToDisk((Template)template);
        }

        private static void Template_AfterDelete(global::umbraco.cms.businesslogic.template.Template sender, global::umbraco.cms.businesslogic.DeleteEventArgs e)
        {
            LogHelper.Info<SyncTemplates>("Template Deleted");
            // to do - because i don't think we can swap old to new
            // here - after all it's just been deleted..
        }


        private static string GetTemplatePath(ITemplate item)
        {
            return GetDocPath(new global::umbraco.cms.businesslogic.template.Template(item.Id));
        }

        private static string GetDocPath(global::umbraco.cms.businesslogic.template.Template item)
        {
            string path = "";
            if (item != null)
            {
                if (item.MasterTemplate > 0)
                {
                    path = GetDocPath(new global::umbraco.cms.businesslogic.template.Template(item.MasterTemplate));
                }

                path = string.Format("{0}\\{1}", path, uSyncIO.ScrubFileName(item.Alias));
            }
            return path;
        }
    }
}
