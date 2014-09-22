using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.Core;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;
// using Umbraco.Core.Models;

using umbraco.cms.businesslogic;

using System.IO;

using System.Xml;
using System.Xml.Linq;

using Jumoo.uSync.BackOffice.Helpers;

namespace Jumoo.uSync.BackOffice
{
    public class SyncDictionaryItems : SyncItemBase
    {
        public static void ImportAllFromDisk()
        {
            var path = Path.Combine(uSyncBackOfficeSettings.Folder, "Dictionary");

            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*.config"))
            {
                XElement node = XElement.Load(file);

                if (node != null)
                {
                    _engine.Dictionary.Import(node);
                }
            }
        }

        public static void SaveAllToDisk()
        {
            foreach(var item in Dictionary.getTopMostItems)
            {
                SaveToDisk(item);
            }

        }

        public static void SaveToDisk(Dictionary.DictionaryItem item)
        {
            if ( item != null )
            {
                Umbraco.Core.Strings.DefaultShortStringHelper _sh = new Umbraco.Core.Strings.DefaultShortStringHelper();

                XElement node = _engine.Dictionary.Export(item);
                var nameKey = _sh.CleanString(item.key, Umbraco.Core.Strings.CleanStringType.Ascii);
                uSyncIO.SaveXmlToDisk(node, "Dictionary", nameKey);
            }
        }

        public static void AttachToEvents()
        {
            Dictionary.DictionaryItem.Saving += DictionaryItem_Saving;
            Dictionary.DictionaryItem.Deleting += DictionaryItem_Deleting;
        }


        static object _deleteLock = new object();
        static System.Collections.ArrayList _dChildren = new System.Collections.ArrayList();

        static void DictionaryItem_Deleting(Dictionary.DictionaryItem sender, EventArgs e)
        {
            LogHelper.Info<SyncDictionaryItems>("Dicitionary Item Deleted");
            lock (_deleteLock)
            {
                if (sender.hasChildren)
                {
                    // we get the delets in a backwards order, so we add all the children of this
                    // node to the list we are not going to delete when we get asked to.
                    // 
                    foreach (Dictionary.DictionaryItem child in sender.Children)
                    {
                        _dChildren.Add(child.id);
                    }
                }

                if (_dChildren.Contains(sender.id))
                {
                    // this is a child of a parent we have already deleted.
                    _dChildren.Remove(sender.id);
                    LogHelper.Debug<SyncDictionaryItems>("No Deleteing Dictionary item {0} because we deleted it's parent",
                        () => sender.key);
                }
                else
                {
                    //actually delete 


                    LogHelper.Debug<SyncDictionaryItems>("Deleting Dictionary Item {0}", () => sender.key);

                    // when you delete a tree, the top gets called before the children. 
                    //             
                    if (!sender.IsTopMostItem())
                    {
                        // if it's not top most, we save it's parent (that will delete)

                        SaveToDisk(GetTop(sender));
                    }
                    else
                    {
                        // it's top we need to delete
                        uSyncIO.ArchiveFile("Dictionary", sender.key);

                    }
                }
            }
        }

        static void DictionaryItem_Saving(Dictionary.DictionaryItem sender, EventArgs e)
        {
            LogHelper.Info<SyncDictionaryItems>("Dicitionary Item Saved");
            SaveToDisk(GetTop(sender));
        }

        private static Dictionary.DictionaryItem GetTop(Dictionary.DictionaryItem item)
        {
            if (!item.IsTopMostItem())
            {
                LogHelper.Debug<SyncDictionaryItems>("is Top Most [{0}]", () => item.IsTopMostItem());
                try
                {
                    if (item.Parent != null)
                    {
                        LogHelper.Debug<SyncDictionaryItems>("parent [{0}]", () => item.Parent.key);
                        return GetTop(item.Parent);
                    }
                }
                catch (ApplicationException ex)
                {
                    LogHelper.Debug<SyncDictionaryItems>("Exception (just like null)");
                }
                catch (ArgumentException ex)
                {
                    LogHelper.Debug<SyncDictionaryItems>("Exception (just like null)");
                }

            }

            return item;
        }

    }
}
