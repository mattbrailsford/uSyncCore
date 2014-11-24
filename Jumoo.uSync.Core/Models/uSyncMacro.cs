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

namespace Jumoo.uSync.Core.Models
{
    public class uSyncMacro : IUSyncCoreBase<IMacro>
    {
        public IMacro Import(XElement node, bool forceUpdate = false)
        {
            var _packagingService = ApplicationContext.Current.Services.PackagingService;
            var macros = _packagingService.ImportMacros(node);

            var _macroService = ApplicationContext.Current.Services.MacroService;

            // packaging service doesn't actually update much for macros.
            foreach(var macro in macros )
            {
                macro.Name = node.Element("name").Value;
                macro.ControlType = node.Element("scriptType").Value;
                macro.ControlAssembly = node.Element("scriptAssembly").Value;
                macro.XsltPath = node.Element("xslt").Value;
                macro.ScriptPath = node.Element("scriptingFile").Value;

                /// these properties don't always get written out in an export
                macro.UseInEditor = node.Element("useInEditor").ValueOrDefault(false);
                macro.CacheDuration = node.Element("refreshRate").ValueOrDefault(0);
                macro.CacheByMember = node.Element("cacheByMember").ValueOrDefault(false);
                macro.CacheByPage = node.Element("cacheByPage").ValueOrDefault(false);
                macro.DontRender = node.Element("dontRender").ValueOrDefault(true);

                // update properties
                // package service adds new ones, 
                // we just need to update and remove

                var properties = node.Elements("properties");
                if (properties != null)
                {
                    foreach(var property in properties.Elements())
                    {
                        var propAlias = property.Attribute("alias").Value;
                        var prop = macro.Properties.First(x => x.Alias == propAlias);

                        if ( prop != null )
                        {
                            prop.Name = property.Attribute("name").Value;
                            prop.EditorAlias = property.Attribute("propertyType").Value;
                        }
                    }
                }

                // remove
                List<string> propertiesToRemove = new List<string>();

                foreach(var currentProp in macro.Properties)
                {
                    XElement propNode = node.Element("properties")
                                            .Elements("property")
                                            .Where(x => x.Attribute("alias").Value == currentProp.Alias)
                                            .SingleOrDefault();

                    if ( propNode == null)
                    {
                        // remove this one
                        propertiesToRemove.Add(currentProp.Alias);
                    }
                }

                foreach(string alias in propertiesToRemove)
                {
                    macro.Properties.Remove(alias);
                }

                // save
                _macroService.Save(macro);
            }

            return null;
        }

        public XElement Export(IMacro item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            var _packagingService = ApplicationContext.Current.Services.PackagingService;

            XElement node = _packagingService.Export(item);

            return node;
        }
    }
}
