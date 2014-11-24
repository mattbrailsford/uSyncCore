using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Services;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.Core.Extensions;

// still legacy on the media types
using umbraco;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms.businesslogic.media;
using umbraco.cms.businesslogic.propertytype;
using umbraco.DataLayer;
using umbraco.cms.businesslogic.template;
using umbraco.BusinessLogic;

using Jumoo.uSync.Core.Helpers;


namespace Jumoo.uSync.Core.Models
{
    public class uSyncMediaType : IUSyncCoreBase<Umbraco.Core.Models.IMediaType>, IUSyncCoreTwoPass<Umbraco.Core.Models.IMediaType>
    {
        public Umbraco.Core.Models.IMediaType Import(XElement node, bool forceUpdate = false)
        {
            XmlNode legacyNode = node.ToXmlNode("MediaType");
            if ( legacyNode != null )
            {
                // do the import...
                var mt = legacyNode.ImportMediaType(false);

                // need to do some convert back to new 
                if ( mt != null) {
                    var newMediaType = ApplicationContext.Current.Services.ContentTypeService.GetMediaType(mt.Id);
                    return newMediaType;
                }

            }
            return null;
        }

        public Umbraco.Core.Models.IMediaType ImportAgain(Umbraco.Core.Models.IMediaType item, XElement node)
        {
            XmlNode legacyNode = node.ToXmlNode("MediaType");
            if (legacyNode != null)
            {
                legacyNode.ImportMediaType(true);
            }
            return null;

        }

        public XElement Export(Umbraco.Core.Models.IMediaType item)
        {
            var legacyItem = MediaType.GetByAlias(item.Alias);

            XmlDocument xmlDoc = uSyncXml.CreateXmlDoc();
            xmlDoc.AppendChild(legacyItem.ToXml(xmlDoc));

            XElement node = xmlDoc.ToXElement();
            return node;
        }
    }
}
 