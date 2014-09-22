using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using Jumoo.uSync.Core.Models;

namespace Jumoo.uSync.Core
{
    /// <summary>
    ///  the core usync Engine - does the heavy work
    /// </summary>
    public class uSyncEngine
    {
        public uSyncContentType ContentType;
        public uSyncDataType DataType;
        public uSyncDictionary Dictionary;
        public uSyncLanguage Language;
        public uSyncMacro Macro;
        public uSyncMediaType MediaType;
        public uSyncStylesheet Stylesheet;
        public uSyncTemplate Template;
        public uSyncContent Content;
        public uSyncMedia Media;
        
        public uSyncEngine()
        {
            ContentType = new uSyncContentType();
            DataType = new uSyncDataType();
            Dictionary = new uSyncDictionary();
            Language = new uSyncLanguage();
            Macro = new uSyncMacro();
            MediaType = new uSyncMediaType();
            Stylesheet = new uSyncStylesheet();
            Template = new uSyncTemplate();
            Content = new uSyncContent();
            Media = new uSyncMedia();
        }

        /// <summary>
        ///  media files are diffrent - it's the only time
        ///  when we need to tell the engine the folder path
        ///  
        ///  so this is seperate - that way you can do other
        ///  things without ever needed to specify a folder
        ///  to the engine.
        /// </summary>
        public uSyncMediaFiles MediaFileHandler(string folder)
        {
            return new uSyncMediaFiles(folder);
        }
    }


}
