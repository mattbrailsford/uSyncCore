using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.Core;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;
using Umbraco.Core.Models;

using System.IO;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.BackOffice.Helpers;

using System.Timers;

namespace Jumoo.uSync.BackOffice
{
    public class SyncDataTypes : SyncItemBase
    {
        public static void ImportAllFromDisk()
        {
            var path = Path.Combine(uSyncBackOfficeSettings.Folder, "DataTypeDefinition");

            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);

                if (node != null)
                {
                    LogHelper.Debug<SyncDataTypes>("Importing DataType: {0}", ()=> node.Attribute("Name").Value);
                    _engine.DataType.Import(node);
                }
            }
        }

        public static void SaveAllToDisk()
        {
            var _dataTypeService = ApplicationContext.Current.Services.DataTypeService;

            foreach(var item in _dataTypeService.GetAllDataTypeDefinitions())
            {
                if ( item != null )
                {
                    SaveToDisk(item);
                }
            }
        }

        public static void SaveToDisk(IDataTypeDefinition item)
        {
            if ( item != null )
            {
                XElement node = _engine.DataType.Export((DataTypeDefinition)item);
                uSyncIO.SaveXmlToDisk(node, "DataTypeDefinition", item.Name);
            }
        }

        private static Timer _saveTimer;
        private static Queue<int> _saveQueue;
        private static object _saveLock;

        public static void AttachToEvents()
        {
            Umbraco.Core.Services.DataTypeService.Deleted += DataTypeService_Deleted;
            Umbraco.Core.Services.DataTypeService.Saved += DataTypeService_Saved;

            // delay trigger - used (upto and including umb 7.1.6
            // saved event on a datatype is called before prevalues
            // are saved - so we just wait a little while before 
            // we save our datatype... 
            //  not ideal but them's the breaks.
            //
            _saveTimer = new Timer(8128); // a perfect waiting time
            _saveTimer.Elapsed += _saveTimer_Elapsed;

            _saveQueue = new Queue<int>();
            _saveLock = new object();
        }

        static void DataTypeService_Saved(Umbraco.Core.Services.IDataTypeService sender, Umbraco.Core.Events.SaveEventArgs<IDataTypeDefinition> e)
        {
            LogHelper.Info<SyncDataTypes>("Data Type Saved");

            if ( e.SavedEntities.Count() > 0 )
            {
                // we lock so saves can't happen while we add to the queue.
                lock (_saveLock)
                {
                    // we reset the time, this means if two or more saves 
                    // happen close together, then they will be queued up
                    // only when no datatype saves have happened in the 
                    // timer elapsed period will saves start to happen.
                    //
                    _saveTimer.Stop();
                    _saveTimer.Start();

                    foreach (var item in e.SavedEntities)
                    {
                        _saveQueue.Enqueue(item.Id);
                    }
                }
            }
        }

        static void DataTypeService_Deleted(Umbraco.Core.Services.IDataTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IDataTypeDefinition> e)
        {
            LogHelper.Info<SyncDictionaryItems>("Dicitionary Item Deleted");
            foreach (var item in e.DeletedEntities)
            {
                uSyncIO.ArchiveFile("DataTypeDefinition", item.Name);
            }
        }

        static void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // we lock so things can't be added to the queue while we save them.
            // typically a save is ~ 50ms
            lock (_saveLock)
            {
                while (_saveQueue.Count > 0)
                {
                    var dataTypeService = ApplicationContext.Current.Services.DataTypeService;

                    int typeId = _saveQueue.Dequeue();

                    var item = dataTypeService.GetDataTypeDefinitionById(typeId);
                    if (item != null)
                    {
                        SaveToDisk(item);
                    }
                }
            }
        }

    }
}
