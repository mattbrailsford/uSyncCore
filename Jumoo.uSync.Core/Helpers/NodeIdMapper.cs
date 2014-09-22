using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Linq;

using System.IO;

using Umbraco.Core.IO;

using System.Timers;

namespace Jumoo.uSync.Core.Helpers
{
    /// <summary>
    ///  handles the mapping of ids to and from
    ///  other sources used in content and media 
    ///  types
    /// </summary>
    public class NodeIdMapper
    {
        public static Dictionary<Guid, Guid> pairs = new Dictionary<Guid, Guid>();
        private static string _pairFile ;

        private static Timer _saveTimer;
        private static object _saveLock = new object();

        static NodeIdMapper()
        {
            // load the mapper..
            _pairFile = IOHelper.MapPath("~/App_Data/Temp/_usyncimport.xml");
            LoadPairFile();

            _saveTimer = new Timer(1000); // one second wait...
            _saveTimer.Elapsed += _saveTimer_Elapsed;
        }

        /// <summary>
        ///  timer fires if no changes happen after one second, means the 
        ///  xml file is saved one second after the last change.
        ///  
        ///  during a bulk update - we basically save at the end.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock(_saveLock)
            {
                _saveTimer.Stop();
                SavePairFile();
            }

        }

        public static void LoadPairFile()
        {
            pairs = new Dictionary<Guid, Guid>();

            if ( File.Exists(_pairFile))
            {
                XElement source = XElement.Load(_pairFile);

                var sourcePairs = source.Descendants("pair");
                foreach(var pair in sourcePairs)
                {
                    pairs.Add(
                        Guid.Parse(pair.Attribute("id").Value),
                        Guid.Parse(pair.Attribute("guid").Value));
                }
            }
        }
        
        public static void SavePairFile()
        {
            if (File.Exists(_pairFile))
                File.Delete(_pairFile);

            XElement saveNode = new XElement("content");
            foreach(KeyValuePair<Guid, Guid> pair in pairs)
            {
                XElement pairNode = new XElement("pair",
                            new XAttribute("id", pair.Key.ToString()),
                            new XAttribute("guid", pair.Value.ToString()));

                saveNode.Add(pairNode);
            }

            saveNode.Save(_pairFile);
        
        }

        public static Guid GetTargetGuid(Guid sourceGuid)
        {
            if (pairs.ContainsKey(sourceGuid))
                return pairs[sourceGuid];

            return sourceGuid;
        }

        public static Guid GetSourceGuid(Guid targetGuid)
        {
            if (pairs.ContainsValue(targetGuid))
                return pairs.FirstOrDefault(x => x.Value == targetGuid).Key;

            return targetGuid;
        }

        public static void AddPair(Guid id, Guid val)
        {
            if (pairs.ContainsKey(id))
                pairs.Remove(id);

            pairs.Add(id, val);

            lock (_saveLock)
            {
                _saveTimer.Start();
            }
        }

        public static void Remove(Guid id)
        {
            if (pairs.ContainsKey(id)) {
                pairs.Remove(id);
                lock (_saveLock)
                {
                    _saveTimer.Start();
                }
                return;
            }

            if (pairs.ContainsValue(id))
            {
                var key = pairs.FirstOrDefault(x => x.Value == id).Key;
                pairs.Remove(key);
                lock (_saveLock)
                {
                    _saveTimer.Start();
                }
            }
        }
    }
}
