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

//
// Stylesheets are still done using the old API (for now)
// (but the events that fire are from the new api)
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.web;
using umbraco.BusinessLogic;

using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core.Models
{
    public class uSyncStylesheet : IUSyncCoreBase<Stylesheet>
    {
        public Stylesheet Import(XElement node, bool forceUpdate = false)
        {
            if (node.Name.LocalName != "Stylesheet" ) 
                throw new ArgumentException("Not a single stylesheet import");

            if (!forceUpdate)
            {
                LogHelper.Debug<uSyncContentType>(">> Checking Hash values");
                if (!ChangeTracker.StylesheetChanged(node))
                    return null;
            }

            XmlNode legacyNode = node.ToXmlNode("Stylesheet");
            if ( legacyNode != null )
            {
                User user = new User(0);
                StyleSheet styleSheet = StyleSheet.Import(legacyNode, user);

                // return the new one back...
                
            }

            return null;
        }

        public XElement Export(Stylesheet item)
        {
            // go from new to old.
            var legacyItem = StyleSheet.GetStyleSheet(item.Id, false, false);

            XmlDocument xmlDoc = uSyncXml.CreateXmlDoc();
            xmlDoc.AppendChild(legacyItem.ToXml(xmlDoc));

            XElement node = xmlDoc.ToXElement();
            node.AddMD5Hash();

            return node; 
        }
    }
}
