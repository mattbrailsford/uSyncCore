using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Core.Models;

using System.IO;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Models;

// for stylesheets
using umbraco.cms.businesslogic.web;


namespace Jumoo.uSync.Core.Helpers
{
    /// <summary>
    ///  looks at the hashvalues in and out of the xml 
    ///  to work out if the thing you are looking at 
    ///  is diffrent to the one inside umbraco
    ///  
    ///  Using the tracker during imports (quickUpdate = true)
    ///  makes them significantly faster, and shold always catch
    ///  changes, so is on by default in all calls.
    /// </summary>
    public class ChangeTracker
    {
        static IDataTypeService _dataTypeService;
        static IPackagingService _packagingService;
        static IFileService _fileService;
        static Dictionary<Guid, IDataTypeDefinition> _dataTypes; 

        public static bool DataTypeChanged(XElement node)
        {
            string fileHash = node.GetUSyncMD5Hash();
            if ( string.IsNullOrWhiteSpace(fileHash))
                return true;

            XAttribute defId = node.Attribute("Definition");
            if (defId == null)
                return true;

            if ( _dataTypeService == null )
                _dataTypeService = ApplicationContext.Current.Services.DataTypeService;

            if (_dataTypes == null )
            {
                // speed test - load all the datatypes once 
                // less db ? it's about 1.8s faster with default datatypes 
                // more so with more types?
                _dataTypes = new Dictionary<Guid, IDataTypeDefinition>();
                foreach(var dtype in _dataTypeService.GetAllDataTypeDefinitions())
                {
                    _dataTypes.Add(dtype.Key, dtype);
                }
            }

            if ( _packagingService == null )
                _packagingService = ApplicationContext.Current.Services.PackagingService;


            Guid defGuid = new Guid(defId.Value);
            if (!_dataTypes.ContainsKey(defGuid))
                return true;

            var item = _dataTypes[defGuid];
            XElement export = _packagingService.Export(item, false);
            if (export == null)
                return true;

            string dbHash = export.CalculateMD5Hash(true);

            LogHelper.Debug<ChangeTracker>("Comparing: [{0}] to [{1}]",
                () => fileHash, () => dbHash);

            return (!fileHash.Equals(dbHash));            
        }

        public static bool ContentTypeChanged(XElement node)
        {
            string fileHash = node.GetUSyncMD5Hash();
            if (string.IsNullOrWhiteSpace(fileHash))
                return true;

            XElement aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return true;

            var _contentTypeServices = ApplicationContext.Current.Services.ContentTypeService;
            var item = _contentTypeServices.GetContentType(aliasNode.Value);

            if (item == null)
                return true;

            XElement export = uSyncContentType.ContentTypeFullExport((ContentType)item);
            string dbHash = export.CalculateMD5Hash(false);

            LogHelper.Debug<ChangeTracker>("Comparing: [{0}] to [{1}]",
                () => fileHash, () => dbHash);

            if (!fileHash.Equals(dbHash))
            {
                LogHelper.Debug<ChangeTracker>("XML (For Existing Node)\n{0}", () => export.ToString());
            }

            return (!fileHash.Equals(dbHash));
       
        }

        public static bool StylesheetChanged(XElement node)
        {
            string fileHash = node.GetUSyncMD5Hash();
            if (string.IsNullOrWhiteSpace(fileHash))
                return true;

            XElement name = node.Element("Name");
            if (name == null)
                return true;

            var legacyItem = StyleSheet.GetByName(name.Value);
            if (legacyItem == null)
                return true;

            XmlDocument xmlDoc = uSyncXml.CreateXmlDoc();
            xmlDoc.AppendChild(legacyItem.ToXml(xmlDoc));
            XElement dbNode = xmlDoc.ToXElement();

            var dbHash = dbNode.CalculateMD5Hash(false);

            LogHelper.Debug<ChangeTracker>("Comparing: [{0}] to [{1}]",
                () => fileHash, () => dbHash);

            return (!fileHash.Equals(dbHash));            
        }

        public static bool TemplateChanged(XElement node)
        {
            string fileHash = node.GetUSyncMD5Hash();
            if (string.IsNullOrWhiteSpace(fileHash))
                return true;

            XElement alias = node.Element("Alias");
            if (alias == null)
                return true;

            if (_fileService == null)
                _fileService = ApplicationContext.Current.Services.FileService;
            
            var item = _fileService.GetTemplate(alias.Value);
            if (item == null)
                return true;

            string vals = item.Alias + item.Name;
            string dbHash = XElementExtensions.CalculateMD5Hash(vals);

            LogHelper.Debug<ChangeTracker>("Comparing: [{0}] to [{1}]",
                () => fileHash, () => dbHash);

            return (!fileHash.Equals(dbHash));
        }
    }
}
