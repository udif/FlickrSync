using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using System.Globalization;
using System.Xml;

namespace FlickrSync
{
    /// <summary>
    /// Stores information about each sync folder
    /// </summary>
    public class SyncFolder
    {
        public enum Methods { SyncFilename = 0, SyncDateTaken, SyncTitleOrFilename };
        public enum FilterTypes { FilterNone = 0, FilterIncludeTags, FilterStarRating };
        public enum OrderTypes { OrderDefault = 0, OrderDateTaken, OrderTitle, OrderTag };

        public string FolderPath;
        public string SetId;
        public string SetTitle;
        public string SetDescription;
        public DateTime LastSync;
        public Methods SyncMethod;
        public FilterTypes FilterType;
        public string FilterTags;
        public int FilterStarRating;
        public FlickrPermissions Permission;
        public bool NoDelete;
        public bool NoDeleteTags;
        public OrderTypes OrderType;
        public bool NoInitialReplace;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SyncFolder()
        {
            FolderPath = "";
            LastSync = new DateTime(2000, 1, 1);

            SetId = "";
            SetTitle = "";
            SetDescription = "";
            SyncMethod = StringToMethod(Properties.Settings.Default.Method);
            FilterType = FilterTypes.FilterNone;
            FilterTags = "";
            FilterStarRating = 0;
            Permission = FlickrPermissions.PermDefault;
            NoDelete = Properties.Settings.Default.NoDelete;
            NoDeleteTags = Properties.Settings.Default.NoDeleteTags;
            OrderType = OrderTypes.OrderDefault;
            NoInitialReplace = false;
        }

        /// <summary>
        /// Constructor which takes a folder path
        /// </summary>
        /// <param name="pFolderPath">given folder path of the new set</param>
        public SyncFolder(string pFolderPath)
        {
            FolderPath = pFolderPath;
            LastSync = new DateTime(2000, 1, 1);

            SetId = "";
            SetTitle = "";
            SetDescription = "";
            SyncMethod = StringToMethod(Properties.Settings.Default.Method);
            FilterType = FilterTypes.FilterNone;
            FilterTags = "";
            FilterStarRating = 0;
            Permission = FlickrPermissions.PermDefault;
            NoDelete = Properties.Settings.Default.NoDelete;
            NoDeleteTags = Properties.Settings.Default.NoDeleteTags;
            OrderType = OrderTypes.OrderDefault;
            NoInitialReplace = false;
        }

        // TODO: investigate further to check whether this is what I think it is...
        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static public Methods StringToMethod(string str)
        {
            if (str == "SyncFilename")
                return Methods.SyncFilename;
            else if (str == "SyncDateTaken")
                return Methods.SyncDateTaken;
            else if (str == "SyncTitleOrFilename")
                return Methods.SyncTitleOrFilename;
            else
                return Methods.SyncFilename; //default
        }

        /// <summary>
        /// Odering information
        /// </summary>
        /// <param name="str">given ordering type</param>
        /// <returns>value from the OrderTypes enumeration</returns>
        static public OrderTypes StringToOrderType(string str)
        {
            if (str == "OrderTag")
                return OrderTypes.OrderTag;
            else if (str == "OrderDateTaken")
                return OrderTypes.OrderDateTaken;
            else if (str == "OrderTitle")
                return OrderTypes.OrderTitle;
            else
                return OrderTypes.OrderDefault; //default
        }

        /// <summary>
        /// Overriden comparator which compares class by the folder path
        /// </summary>
        /// <param name="sf">SyncFolder being compared</param>
        /// <returns>true if the same folder path; otherwise, false.</returns>
        public override bool Equals(object sf)
        {
            return FolderPath.Equals(((SyncFolder)sf).FolderPath, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Overriden class hashcode
        /// </summary>
        /// <returns>int hashcode</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // Could use XMLTextWriter to encode these but this is probably sufficient

        /// <summary>
        /// Encode the two special characters so they are usable by XML
        /// </summary>
        /// <param name="text">XML string to encoder</param>
        /// <returns>String of encoded XML</returns>
        private string XmlEncode(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;");
        }

        /// <summary>
        /// Decode the two special characters so they are usable by XML
        /// </summary>
        /// <param name="text">XML string to Decoder</param>
        /// <returns>String of Decoded XML</returns>
        private string XmlDecode(string text)
        {
            return text.Replace("&lt;", "<").Replace("&amp;", "&");
        }

        /// <summary>
        /// Create the XML fragment to save the folders details
        /// </summary>
        /// <returns>String of the XML</returns>
        public XmlDocumentFragment GetXml()
        {
            // create an XML fragment to store the syncfolder details
            XmlDocumentFragment SyncFolderXml = new XmlDocument().CreateDocumentFragment();

            string NoDeleteValue = "0";
            string NoDeleteTagsValue = "0";
            string NoInitialReplaceValue = "0";

            if (NoDelete)
            {
                NoDeleteValue = "1";
            }

            if (NoDeleteTags)
            {
                NoDeleteTagsValue = "1";
            }

            if (NoInitialReplace)
            {
                NoInitialReplaceValue = "1";
            }

            SyncFolderXml.InnerXml =  "\n\t<SyncFolder>\n" +
                                      "\t\t<FolderPath>" + XmlEncode(FolderPath) + "</FolderPath>\n" +
                                      "\t\t<LastSync>" + LastSync.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo) + "</LastSync>\n" +
                                      "\t\t<SetId>" + SetId + "</SetId>\n" +
                                      "\t\t<SetTitle>" + XmlEncode(SetTitle) + "</SetTitle>\n" +
                                      "\t\t<SetDescription>" + XmlEncode(SetDescription) + "</SetDescription>\n" +
                                      "\t\t<SyncMethod>" + SyncMethod.ToString() + "</SyncMethod>\n" +
                                      "\t\t<FilterType>" + FilterType.ToString() + "</FilterType>\n" +
                                      "\t\t<FilterTags>" + FilterTags + "</FilterTags>\n" +
                                      "\t\t<FilterStarRating>" + FilterStarRating + "</FilterStarRating>\n" +
                                      "\t\t<Permissions>" + Permission.ToString() + "</Permissions>\n" +
                                      "\t\t<NoDelete>" + NoDeleteValue + "</NoDelete>\n" +
                                      "\t\t<NoDeleteTags>" + NoDeleteTagsValue + "</NoDeleteTags>\n" +
                                      "\t\t<OrderType>" + OrderType.ToString() + "</OrderType>\n" +
                                      "\t\t<NoInitialReplace>" + NoInitialReplaceValue + "</NoInitialReplace>\n" +
                                      "\t</SyncFolder>\n";      
      
            return SyncFolderXml;
        }

        /// <summary>
        /// Load the information from the stored XML file using the XPathNavigator
        /// </summary>
        /// <param name="nav">the navigation object of the given settings XML file</param>
        public void LoadFromXPath(XPathNavigator nav)
        {
            nav.MoveToFirstChild();

            do
            {
                if (nav.Name == "FolderPath") FolderPath = XmlDecode(nav.Value);
                else if (nav.Name == "LastSync")
                {
                    try
                    {
                        LastSync = DateTime.ParseExact(nav.Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        LastSync = DateTime.Parse(nav.Value); //old method
                    }
                }
                else if (nav.Name == "SetId") SetId = nav.Value;
                else if (nav.Name == "SetTitle") SetTitle = XmlDecode(nav.Value);
                else if (nav.Name == "SetDescription") SetDescription = XmlDecode(nav.Value);
                else if (nav.Name == "SyncMethod")
                {
                    if (nav.Value == "SyncFilename") SyncMethod = SyncFolder.Methods.SyncFilename;
                    else if (nav.Value == "SyncDateTaken") SyncMethod = SyncFolder.Methods.SyncDateTaken;
                    else if (nav.Value == "SyncTitleOrFilename") SyncMethod = SyncFolder.Methods.SyncTitleOrFilename;
                }
                else if (nav.Name == "FilterType")
                {
                    if (nav.Value == "FilterNone") FilterType = SyncFolder.FilterTypes.FilterNone;
                    else if (nav.Value == "FilterIncludeTags") FilterType = SyncFolder.FilterTypes.FilterIncludeTags;
                    else if (nav.Value == "FilterStarRating") FilterType = SyncFolder.FilterTypes.FilterStarRating;
                }
                else if (nav.Name == "FilterTags") FilterTags = nav.Value;
                else if (nav.Name == "FilterStarRating") FilterStarRating = nav.ValueAsInt;
                else if (nav.Name == "Permissions")
                {
                    if (nav.Value == "PermDefault") Permission = FlickrPermissions.PermDefault;
                    else if (nav.Value == "PermPublic") Permission = FlickrPermissions.PermPublic;
                    else if (nav.Value == "PermFamilyFriends") Permission = FlickrPermissions.PermFamilyFriends;
                    else if (nav.Value == "PermFriends") Permission = FlickrPermissions.PermFriends;
                    else if (nav.Value == "PermFamily") Permission = FlickrPermissions.PermFamily;
                    else if (nav.Value == "PermPrivate") Permission = FlickrPermissions.PermPrivate;
                }
                else if (nav.Name == "NoDelete") NoDelete = nav.ValueAsBoolean;
                else if (nav.Name == "NoDeleteTags") NoDeleteTags = nav.ValueAsBoolean;
                else if (nav.Name == "OrderType")
                {
                    if (nav.Value == "OrderDefault") OrderType = OrderTypes.OrderDefault;
                    else if (nav.Value == "OrderDateTaken") OrderType = OrderTypes.OrderDateTaken;
                    else if (nav.Value == "OrderTitle") OrderType = OrderTypes.OrderTitle;
                    else if (nav.Value == "OrderTag") OrderType = OrderTypes.OrderTag;
                }
                else if (nav.Name == "NoInitialReplace") NoInitialReplace = nav.ValueAsBoolean;
            } while (nav.MoveToNext());
        }
    }
}
