﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;

namespace Jumoo.uSync.Core.Helpers
{
    /// <summary>
    ///  walks the content gets IDs
    ///  and gets IDs and gives you paths...
    /// </summary>
    public class ContentWalker
    {
        public string GetPathFromID(int id)
        {
            var _contentService = ApplicationContext.Current.Services.ContentService;

            LogHelper.Debug<ContentWalker>("Walking the path for node id: {0}", () => id);
            var content = _contentService.GetById(id);
            if (content != null)
            {
                return GetContentPath(content);
            }

            return "";
        }

        private string GetContentPath(IContent content)
        {
            var path = content.Name;
            if (content.ParentId != -1)
            {
                path = GetContentPath(content.Parent()) + "\\" + path;
            }

            return path;

        }

        public int GetIdFromPath(string path)
        {
            var _contentService = ApplicationContext.Current.Services.ContentService;

            LogHelper.Debug<ContentWalker>("Getting the id for path: {0}", () => path);

            if (!string.IsNullOrWhiteSpace(path))
            {
                var bits = path.Split('\\');
                var rootName = bits[0];

                var root = _contentService.GetByLevel(1).Where(x => x.Name == rootName).FirstOrDefault();
                if (root != null)
                {
                    // recurse into the rest of it...
                    return GetLastId(_contentService, root.Id, bits, 2);
                }
            }
            return -1;

        }

        private int GetLastId(IContentService _service, int parentId, string[] bits, int level)
        {
            LogHelper.Debug<ContentWalker>("Recursing {0} - {1}", () => level, () => parentId);

            var here = _service.GetChildrenByName(parentId, bits[level - 1]).FirstOrDefault();
            // var here = node.Children().Where(x => x.Name == bits[level-1]).FirstOrDefault();            
            if (here != null)
            {
                if (bits.Length == level)
                {
                    LogHelper.Debug<ContentWalker>("We are at level {0} we thing {1} is our node", () => level, () => here.Name);
                    return here.Id;
                }
                else if (bits.Length > level)
                {
                    return GetLastId(_service, here.Id, bits, level + 1);
                }
                else
                {
                    // we've gone to far if we get here...
                    LogHelper.Debug<ContentWalker>("Somethings gone wrong, we've gone to far....");
                    return -1;
                }
            }
            else
            {
                LogHelper.Debug<ContentWalker>("Couldn't find {0}", () => bits[level - 1]);
                return -1;
            }
        }
    }
}
