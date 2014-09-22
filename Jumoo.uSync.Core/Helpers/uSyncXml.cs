using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Helpers
{
    /// <summary>
    ///  forsimplicity - this is the stuff for dealing with
    ///  legacy types that use XmlDocument not XElement
    ///  when we no longer call legacy API calls, this class
    ///  can go.
    /// </summary>
    public static class uSyncXml
    {
        public static XmlDocument CreateXmlDoc()
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "no");
            doc.AppendChild(dec);

            return doc; 
        }

        public static XElement ToXElement(this XmlDocument doc)
        {
            XElement e = XElement.Load(new XmlNodeReader(doc));
            return e;
        }

        public static XmlNode ToXmlNode(this XElement node, string elementName)
        {
            XmlDocument xD = new XmlDocument();
            xD.LoadXml(node.ToString());
            return xD.SelectSingleNode("//" + elementName);
        }
    }
}
