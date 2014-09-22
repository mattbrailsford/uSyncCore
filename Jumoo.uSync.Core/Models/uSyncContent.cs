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
    public class uSyncContent : uSyncIContentBase
    {
        public IContent Import(XElement node, bool forceUpdate = false, int parentId = -1)
        {
            bool newItem = false; 

            var guidNode = node.Attribute("guid");
            if (guidNode == null)
            {
                return null;
            }

            Guid contentGuid = new Guid(guidNode.Value);
            Guid _guid = NodeIdMapper.GetTargetGuid(contentGuid);
            
            var name = node.Attribute("nodeName").Value;
            var nodeType = node.Attribute("nodeTypeAlias").Value;
            var templateAlias = node.Attribute("templateAlias").Value;

            var sortOrder = int.Parse(node.Attribute("sortOrder").Value);
            var published = bool.Parse(node.Attribute("published").Value);

            // try to load the content
            var _contentService = ApplicationContext.Current.Services.ContentService;
            var item = _contentService.GetById(_guid);
            if (item == null)
            {
                item = _contentService.CreateContent(name, parentId, nodeType);
                newItem = true;
            }
            else
            {
                if (item.Trashed)
                {
                    // in the bin - we create a new one.
                    item = _contentService.CreateContent(name, parentId, nodeType);
                    newItem = true;
                }
                else
                {
                    // it's not new 
                    if (!forceUpdate)
                    {
                        // do some checking to see if we can skip an update
                        // for content we do this based on update date.
                        DateTime updateTime = DateTime.Now;
                        if (node.Element("updated") != null)
                        {
                            updateTime = DateTime.Parse(node.Element("updated").Value);
                        }

                        if (DateTime.Compare(updateTime, item.UpdateDate.ToLocalTime()) <= 0)
                        {
                            // no change
                            return item;
                        }
                    }
                }
            }

            // shouldn't happen but might if create fails for some reason
            if (item == null)
            {
                return null;
            }

            var template = ApplicationContext.Current.Services.FileService.GetTemplate(templateAlias);

            if (template != null)
                item.Template = template;

            item.SortOrder = sortOrder;
            item.Name = name;

            if (item.ParentId != parentId)
                item.ParentId = parentId;

            var props = from property in node.Elements()
                        where property.Attribute("isDoc") == null
                        select property;

            foreach (var prop in props)
            {
                var propTypeAlias = prop.Name.LocalName;
                if (item.HasProperty(propTypeAlias))
                {
                    item.SetValue(propTypeAlias, GetImportIds(GetImportXML(prop)));
                }
            }


            if (published)
            {
                var attempt = _contentService.SaveAndPublishWithStatus(item, 0, false);
                if (!attempt.Success)
                {
                    // didn't work for some reason
                    LogHelper.Info<uSyncContent>("Failed to publish {0}", () => attempt.Exception.ToString());
                }
            }
            else
            {
                _contentService.Save(item, 0, false);

                // if it was published - we want to unpublish
                if (item.Published)
                {
                    _contentService.UnPublish(item);
                }
            }

            if ( newItem )
            {
                // if it was a new item, we need to update our mapping 
                // tables
                NodeIdMapper.AddPair(contentGuid, item.Key);
            }

            return item;
        }

        /// <summary>
        ///  Takes the content, using the ID Mapping to search 
        ///  and replace and Ids it finds in the code
        /// </summary>
        /// <returns></returns>
        private string GetImportIds(string content)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            string guidRegEx = @"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b";  

            foreach (Match m in Regex.Matches(content, guidRegEx))
            {
                var id = GetIdFromGuid(Guid.Parse(m.Value));

                if ((id != -1) && (!replacements.ContainsKey(m.Value)))
                {
                    replacements.Add(m.Value, id.ToString());
                }

                // now loop through the replacements and add them

                foreach (KeyValuePair<string, string> pair in replacements)
                {
                    content = content.Replace(pair.Key, pair.Value);
                }
            }
            return content;
        }


        public XElement Export(IContent item)
        {
            var nodeName = item.ContentType.Alias;
            XElement node = base.uSyncIContentExportBase(item, nodeName);

            node.Add(new XAttribute("parentGUID", item.Level > 1 ? item.Parent().Key : new Guid("00000000-0000-0000-0000-000000000000")));
            node.Add(new XAttribute("nodeTypeAlias", item.ContentType.Alias));
            node.Add(new XAttribute("templateAlias", item.Template == null ? "" : item.Template.Alias));

            node.Add(new XAttribute("sortOrder", item.SortOrder));
            node.Add(new XAttribute("published", item.Published));

            return node;

        }

        public string GetImportXML(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            string xml = reader.ReadInnerXml();

            if (xml.StartsWith("<![CDATA["))
                return parent.Value;
            else
                return xml.Replace("&amp;", "&");
        }
    }

}