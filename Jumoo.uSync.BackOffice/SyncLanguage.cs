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
    public class SyncLanguage : SyncItemBase
    {
        public static void ImportAllFromDisk()
        {
            var path = Path.Combine(uSyncBackOfficeSettings.Folder, "Language");

            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);

                if (node != null)
                {
                    _engine.Language.Import(node);
                }
            }
        }

        public static void SaveAllToDisk()
        {
            var _localService = ApplicationContext.Current.Services.LocalizationService;

            foreach(var lang in _localService.GetAllLanguages())
            {
                SaveToDisk(lang);
            }
        }

        public static void SaveToDisk(ILanguage item)
        {
           if ( item != null )
           {
               XElement node = _engine.Language.Export((Language)item);
               uSyncIO.SaveXmlToDisk(node, "Language", item.CultureName);
           }
        }

        public static void AttachToEvents()
        {
            /*
            Umbraco.Core.Services.LocalizationService.SavedLanguage += LocalizationService_SavedLanguage;
            Umbraco.Core.Services.LocalizationService.DeletedLanguage += LocalizationService_DeletedLanguage;
             */
            umbraco.cms.businesslogic.language.Language.New += Language_New;
            umbraco.cms.businesslogic.language.Language.AfterSave +=Language_AfterSave;
            umbraco.cms.businesslogic.language.Language.AfterDelete += Language_AfterDelete;
        }

        static void Language_AfterDelete(umbraco.cms.businesslogic.language.Language sender, umbraco.cms.businesslogic.DeleteEventArgs e)
        {
            uSyncIO.ArchiveFile("Language", sender.CultureAlias);
        }

        static void Language_AfterSave(umbraco.cms.businesslogic.language.Language sender, umbraco.cms.businesslogic.SaveEventArgs e)
        {
            var lang = ApplicationContext.Current.Services.LocalizationService.GetLanguageByCultureCode(sender.CultureAlias);
            if (lang != null)
                SaveToDisk(lang);
        }

        static void Language_New(umbraco.cms.businesslogic.language.Language sender, umbraco.cms.businesslogic.NewEventArgs e)
        {
            var lang = ApplicationContext.Current.Services.LocalizationService.GetLanguageByCultureCode(sender.CultureAlias);
            if (lang != null)
                SaveToDisk(lang);
        }
        
        /*
        static void LocalizationService_DeletedLanguage(Umbraco.Core.Services.ILocalizationService sender, Umbraco.Core.Events.DeleteEventArgs<ILanguage> e)
        {
            LogHelper.Info<SyncLanguage>("Lanaguage Deleted");
            foreach (var item in e.DeletedEntities)
            {
                uSyncIO.ArchiveFile("Language", item.CultureName);
            }
        }
        static void LocalizationService_SavedLanguage(Umbraco.Core.Services.ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<ILanguage> e)
        {
            LogHelper.Info<SyncLanguage>("Lanaguage Saved");
            foreach (var item in e.SavedEntities)
            {
                SaveToDisk(item);
            }
        }
        */
    }
}
