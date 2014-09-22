using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using Jumoo.uSync.Core.Extensions;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.Core.Helpers;

using System.Text.RegularExpressions;


namespace Jumoo.uSync.Core.Models
{
    public class uSyncIContentBase
    {        
        public XElement uSyncIContentExportBase(IContentBase item, string type)
        {
            var node = new XElement(type);

            Guid sourceGuid = NodeIdMapper.GetSourceGuid(item.Key);

            node.Add(new XAttribute("guid", sourceGuid));
            node.Add(new XAttribute("id", item.Id));
            node.Add(new XAttribute("nodeName", item.Name));
            node.Add(new XAttribute("isDoc", ""));
            node.Add(new XAttribute("updated", item.UpdateDate));

            foreach (var prop in item.Properties.Where(p => p != null))
            {
                XElement propNode = null;

                try
                {
                    propNode = prop.ToXml();
                }
                // sometime you can't serialize the property
                catch
                {
                    propNode = new XElement(prop.Alias, prop.Value);
                }

                string xml = "";
                xml = GetExportIds(GetInnerXML(propNode));

                var updatedNode = XElement.Parse(
                    String.Format("<{0}>{1}</{0}>", propNode.Name.ToString(), xml),
                    LoadOptions.PreserveWhitespace);

                node.Add(updatedNode);
            }

            return node;
        }

        private string GetExportIds(string value)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            foreach(Match m in Regex.Matches(value, @"\d{1,9}"))
            {
                int id;

                if(int.TryParse(m.Value, out id))
                {
                    Guid? localGuid = GetGuidFromId(id);
                    if ( localGuid != null )
                    {
                        if (!replacements.ContainsKey(m.Value))
                        {
                            Guid sourceGuid = NodeIdMapper.GetSourceGuid(localGuid.Value);
                            replacements.Add(m.Value, sourceGuid.ToString());
                        }
                    }
                }

            }

            foreach(KeyValuePair<string, string> pair in replacements)
            {
                value = value.Replace(pair.Key, pair.Value);
            }

            return value;
        }

        internal Guid? GetGuidFromId(int id)
        {
            var item = ApplicationContext.Current.Services.ContentService.GetById(id);
            if (item != null)
                return item.Key;

            var mediaItem = ApplicationContext.Current.Services.MediaService.GetById(id);
            if (mediaItem != null)
                return item.Key;

            return null;
        }


        /// <summary>
        ///  turns a Guid into a ID piece of content
        /// </summary>
        /// <param name="guid">the guid from a bit of conent (will be using a master guid)</param>
        /// <returns>the ID for the local content with that guid</returns>
        internal int GetIdFromGuid(Guid guid)
        {
            Guid sourceGuid = NodeIdMapper.GetTargetGuid(guid);

            var c = ApplicationContext.Current.Services.ContentService.GetById(sourceGuid);
            if (c != null)
                return c.Id;

            // try media 
            var m = ApplicationContext.Current.Services.MediaService.GetById(sourceGuid);
            if (m != null)
                return m.Id;

            return -1;

        }


        private string GetInnerXML(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }
    }
}
