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

using Umbraco.Core.IO;
using System.IO;

using Jumoo.uSync.Core.Helpers;

using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jumoo.uSync.Core.Models
{
    public class uSyncMedia : uSyncIContentBase
    {
        public IMedia Import(XElement node, bool forceUpdate = false, int parentId = -1)
        {
            bool _new = false;

            var guidNode = node.Attribute("guid");
            if (guidNode == null)
                return null;

            Guid mediaGuid = new Guid(guidNode.Value);
            
            Guid _guid = NodeIdMapper.GetTargetGuid(mediaGuid);

            string name = node.Attribute("nodeName").Value;
            string mediaAlias = node.Attribute("mediaTypeAlias").Value;

            DateTime updateDate = DateTime.Now;

            if (node.Attribute("updated") != null)
                updateDate = DateTime.Parse(node.Attribute("updated").Value);

            IMedia item = ApplicationContext.Current.Services.MediaService.GetById(_guid);
            if (item != null)
            {
                _new = true;
                item = ApplicationContext.Current.Services.MediaService.CreateMedia(name, parentId, mediaAlias);
            }
            else
            {
                if (item.Trashed)
                {
                    // trashed, so we create new one...
                    _new = true;
                    item = ApplicationContext.Current.Services.MediaService.CreateMedia(name, parentId, mediaAlias);
                }
                else
                {
                    // not new - we can do some is this updated stuff..

                    if ( !forceUpdate )
                    {
                        if ( DateTime.Compare(updateDate, item.UpdateDate.ToLocalTime()) < 0 ) 
                        {
                            // xml is older - don't the update
                            return item;
                        }
                    }

                }
            }

            if ( item != null )
            {
                // we do this which checks - because it stops
                // the properties getting marked as dirty when
                // their is no change - making it a bit faster
                if (item.Name != name)
                    item.Name = name;

                if (item.ParentId != parentId)
                    item.ParentId = parentId;

                // do the impressive file import here....
                // ImportMediaFile(mediaGuid, item);

                ApplicationContext.Current.Services.MediaService.Save(item);
            }

            if ( _new )
            {
                NodeIdMapper.AddPair(mediaGuid, item.Key);
            }

            return item;         
        }


        public XElement Export(IMedia item)
        {
            var nodeName = item.ContentType.Alias;

            XElement node = base.uSyncIContentExportBase(item, nodeName);

            node.Add(new XAttribute("parentGUID", item.Level > 1 ? item.Parent().Key : new Guid("")));
            node.Add(new XAttribute("mediaTypeAlias", item.ContentType.Alias));
            node.Add(new XAttribute("path", item.Path));

            foreach(var file in item.Properties.Where(p => p.Alias == "umbracoFile"))
            {
                if ( file == null || file.Value == null )
                {
                    // we don't have an associated file
                }
                else
                {
                    // export the file to the media folder to
                    // ExportMediaFile(file.Value.ToString(), NodeIdMapper.GetSourceGuid(item.Key));
                }

            }

            return node;

        }

        /// <summary>
        ///  gets the file and saves it in a mediafiles/guid folder...
        /// </summary>
        /// <param name="umbracoFile"></param>
        /// <param name="guid"></param>

    }


    public class uSyncMediaFiles
    {
        private string _mediaFolder;
        public uSyncMediaFiles (string folder)
        {
            _mediaFolder = folder;
        }


        private void ImportMediaFile(Guid guid, IMedia item)
        {
            string fileRoot = _mediaFolder;
            string folder = string.Format("{0}\\{1}", fileRoot, guid.ToString());

            if (!Directory.Exists(folder))
                return; // nothing to import

            FileInfo currentFile = null;

            if (item.HasProperty("umbracoFile"))
            {
                var umbracoFile = item.GetValue("umbracoFile");
                if (umbracoFile != null)
                {
                    string path = umbracoFile.ToString();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        if (IsJson(path))
                        {
                            path = JsonConvert.DeserializeObject<dynamic>(path).src;
                        }

                        if (path.StartsWith("/media/"))
                        {
                            string f = IOHelper.MapPath(string.Format("~{0}", path));
                            if (System.IO.File.Exists(f))
                            {
                                // get the current file info..... 
                                currentFile = new FileInfo(f);
                            }
                        }
                    }
                }

                foreach (var file in Directory.GetFiles(folder, "*.*"))
                {
                    if (currentFile != null)
                    {
                        if (!FilesAreEqual(currentFile, new FileInfo(file)))
                        {
                            string filename = Path.GetFileName(file);

                            FileStream s = new FileStream(file, FileMode.Open);
                            item.SetValue("umbracoFile", filename, s);
                            s.Close();

                            // OK so now we've gone and created the new file and
                            // folder in umbraco.
                            if (Directory.Exists(currentFile.DirectoryName))
                            {
                                Directory.Delete(currentFile.DirectoryName, true);
                            }
                        }
                    }
                    else
                    {
                        // we have no current file, that's because this is a 
                        // new file.
                        FileStream s = new FileStream(file, FileMode.Open);
                        item.SetValue("umbracoFile", Path.GetFileName(file), s);
                        s.Close();
                    }
                }

            }
        }

        const int BYTES_TO_READ = sizeof(Int64);

        private bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }

            return true;
        }

        private static bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}"))
                || (input.StartsWith("[") && input.EndsWith("]"));
        }

        private void ExportMediaFile(string umbracoFile, Guid guid)
        {
            string fileroot = _mediaFolder;
            string path = string.Empty;

            // umbraco 7 ness...
            if (IsJson(umbracoFile))
                path = JsonConvert.DeserializeObject<dynamic>(umbracoFile).src;
            else
                path = umbracoFile;

            string folder = Path.Combine(fileroot, guid.ToString());
            string dest = Path.Combine(folder, Path.GetFileName(path));
            string source = IOHelper.MapPath(string.Format("~{0}", path));

            if (System.IO.File.Exists(source))
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                if (System.IO.File.Exists(dest))
                    System.IO.File.Decrypt(dest);

                System.IO.File.Copy(source, dest);
            }
        }

    }
}
