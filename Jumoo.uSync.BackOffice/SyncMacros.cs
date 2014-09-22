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
    public class SyncMacros : SyncItemBase
    {
        public static void ImportAllFromDisk()
        {
            var path = Path.Combine(
                uSyncBackOfficeSettings.Folder, "Macro");

            if (!Directory.Exists(path))
                return;

            foreach(var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);

                if (node != null)
                    _engine.Macro.Import(node);
            }
        }

        public static void SaveAllToDisk()
        {
            var _macroService = ApplicationContext.Current.Services.MacroService;

            foreach(var item in _macroService.GetAll())
            {
                SaveToDisk(item);
            }
        }

        public static void SaveToDisk(IMacro item)
        {
            if ( item != null )
            {
                XElement node = _engine.Macro.Export(item);
                uSyncIO.SaveXmlToDisk(node, "Macro", item.Alias);
            }
        }

        public static void AttachToEvents()
        {
            Umbraco.Core.Services.MacroService.Saved += MacroService_Saved;
            Umbraco.Core.Services.MacroService.Deleted += MacroService_Deleted;
        }

        static void MacroService_Deleted(Umbraco.Core.Services.IMacroService sender, Umbraco.Core.Events.DeleteEventArgs<IMacro> e)
        {
            LogHelper.Info<SyncMacros>("Macro Deleted");
            foreach (var macro in e.DeletedEntities)
            {
                uSyncIO.ArchiveFile("macro", macro.Alias);
            }
        }

        static void MacroService_Saved(Umbraco.Core.Services.IMacroService sender, Umbraco.Core.Events.SaveEventArgs<IMacro> e)
        {
            LogHelper.Info<SyncMacros>("Macro saved");
            foreach (var macro in e.SavedEntities)
            {
                SaveToDisk(macro);
            }
        }
    }
}
