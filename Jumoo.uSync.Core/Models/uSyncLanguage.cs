using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.Core.Extensions;

using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core.Models
{
    public class uSyncLanguage: IUSyncCoreBase<Language>
    {
        public Language Import(XElement node, bool forceUpdate = false)
        {
            XmlNode legacyNode = node.ToXmlNode("Language");
            if ( legacyNode != null )
            {
                umbraco.cms.businesslogic.language.Language l =
                    umbraco.cms.businesslogic.language.Language.Import(legacyNode);

                // convert back to the new one...                

            }
            return null;
        }

        public XElement Export(Language item)
        {
            umbraco.cms.businesslogic.language.Language legacyItem =
                new umbraco.cms.businesslogic.language.Language(item.Id);

            if ( legacyItem != null )
            {
                XmlDocument xmlDoc = uSyncXml.CreateXmlDoc();
                xmlDoc.AppendChild(legacyItem.ToXml(xmlDoc));

                XElement node = xmlDoc.ToXElement();
                return node;
            }

            return null; 
        }
    }
}
