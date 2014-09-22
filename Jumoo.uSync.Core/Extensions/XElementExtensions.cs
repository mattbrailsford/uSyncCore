﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Extensions
{
    public static class XElementExtensions
    {
        public static void AddMD5Hash(this XElement node)
        {
            node.AddMD5Hash(false);
        }

        public static void AddMD5Hash(this XElement node, bool removePreValIds)
        {
            string md5 = "";
            md5 = CalculateMD5Hash(node, removePreValIds);
            node.Add(new XElement("Hash", md5));
        }

        public static void AddMD5Hash(this XElement node, string val)
        {
            string md5 = CalculateMD5Hash(val);
            node.Add(new XElement("Hash", md5));
        }

        public static string GetUSyncMD5Hash(this XElement node)
        {
            XElement hashNode = node.Element("Hash");
            if (hashNode == null)
                return "";

            return hashNode.Value; 
        }

        public static string CalculateMD5Hash(this XElement node, bool removePreValIds)
        {
            if (removePreValIds)
            {
                XElement nodeCopy = new XElement(node);
                var preValRoot = nodeCopy.Element("PreValues");
                if (preValRoot != null && preValRoot.HasElements)
                {
                    foreach (var preValue in preValRoot.Elements("PreValue"))
                    {
                        preValue.SetAttributeValue("Id", "");
                    }
                }
                return CalculateMD5Hash(nodeCopy);
            }
            else
            {
                return CalculateMD5Hash(node);
            }
        }

        private static string CalculateMD5Hash(this XElement node)
        {
            string md5Hash = "";
            MemoryStream stream = new MemoryStream();
            node.Save(stream);

            stream.Position = 0;

            using (var md5 = MD5.Create())
            {
                md5Hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }

            stream.Close();
            return md5Hash; 

        }

        public static string CalculateMD5Hash(string input)
        {
            string hash = "";
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            using (var md5 = MD5.Create())
            {
                hash = BitConverter.ToString(md5.ComputeHash(inputBytes)).Replace("-", "").ToLower();
            }
            return hash;
        }

        public static string ValueOrDefault(this XElement node, string defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                return node.Value;
            }
            return defaultValue;
        }
        
        public static bool ValueOrDefault(this XElement node, bool defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                bool val;
                if (bool.TryParse(node.Value, out val))
                    return val;
            }
            return defaultValue;
        }
        
        public static int ValueOrDefault(this XElement node, int defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                int val;
                if (int.TryParse(node.Value, out val))
                    return val;
            }
            return defaultValue;
        }


        
    }
}
