using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core.Models
{
    public class uSyncTemplate : IUSyncCoreBase<ITemplate>
    {
        public ITemplate Import(XElement node, bool quickUpdate = true)
        {
            if (node.Name.LocalName != "Template")
                throw new ArgumentException("Not a template import");


            if (quickUpdate)
            {
                LogHelper.Debug<uSyncTemplate>(">> Checking Hash values");
                if (!ChangeTracker.TemplateChanged(node))
                    return null;
            }
            LogHelper.Debug<uSyncTemplate>("Importing Template");
            var packagingService = ApplicationContext.Current.Services.PackagingService;

            var templates = packagingService.ImportTemplates(node);

            var template = templates.FirstOrDefault();
            if (template != null)
            {
                // master setting - doesn't appear to be a thing on the import so we do it here...
                if (node.Element("Master") != null && !string.IsNullOrEmpty(node.Element("Master").Value))
                {
                    var master = node.Element("Master");

                    var masterTemplate = ApplicationContext.Current.Services.FileService.GetTemplate(master.Value);

                    if (masterTemplate != null)
                    {
                        template.SetMasterTemplate(masterTemplate);
                        ApplicationContext.Current.Services.FileService.SaveTemplate(template);
                        LogHelper.Info<uSyncTemplate>("uSync has stepped in and set the master to {0}", () => masterTemplate.Alias);
                    }
                }
            }

            return template;

        }

        public XElement Export(ITemplate item)
        {
            var _packagingService = ApplicationContext.Current.Services.PackagingService;

            XElement node = _packagingService.Export(item);

            if (node.Elements("Master") == null)
            {
                int masterId = GetMasterId(item);
                if ( masterId > 0 )
                {
                    var master =
                        ApplicationContext.Current.Services.FileService.GetTemplate(masterId);

                    if ( master != null )
                    {
                        node.Add(new XElement("Master", master.Alias));
                    }
                }
            }
            node.AddMD5Hash(item.Alias + item.Name);

            return node;
        }

        /// <summary>
        ///  oldscool calls to get the master id
        /// </summary>
        private int GetMasterId(ITemplate item)
        {
            global::umbraco.cms.businesslogic.template.Template t =
                new umbraco.cms.businesslogic.template.Template(item.Id);

            if (t.MasterTemplate > 0)
                return t.MasterTemplate;

            return -1;
        }
    }
}
