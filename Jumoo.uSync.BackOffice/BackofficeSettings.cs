using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;

using Umbraco.Core.Logging;
using Umbraco.Core.IO;

namespace Jumoo.uSync.BackOffice
{
    public class uSyncBackOfficeSettings
    {
        private static string _settingsFile = "usync.config";
        private static uSyncBackOfficeSettingsSection _settings;
        private static Configuration config;

        static uSyncBackOfficeSettings()
        {
            try 
            {
                ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
                fileMap.ExeConfigFilename = IOHelper.MapPath(string.Format("~/config/{0}", _settingsFile));

                if (System.IO.File.Exists(fileMap.ExeConfigFilename))
                {
                    // load the settings file
                    config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

                    if (config != null)
                    {
                        _settings = (uSyncBackOfficeSettingsSection)config.GetSection("usync.backoffice");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Info<uSyncBackOfficeSettings>("Error loading settings file {0}", () => ex.ToString());
            }
            finally
            {

                if (_settings == null)
                {
                    LogHelper.Info<uSyncBackOfficeSettings>("WARNING: Working with no config file");
                    _settings = new uSyncBackOfficeSettingsSection(); // default config - won't be savable mind?
                }
            }
        }

        public static bool Import
        {
            get { return _settings.Import; }
        }

        public static bool ExportAtStartup
        {
            get { return _settings.ExportAtStartup; }
        }

        public static bool ExportOnSave
        {
            get { return _settings.ExportOnSave; }
        }

        public static bool WatchFolderForChanges
        {
            get { return _settings.WatchForChanges; }
        }

        public static bool ArchiveVersions
        {
            get { return _settings.ArchiveVersions; }
        }

        public static int MaxArchiveCount
        {
            get { return _settings.MaxArchiveCount; }
        }

        public static string ArchiveFolder
        {
            get { return Umbraco.Core.IO.IOHelper.MapPath(_settings.ArchiveFolder); }
        }

        public static string Folder
        {
            get { return Umbraco.Core.IO.IOHelper.MapPath(_settings.Folder); }
        }

        public static uSyncBackofficeElements Elements
        {
            get { return _settings.Elements; }
        }
    }

    public class uSyncBackOfficeSettingsSection : ConfigurationSection
    {

        [ConfigurationProperty("Import", DefaultValue = true, IsRequired = false)]
        public bool Import
        {
            get { return (bool)this["Import"]; }
            set { this["Import"] = value; }
        }

        [ConfigurationProperty("ExportAtStartup", DefaultValue = false, IsRequired = false)]
        public bool ExportAtStartup
        {
            get { return (bool)this["ExportAtStartup"]; }
            set { this["ExportAtStartup"] = value; }
        }

        [ConfigurationProperty("ExportOnSave", DefaultValue = true, IsRequired = false)]
        public bool ExportOnSave
        {
            get { return (bool)this["ExportOnSave"]; }
            set { this["ExportOnSave"] = value; }
        }

        [ConfigurationProperty("WatchForChanges", DefaultValue = false, IsRequired = false)]
        public bool WatchForChanges
        {
            get { return (bool)this["WatchForChanges"]; }
            set { this["WatchForChanges"] = value; }
        }


        [ConfigurationProperty("ArchiveVersions", DefaultValue = false, IsRequired = false)]
        public bool ArchiveVersions
        {
            get { return (bool)this["ArchiveVersions"]; }
            set { this["ArchiveVersions"] = value; }
        }

        [ConfigurationProperty("MaxArchiveCount", DefaultValue = 0, IsRequired = false)]
        public int MaxArchiveCount
        {
            get { return (int)this["MaxArchiveCount"]; }
            set { this["MaxArchiveCount"] = value; }
        }


        [ConfigurationProperty("Folder", DefaultValue = "~/uSync/", IsRequired = false)]
        public string Folder
        {
            get { return (string)this["Folder"]; }
        }
        
        [ConfigurationProperty("ArchiveFolder", DefaultValue = "~/uSync.Archive/", IsRequired = false)]
        public string ArchiveFolder
        {
            get { return (string)this["ArchiveFolder"]; }
        }

        [ConfigurationProperty("Elements", IsRequired = false)]
        public uSyncBackofficeElements Elements
        {
            get { return (uSyncBackofficeElements)this["Elements"]; }
        }

    }

    public class uSyncBackofficeElements : ConfigurationElement
    {
        [ConfigurationProperty("docTypes", DefaultValue = "true", IsRequired = true)]
        public Boolean DocumentTypes
        {
            get { return (Boolean)this["docTypes"]; }
        }

        [ConfigurationProperty("mediaTypes", DefaultValue = "true", IsRequired = true)]
        public Boolean MediaTypes
        {
            get { return (Boolean)this["mediaTypes"]; }
        }

        [ConfigurationProperty("dataTypes", DefaultValue = "true", IsRequired = true)]
        public Boolean DataTypes
        {
            get { return (Boolean)this["dataTypes"]; }
        }

        [ConfigurationProperty("templates", DefaultValue = "true", IsRequired = true)]
        public Boolean Templates
        {
            get { return (Boolean)this["templates"]; }
        }

        [ConfigurationProperty("stylesheets", DefaultValue = "true", IsRequired = true)]
        public Boolean Stylesheets
        {
            get { return (Boolean)this["stylesheets"]; }
        }

        [ConfigurationProperty("macros", DefaultValue = "true", IsRequired = true)]
        public Boolean Macros
        {
            get { return (Boolean)this["macros"]; }
        }

        [ConfigurationProperty("dictionary", DefaultValue = "false", IsRequired = false)]
        public Boolean Dictionary
        {
            get { return (Boolean)this["dictionary"]; }
        }
    }

}
