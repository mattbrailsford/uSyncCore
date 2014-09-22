using System;
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
    ///  the same but for media (these should be generic but i can't find the right base class...)
    /// </summary>
    public class MediaWalker
    {
        public string GetPathFromID(int id)
        {
            var _mediaService = ApplicationContext.Current.Services.MediaService;

            LogHelper.Info<MediaWalker>("Walking the path for node id: {0}", () => id);
            var media = _mediaService.GetById(id);
            if (media != null)
            {
                return GetMediaPath(media);
            }

            return "";
        }

        private string GetMediaPath(IMedia content)
        {
            var path = content.Name;
            if (content.ParentId != -1)
            {
                path = GetMediaPath(content.Parent()) + "\\" + path;
            }

            return path;

        }

        public int GetIdFromPath(string path)
        {
            var _mediaService = ApplicationContext.Current.Services.MediaService;

            LogHelper.Debug<MediaWalker>("Getting the id for path: {0}", () => path);

            if (!string.IsNullOrWhiteSpace(path))
            {
                var bits = path.Split('\\');
                var rootName = bits[0];

                var root = _mediaService.GetByLevel(1).Where(x => x.Name == rootName).FirstOrDefault();
                if (root != null)
                {
                    return GetLastId(_mediaService, root.Id, bits, 2);
                }
            }
            return -1;

        }

        private int GetLastId(IMediaService _service, int parentId, string[] bits, int level)
        {
            LogHelper.Debug<MediaWalker>("Recursing {0} - {1}", () => level, () => parentId);

            var here = _service.GetChildren(parentId).Where(x => x.Name == bits[level - 1]).FirstOrDefault();
            // var here = _service.GetChildrenByName(parentId, bits[level - 1]).FirstOrDefault();
            // var here = node.Children().Where(x => x.Name == bits[level-1]).FirstOrDefault();            
            if (here != null)
            {
                if (bits.Length == level)
                {
                    LogHelper.Debug<MediaWalker>("We are at level {0} we think {1} is our node", () => level, () => here.Name);
                    return here.Id;
                }
                else if (bits.Length > level)
                {
                    return GetLastId(_service, here.Id, bits, level + 1);
                }
                else
                {
                    // we've gone to far if we get here...
                    LogHelper.Debug<MediaWalker>("Somethings gone wrong, we've gone to far....");
                    return -1;
                }
            }
            else
            {
                LogHelper.Debug<MediaWalker>("Couldn't find {0}", () => bits[level - 1]);
                return -1;
            }
        }

    }


}

