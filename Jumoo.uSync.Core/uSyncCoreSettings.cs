using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

using Umbraco.Core.IO;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core
{
    public class uSyncCoreSettings
    {
        private static string _settingFile = "uSync.config";
        private static uSyncCoreSettingsSection _settings;
        private static Configuration config;

        static uSyncCoreSettings()
        {
            try
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = IOHelper.MapPath(string.Format("~/config/{0}", _settingFile));

                if (System.IO.File.Exists(fileMap.ExeConfigFilename))
                {
                    // load the settings file
                    config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                    if (config != null)
                    {
                        _settings = (uSyncCoreSettingsSection)config.GetSection("usync.core");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info<uSyncCoreSettings>("Error loading settings file {0}", () => ex.ToString());
            }
            finally
            {

                if (_settings == null)
                {
                    LogHelper.Info<uSyncCoreSettings>("WARNING: Working with no config file");
                    _settings = new uSyncCoreSettingsSection(); // default config - won't be savable mind?
                }
            }
        }

        public static void Save()
        {
            if ( _settings != null ) 
                _settings.CurrentConfiguration.Save(ConfigurationSaveMode.Full);
        }


        // core settings 
        public static DataTypeSettings DataTypeSettings
        {
            get {
                return _settings.DataTypeSettings;
            }
        }

    }


    public class uSyncCoreSettingsSection : ConfigurationSection
    {
        [ConfigurationProperty("DataTypes", IsRequired = false)]
        public DataTypeSettings DataTypeSettings {
            get { 
                return (DataTypeSettings)this["DataTypes"];
            }
        }
    }
    
    public class DataTypeSettings : ConfigurationElement
    {
        [ConfigurationProperty("PreValuesWithContentIds",DefaultValue = "startNode, startNodeId", IsRequired = false)]
        public string PreValuesWithContentIds{
            get {
                return (string)this["PreValuesWithContentIds"];
            }
        }
    }
}