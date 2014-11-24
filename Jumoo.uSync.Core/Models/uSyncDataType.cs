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

using System.Text.RegularExpressions;


namespace Jumoo.uSync.Core.Models
{
    public class uSyncDataType: IUSyncCoreBase<IDataTypeDefinition>
    {
        public IDataTypeDefinition Import(XElement node, bool forceUpdate = false)
        {

            var name = node.Name.LocalName;
            if (name != "DataType")
                throw new ArgumentException("Import only imports single DataTypes");

            if (!forceUpdate)
            {
                LogHelper.Debug<uSyncDataType>(">> Checking Hash values");
                if (!ChangeTracker.DataTypeChanged(node))
                {
                    return null;
                }
            }

            var _packagingService = ApplicationContext.Current.Services.PackagingService;
            var _dataTypeService = ApplicationContext.Current.Services.DataTypeService;

            // import the datatypes 
            // the return value isn't really to be trusted (only returns on a create?)
            var datatypes = _packagingService.ImportDataTypeDefinitions(node);

            // packaging service - only creates new stuff
            // we need to update package values ourselfs.
            var def = node.Attribute("Definition");
            if (def != null)
            {
                var dataTypeDefinitionId = new Guid(def.Value);
                var definition = _dataTypeService.GetDataTypeDefinitionById(dataTypeDefinitionId);
                if (definition != null)

                    LogHelper.Debug<uSyncDataType>(">> Going node hunting");
                // Node Hunting (replacing IDs of source to our target)
                var cNode = HuntContentNodes(node);

                // update
                LogHelper.Debug<uSyncDataType>(">> Updating preValues");
                UpdatePreValues(definition, node);
                return definition;
            }

            if (datatypes != null)
            {
                return datatypes.FirstOrDefault();
            }

            return null;

        }

        public XElement Export(IDataTypeDefinition item)
        {
            var _packagingService = ApplicationContext.Current.Services.PackagingService;

            XElement node = _packagingService.Export(item);
            // substitue.
            node = ReplaceCotentNodes(node);

            return node;

        }

        private void UpdatePreValues(IDataTypeDefinition item, XElement node)
        {
            var preValues = node.Element("PreValues");
            var dataTypeSerivce = ApplicationContext.Current.Services.DataTypeService;

            if (preValues != null)
            {
                var valuesWithoutKeys = preValues.Elements("PreValue")
                                                      .Where(x => ((string)x.Attribute("Alias")).IsNullOrWhiteSpace())
                                                      .Select(x => x.Attribute("Value").Value);

                var valuesWithKeys = preValues.Elements("PreValue")
                                                     .Where(x => ((string)x.Attribute("Alias")).IsNullOrWhiteSpace() == false)
                                                     .ToDictionary(key => (string)key.Attribute("Alias"), val => new PreValue((string)val.Attribute("Value")));

                dataTypeSerivce.SavePreValues(item.Id, valuesWithKeys);
                dataTypeSerivce.SavePreValues(item.Id, valuesWithoutKeys);
            }
        }

        #region Node Hunting (fixes MultiNodeTree Pickers and the like)
        /// <summary>
        ///  goes through the prevalues and makes content ids portable.
        /// </summary>
        private static XElement ReplaceCotentNodes(XElement node)
        {
            XElement nodepaths = null;

            var preValueRoot = node.Element("PreValues");
            if (preValueRoot.HasElements)
            {
                var preValues = preValueRoot.Elements("PreValue");
                foreach (var preValue in preValues)
                {
                    if (!((string)preValue.Attribute("Alias")).IsNullOrWhiteSpace())
                    {
                        // look for an alias name that contains a content node
                        if (uSyncCoreSettings.DataTypeSettings.PreValuesWithContentIds.Contains((string)preValue.Attribute("Alias")))
                        {
                            LogHelper.Debug<uSyncDataType>("Mapping Content Ids in PreValue {0}", () => preValue.Attribute("Alias"));
                            var propVal = (string)preValue.Attribute("Value");
                            if (!String.IsNullOrWhiteSpace(propVal))
                            {
                                foreach (Match m in Regex.Matches(propVal, @"\d{1,9}"))
                                {
                                    int id;

                                    if (int.TryParse(m.Value, out id))
                                    {
                                        // we have an ID : yippe, time to do some walking...
                                        string type = "content";

                                        ContentWalker cw = new ContentWalker();
                                        string nodePath = cw.GetPathFromID(id);

                                        // Didn't find the content id try media ...
                                        if (string.IsNullOrWhiteSpace(nodePath))
                                        {
                                            type = "media";
                                            MediaWalker mw = new MediaWalker();
                                            nodePath = mw.GetPathFromID(id);
                                        }

                                        if (!string.IsNullOrWhiteSpace(nodePath))
                                        {
                                            // attach the node tree to the XElement
                                            if (nodepaths == null)
                                            {
                                                nodepaths = new XElement("Nodes");
                                                node.Add(nodepaths);
                                            }
                                            nodepaths.Add(new XElement("Node",
                                                new XAttribute("Id", m.Value),
                                                new XAttribute("Value", nodePath),
                                                new XAttribute("Alias", (string)preValue.Attribute("Alias")),
                                                new XAttribute("Type", type)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return node;
        }

        /// <summary>
        ///  turns portable content ids back into static ones.
        /// </summary>
        private static XElement HuntContentNodes(XElement node)
        {
            var nodes = node.Element("Nodes");
            var preValues = node.Element("PreValues");

            if (nodes != null && preValues != null)
            {
                if (nodes.HasElements && preValues.HasElements)
                {
                    LogHelper.Debug<uSyncDataType>("Mapping PreValue Content Ids to Local Content Id Values");

                    foreach (var nodepath in nodes.Elements("Node"))
                    {
                        // go through the mapped things, and see if they are 
                        var alias = (string)nodepath.Attribute("Alias");
                        if (!String.IsNullOrWhiteSpace(alias))
                        {
                            // find the alias in the preValues...
                            var preVal = preValues.Elements().Where(x => (string)x.Attribute("Alias") == alias).FirstOrDefault();
                            if (preVal != null)
                            {
                                var preValVal = (string)preVal.Attribute("Value");
                                if (!string.IsNullOrWhiteSpace(preValVal))
                                {
                                    LogHelper.Debug<uSyncDataType>("We have the preValue (we think....) {0}", () => preValVal);

                                    var nodeidPath = (string)nodepath.Attribute("Value");
                                    var nodeid = (string)nodepath.Attribute("Id");

                                    if (!string.IsNullOrWhiteSpace(nodeidPath))
                                    {

                                        int id = -1;

                                        var nodeType = (string)nodepath.Attribute("Type");
                                        if (nodeType == "stylesheet")
                                        {
                                            // it's a stylesheet id - quick swapsy ...
                                            // the core api - doesn't do getStyleSheetByID... so we're not doing this just yet
                                            // 
                                        }
                                        else if (nodeType == "media")
                                        {
                                            LogHelper.Debug<uSyncDataType>("searching for a media node");
                                            MediaWalker mw = new MediaWalker();
                                            id = mw.GetIdFromPath(nodeidPath);
                                        }
                                        else
                                        {
                                            // content
                                            LogHelper.Debug<uSyncDataType>("searching for a content node");
                                            ContentWalker cw = new ContentWalker();
                                            id = cw.GetIdFromPath(nodeidPath);
                                        }

                                        if (id != -1)
                                        {
                                            // try to illiminate changes for changes sake. 
                                            if (preValVal.Contains(nodeid) && nodeid != id.ToString())
                                            {
                                                preVal.SetAttributeValue("Value", preValVal.Replace(nodeid, id.ToString()));
                                                LogHelper.Debug<uSyncDataType>("Set preValue value to {0}", () => preVal.Attribute("Value"));
                                            }
                                        }
                                        else
                                        {
                                            LogHelper.Debug<uSyncDataType>("We didn't match the pre-value so we're leaving it alone");
                                        }
                                    }
                                    else
                                    {
                                        LogHelper.Debug<uSyncDataType>("Couldn't retrieve nodeIdPath from Value");
                                    }

                                }
                            }

                        }
                    }
                }

            }


            return node;
        }

        #endregion
    }
}
