using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core.IO;
using Umbraco.Core.Logging;

using System.IO;

using System.Xml;
using System.Xml.Linq;

namespace Jumoo.uSync.BackOffice.Helpers
{
    /// <summary>
    ///  helping us get stuff physically on and off the disk...
    /// </summary>
    public class uSyncIO
    {
        public static void SaveXmlToDisk(XElement node, string path, string filename, string itemType, string root)
        {
            var filepath = string.Format("{0}\\{1}\\{2}.config", itemType, path, ScrubFileName(filename));
            SaveXmlToDisk(node, filepath, root);
        }

        public static void SaveXmlToDisk(XElement node, string path, string filename, string root)
        {
            var filepath = string.Format("{0}\\{1}.config", path, ScrubFileName(filename));
            SaveXmlToDisk(node, filepath, root);
        }

        public static void SaveXmlToDisk(XElement node, string path, string root)
        {
            string targetFile = GetPhysicalFilePath(path, root);
            string folder = Path.GetDirectoryName(targetFile);

            LogHelper.Info<uSyncIO>("Saving {0}", () => targetFile);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            else if (File.Exists(targetFile))
            {
                if (uSyncBackOfficeSettings.ArchiveVersions)
                {
                    ArchiveFile(
                        Path.GetDirectoryName(path),
                        Path.GetFileNameWithoutExtension(path));
                }

                File.Delete(targetFile);
            }

            node.Save(targetFile);
        }

        public static string GetPhysicalFilePath(string path, string root)
        {
            return Path.Combine(root, path.Trim('\\'));
        }

        public static string ScrubFileName(string filename)
        {
            StringBuilder sb = new StringBuilder(filename);
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char item in invalid)
            {
                sb.Replace(item.ToString(), "");
            }

            return sb.ToString();
        }

        public static void ArchiveFile(string path, string filename, string itemType)
        {
            var filepath = string.Format("{0}\\{1}", path, itemType);
            ArchiveFile(filepath, filename );
        }

        public static void ArchiveFile(string path, string filename)
        {
            string currentFile =
                string.Format("{0}\\{1}\\{2}.config", 
                    uSyncBackOfficeSettings.Folder, path, ScrubFileName(filename));

            string archiveFile =
                string.Format("{0}\\{1}\\{2}_{3}.config",
                    uSyncBackOfficeSettings.ArchiveFolder, path,
                    ScrubFileName(filename), DateTime.Now.ToString("ddMMyy_HHmmss"));

            try
            {

                // we need to confirm the archive directory exists 
                if (!Directory.Exists(Path.GetDirectoryName(archiveFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(archiveFile));
                }

                if (File.Exists(currentFile))
                {
                    // it shouldn't happen as we are going for a unique name
                    // but it might be called twice v'quickly

                    if (File.Exists(archiveFile))
                    {
                        File.Delete(archiveFile);
                    }

                    // 
                    File.Copy(currentFile, archiveFile);
                    // OnPreDelete(new XmlDocFileEventArgs(currentFile));
                    File.Delete(currentFile);
                    // OnDeleted(new XmlDocFileEventArgs(currentFile));

                    LogHelper.Info<uSyncIO>("Archived [{0}] to [{1}]", () => currentFile, () => archiveFile);

                    // latest n? - only keep last n in the folder?
                    if (uSyncBackOfficeSettings.MaxArchiveCount > 0)
                    {
                        ClenseArchiveFolder(Path.GetDirectoryName(archiveFile), uSyncBackOfficeSettings.MaxArchiveCount);
                    }
                }
            }
            catch (Exception ex)
            {
                // archive is a non critical thing - if it fails we are not stopping
                // umbraco, but we are going to log that it didn't work. 
                // Log.Add(LogTypes.Error, 0, "Failed to archive") ; 
                // to do some dialog popup text like intergration
                LogHelper.Info<uSyncIO>("Failed to Archive {0} {1}", () => filename, ()=> ex.ToString());
            }
        }

        private static void ClenseArchiveFolder(string folder, int versions)
        {
            LogHelper.Info<uSyncIO>("Keeping Archive versions in {0} at {1} versions", () => folder, () => versions);
            if (Directory.Exists(folder))
            {
                DirectoryInfo dir = new DirectoryInfo(folder);
                FileInfo[] FileList = dir.GetFiles("*.*");
                var files = FileList.OrderByDescending(file => file.CreationTime);
                var i = 0;
                foreach (var file in files)
                {
                    i++;
                    if (i > versions)
                    {
                        file.Delete();
                    }
                }
            }
        }

    }
}
