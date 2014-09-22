using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Models
{
    public interface IUSyncCoreBase<T>
    {
        T Import(XElement node, bool forceUpdate);
        XElement Export(T item);
    }

    /// <summary>
    ///  double pass importing (doctypes really)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IUSyncCoreTwoPass<T>
    {
        T ImportAgain(T item, XElement node);
    }
}

