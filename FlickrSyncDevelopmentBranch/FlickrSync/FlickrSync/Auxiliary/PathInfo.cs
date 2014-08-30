using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace FlickrSync
{
    /// <summary>
    /// Provide the folder path to open in the TreeFolder view
    /// </summary>   
    public class PathInfo
    {
        public string Path;
        public bool Open;
        public bool ManualAdd;

        /// <summary>
        /// Default constructor (no path)
        /// </summary>
        public PathInfo()
        {
            Path = "";
            Open = false;
            ManualAdd = false;
        }

        /// <summary>
        /// Constructor with path passed in as string
        /// </summary>
        /// <param name="pPath">input path string</param>
        public PathInfo(string pPath)
        {
            Path = pPath;
            Open = false;
            ManualAdd = false;
        }

        /// <summary>
        /// Constructor with path and whether it is opened and manually added
        /// </summary>
        /// <param name="pPath">input path string</param>
        /// <param name="pOpen">path is open</param>
        /// <param name="pManualAdd">path manually added</param>
        public PathInfo(string pPath, bool pOpen, bool pManualAdd)
        {
            Path = pPath;
            Open = pOpen;
            ManualAdd = pManualAdd;
        }

        /// <summary>
        /// Override Equals() method to compare paths
        /// </summary>
        /// <param name="pi">PathInfo object to compare</param>
        /// <returns>true if the paths are the same; otherwise, false.</returns>
        public override bool Equals(object pi)
        {
            return Path.Equals(((PathInfo)pi).Path, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Provide the hashcode of the class (Not exactly sure why...)
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Encode the paths using simple string replaces
        /// </summary>
        /// <param name="text">text to be encoded</param>
        /// <returns>encoded text</returns>
        private string XmlEncode(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;");
        }

        /// <summary>
        /// Decode the paths using simple string replaces
        /// </summary>
        /// <param name="text">text to be decoded</param>
        /// <returns>decoded text</returns>
        private string XmlDecode(string text)
        {
            return text.Replace("&lt;", "<").Replace("&amp;", "&");
        }

        /// <summary>
        /// Provide XML sequence for current path
        /// </summary>
        /// <returns></returns>
        public XmlDocumentFragment GetXml()
        {
            XmlDocumentFragment PathInfoXml = new XmlDocument().CreateDocumentFragment();

            string OpenValue = "0";
            string ManualAddValue = "0";

            if (Open)
            {
                OpenValue = "1";
            }

            if (ManualAdd)
            {
                ManualAddValue = "1";
            }
            
            PathInfoXml.InnerXml =  "\n\t<PathInfo>\n" +
                                    "\t\t<Path>" + XmlEncode(Path) + "</Path>\n" +
                                    "\t\t<Open>" + OpenValue + "</Open>\n" +
                                    "\t\t<ManualAdd>" + ManualAddValue + "</ManualAdd>\n" +
                                    "\t</PathInfo>\n";
                   
            return PathInfoXml;
        }

        /// <summary>
        /// Load the paths from and XML file at startup?
        /// </summary>
        /// <param name="nav">XPathNavigator object from input XML file</param>
        public void LoadFromXPath(XPathNavigator nav)
        {
            nav.MoveToFirstChild();

            do
            {
                if (nav.Name == "Path") Path = XmlDecode(nav.Value);
                else if (nav.Name == "Open") Open = nav.ValueAsBoolean;
                else if (nav.Name == "ManualAdd") ManualAdd = nav.ValueAsBoolean;
            } while (nav.MoveToNext());
        }

        /// <summary>
        /// Check the path status
        /// </summary>
        /// <returns>If not open and manually added true; otherwise, false</returns>
        public bool IsEmpty()
        {
            return !Open && !ManualAdd;
        }
    }
}
