using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Core
{

    public class uSyncResultBase<T>
    {
        public uSyncResultStatus Status;
        public uSyncAction Action ;
        public T item;
    }

    public class uSyncImportResult<T> : uSyncResultBase<T>
    {}

    public class uSyncExportResult<T> : uSyncResultBase<T>
    {
        public System.Xml.Linq.XElement node;
    }

    public enum uSyncResultStatus
    {
        Success,
        Error,
    }

    public enum uSyncAction
    {
        Create,
        Modify,
        Delete,
        NoChange,
        Export,
        NoAction
    }
}
