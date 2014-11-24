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
    /// <summary>
    ///  imports and export ContentTypes (Document Types to you and me)
    /// </summary>
    public class uSyncContentType : IUSyncCoreBase<ContentType>, IUSyncCoreTwoPass<ContentType>
    {
        public ContentType Import(XElement node, bool forceUpdate = false)
        {
            if (node.Name.LocalName != "DocumentType")
                throw new ArgumentException("We only import single DocumentTypes one at a time");

            if (!forceUpdate)
            {
                if (!ChangeTracker.ContentTypeChanged(node))
                    return null;
            }

            LogHelper.Debug<uSyncContentType>(">> Changes Detected");

            var _packagingService = ApplicationContext.Current.Services.PackagingService;
            IEnumerable<IContentType> contentTypes = _packagingService.ImportContentTypes(node);

            if (contentTypes.Count() == 1)
                return (ContentType)contentTypes.First();

            return null;            
        }

        //
        // a second pass import
        // 
        public ContentType ImportAgain(ContentType item, XElement node)
        {
            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;

            ImportContainerTypes(item, node);
            ImportStructure(item, node);
            ImportRemoveMissingProperties(item, node);
            ImportTabSortOrder(item, node);

            _contentTypeService.Save(item);

            return item;
        }

        private void ImportContainerTypes(ContentType item, XElement node)
        {
            XElement Info = node.Element("Info");

            if (Info != null)
            {
                XElement container = Info.Element("Container");
                if (container != null)
                {
                    bool isContainer = false;
                    bool.TryParse(container.Value, out isContainer);
                    item.IsContainer = isContainer;
                }
            }
        }

        private void ImportStructure(ContentType item, XElement node)
        {
            XElement structure = node.Element("Structure");

            List<ContentTypeSort> allowed = new List<ContentTypeSort>();
            int sortOrder = 0;

            foreach (var doctype in structure.Elements("DocumentType"))
            {
                string alias = doctype.Value;

                if (!string.IsNullOrEmpty(alias))
                {
                    var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;

                    IContentType aliasDoc = _contentTypeService.GetContentType(alias);

                    if (aliasDoc != null)
                    {
                        allowed.Add(new ContentTypeSort(
                            new Lazy<int>(() => aliasDoc.Id), sortOrder, aliasDoc.Name));
                        sortOrder++;
                    }
                }
            }
            item.AllowedContentTypes = allowed;

        }

        private void ImportRemoveMissingProperties(ContentType item, XElement node)
        {
            List<string> propertiesToRemove = new List<string>();
            Dictionary<string, string> propertiesToMove = new Dictionary<string, string>();

            // go through the properties in the item
            foreach (var property in item.PropertyTypes)
            {
                // is this property in the xml ?
                XElement propertyNode = node.Element("GenericProperties")
                                            .Elements("GenericProperty")
                                            .Where(x => x.Element("Alias").Value == property.Alias)
                                            .SingleOrDefault();

                if (propertyNode == null)
                {
                    LogHelper.Debug<uSyncContentType>("Removing {0} from {1}", () => property.Alias, () => item.Name);
                    propertiesToRemove.Add(property.Alias);
                }
                else
                {
                    // at this point we write our properties over those 
                    // in the db - because the import doesn't do this 
                    // for existing items.
                    LogHelper.Debug<uSyncContentType>("Updating prop {0} for {1}", () => property.Alias, () => item.Alias);

                    var editorAlias = propertyNode.Element("Type").Value;

                    var _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
                    var dataTypeDefinition = _dataTypeService.GetDataTypeDefinitionByPropertyEditorAlias(editorAlias).FirstOrDefault();

                    property.Name = propertyNode.Element("Name").Value;
                    property.Description = propertyNode.Element("Description").Value;
                    property.Mandatory = propertyNode.Element("Mandatory").Value.ToLowerInvariant().Equals("true");
                    property.ValidationRegExp = propertyNode.Element("Validation").Value;

                    XElement sortOrder = propertyNode.Element("SortOrder");
                    if (sortOrder != null)
                        property.SortOrder = int.Parse(sortOrder.Value);

                    var tab = propertyNode.Element("Tab").Value;
                    if (!string.IsNullOrEmpty(tab))
                    {
                        var propGroup = item.PropertyGroups.First(x => x.Name == tab);

                        if (!propGroup.PropertyTypes.Any(x => x.Alias == property.Alias))
                        {
                            // if it's not in this prop group - we can move it it into it
                            LogHelper.Debug<uSyncContentType>("Moving {0} in {1} to {2}",
                                () => property.Alias, () => item.Name, () => tab);
                            propertiesToMove.Add(property.Alias, tab);
                        }
                    }
                }
            }
        }

        private void ImportTabSortOrder(ContentType item, XElement node)
        {
            XElement tabs = node.Element("Tabs");

            foreach (var tab in tabs.Elements("Tab"))
            {
                var tabName = tab.Element("Caption").Value;
                var sortOrder = tab.Element("SortOrder");

                if (sortOrder != null)
                {
                    if (!String.IsNullOrEmpty(sortOrder.Value))
                    {
                        var itemTab = item.PropertyGroups.FirstOrDefault(x => x.Name == tabName);
                        if (itemTab != null)
                        {
                            itemTab.SortOrder = int.Parse(sortOrder.Value);
                        }
                    }
                }
            }
        }
        
        public XElement Export(ContentType item)
        {
            //
            // it looks like and odd wrapper, but it means 
            // during the import we can call the FUllExport
            // function and then get a hashfor it. to
            // do the comparison for speed.
            //
            XElement node = ContentTypeFullExport(item);
        
            return node;
        }

        internal static XElement ContentTypeFullExport(ContentType item)
        {
            var _packagingService = ApplicationContext.Current.Services.PackagingService;
            var _conentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            XElement node = _packagingService.Export(item);

            if (node != null)
            {
                // is this a container (v7)
                if (node.Element("Info").Element("Container") == null)
                    node.Element("Info").Add(new XElement("Container", item.IsContainer.ToString()));

                // structure
                var structure = node.Element("Structure");
                structure.RemoveNodes();

                SortedList<int, ContentTypeSort> allowedTypes = new SortedList<int, ContentTypeSort>();
                foreach(var t in item.AllowedContentTypes)
                {
                    allowedTypes.Add(t.Id.Value, t);
                }                

                foreach(var allowedType in allowedTypes)
                {
                    var allowedItem = _conentTypeService.GetContentType(allowedType.Value.Id.Value);
                    structure.Add(new XElement(Constants.Packaging.DocumentTypeNodeName, allowedItem.Alias));
                    allowedItem.DisposeIfDisposable();
                }

                // sort order on tabs
                var tabs = node.Element("Tabs");
                foreach(var tab in item.PropertyGroups)
                {
                    XElement tabNode = tabs.Elements().First(x => x.Element("Id").Value == tab.Id.ToString());

                    if (tabNode != null)
                        tabNode.Add(new XElement("SortOrder", tab.SortOrder));
                }

                // sort order on properties
                var properties = node.Element("GenericProperties");
                foreach(var prop in item.PropertyTypes)
                {
                    XElement propNode = properties.Elements().First(x => x.Element("Alias").Value == prop.Alias);

                    if (propNode != null)
                        propNode.Add(new XElement("SortOrder", prop.SortOrder));
                }
            }
            return node;
        }
   }
}
