using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;
using Jumoo.uSync.Core.Extensions;

using System.Xml;
using System.Xml.Linq;

// dictionary items are legacy too.
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic;
using Jumoo.uSync.Core.Helpers;


namespace Jumoo.uSync.Core.Models
{
    public class uSyncDictionary : IUSyncCoreBase<Dictionary.DictionaryItem>
    {
        public Dictionary.DictionaryItem Import(XElement node, bool forceUpdate = false)
        {
            XmlNode legacyNode = node.ToXmlNode("DictionaryItem");
            if ( legacyNode != null )
            {
                Dictionary.DictionaryItem item = Dictionary.DictionaryItem.Import(legacyNode);
                if ( item != null )
                    item.Save();
                return item;
            }

            return null; 
        }

        public XElement Export(Dictionary.DictionaryItem item)
        {
            XmlDocument xmlDoc = uSyncXml.CreateXmlDoc();
            xmlDoc.AppendChild(item.ToXml(xmlDoc));

            XElement node = xmlDoc.ToXElement();
            node.AddMD5Hash();

            return node;            
        }
    }
}
