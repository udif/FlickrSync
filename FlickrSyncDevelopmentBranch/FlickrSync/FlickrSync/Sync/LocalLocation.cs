using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.IO;

namespace FlickrSync
{
    /// <summary>
    /// Store information about local folders and files
    /// </summary>
    class LocalLocation : SyncLocation
    {

        #region Properties

        // Folders to sync and path information
        ArrayList SyncFolders;
        ArrayList PathInfoList;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public LocalLocation() 
        {
            Program.LocalLoc = this;

            SyncFolders = new ArrayList();
            PathInfoList = new ArrayList();            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add (and create) a new SyncFolder
        /// </summary>
        /// <param name="pFolderPath">String path to the folder</param>
        public void Add(string pFolderPath)
        {
            SyncFolders.Add(new SyncFolder(pFolderPath));
        }

        /// <summary>
        /// Add an existing SyncFolder
        /// </summary>
        /// <param name="sf">SyncFolder to add</param>
        public void Add(SyncFolder sf)
        {
            SyncFolders.Add(sf);
        }

        /// <summary>
        /// Remove and existing SyncFolder TODO: why does it create a new one to add one?
        /// </summary>
        /// <param name="pFolderPath"></param>
        public void Remove(string pFolderPath)
        {
            SyncFolders.Remove(new SyncFolder(pFolderPath));
        }

        /// <summary>
        /// Add (and create) a new PathInfo
        /// </summary>
        /// <param name="pPath">String path</param>
        /// <param name="pOpen">Boolean open or not</param>
        /// <param name="pManualAdd">Boolean manually added or not</param>
        public void AddPath(string pPath,bool pOpen,bool pManualAdd)
        {
            PathInfoList.Add(new PathInfo(pPath,pOpen,pManualAdd));
        }

        /// <summary>
        /// Add an existing PathInfo
        /// </summary>
        /// <param name="pi">PathInfo to add</param>
        public void AddPath(PathInfo pi)
        {
            PathInfoList.Add(pi);
        }

        /// <summary>
        /// Remove a PathInfo TODO: again why create one to remove it?
        /// </summary>
        /// <param name="pPath"></param>
        public void RemovePath(string pPath)
        {
            PathInfoList.Remove(new PathInfo(pPath));
        }

        /// <summary>
        /// Get XML information about what to sync
        /// </summary>
        /// <returns>String of xml data</returns>
        public XmlDocument GetSyncXML()
        {
            // XML to store settings
            XmlDocument FlickrSyncXml = new XmlDocument();

            XmlElement root = FlickrSyncXml.CreateElement("FlickrSync");
            FlickrSyncXml.AppendChild(root);

            foreach (SyncFolder sf in SyncFolders)
            {
                root.AppendChild(FlickrSyncXml.ImportNode(sf.GetXml(), true));
            }

            foreach (PathInfo pi in PathInfoList)
            {
                root.AppendChild(FlickrSyncXml.ImportNode(pi.GetXml(), true));
            }
                
            return FlickrSyncXml;

            /*
             *             // 1. Get existing settings from Properties... 
            XmlDocument SyncFoldersXml = Properties.Settings.Default.LocalInfoXml;

            // 2. Get SyncFolder settings from the files themselves

            XmlElement root = SyncFoldersXml.DocumentElement;

            // ensure the existing settings file is valid
            if(!(root.LocalName).Equals("FlickrSync")) 
            {
                // if it isn't make a new valid one
                SyncFoldersXml = new XmlDocument();
                root = SyncFoldersXml.CreateElement("FlickrSync");
                SyncFoldersXml.AppendChild(root);
            }
            
            // 3. Merge these preferring the newer seetings in 2 over 1

            // create a navigator to go through the XmlDocument
            XPathNavigator Nav = SyncFoldersXml.CreateNavigator();
            XPathNodeIterator Iterator = Nav.Select("/FlickrSync/SyncFolder");

            ArrayList ExistingFolders = new ArrayList();

            // go through all the SyncFolder nodes checking sub nodes for existing FolderPaths
            while (Iterator.MoveNext())
            {
                Nav.MoveToFirstChild();

                if (Nav.Name == "FolderPath")
                {
                    ExistingFolders.Add(Nav.Value);
                }   
            }
                                    
            // add the new information removing existing information when appropriate
            foreach (SyncFolder sf in SyncFolders)
            {
                // if the SyncFolder already exists delete it from the XmlDocument
                if (ExistingFolders.Contains(sf.FolderPath))
                {
                    Nav.DeleteSelf();
                }
                
                // Write the new SyncFolder information
                root.AppendChild(SyncFoldersXml.ImportNode(sf.GetXml(), true));               
            }

            // TODO: need to do the same as above for PathInfo?
            foreach (PathInfo pi in PathInfoList)
            {
                root.AppendChild(SyncFoldersXml.ImportNode(pi.GetXml(), true));
            }


            // 4. Provide the merged document as the return            
            return SyncFoldersXml;
             */
        }


        /// <summary>
        /// Load the sync instructions from the XML file into this object
        /// </summary>
        /// <param name="xml">String of XML</param>
        public void LoadFromXML(XmlDocument xmldoc)
        {
            XPathNavigator nav = xmldoc.CreateNavigator();
            XPathNodeIterator iterator=nav.Select("/FlickrSync/SyncFolder");

            while (iterator.MoveNext())
            {
                SyncFolder sf=new SyncFolder();
                XPathNavigator nav2 = iterator.Current;
                sf.LoadFromXPath(nav2);

                DirectoryInfo dir = new DirectoryInfo(sf.FolderPath);
                if (!dir.Exists)
                {
                    if (Properties.Settings.Default.MessageLevel!=MessageLevel.MessagesNone) 
                    {
                        if (UserMessage.DisplayMessage("Folder " + sf.FolderPath + " no longer exists. Remove from configuration?", "Warning") == true)
                        {
                            Program.logger.Info(sf.FolderPath + "marked for removal from configuration");
                            continue;
                        }
                        else
                        {
                            Program.logger.Info(sf.FolderPath + "does not exists");
                        }                            
                    }
                }

                SyncFolders.Add(sf);
            }

            iterator = nav.Select("/FlickrSync/PathInfo");

            while (iterator.MoveNext())
            {
                PathInfo pi = new PathInfo();
                XPathNavigator nav2 = iterator.Current;
                pi.LoadFromXPath(nav2);

                DirectoryInfo dir = new DirectoryInfo(pi.Path);
                if (!dir.Exists)
                {
                    if (!pi.ManualAdd)
                        continue;

                    if (Properties.Settings.Default.MessageLevel != MessageLevel.MessagesNone)
                    {
                        if (UserMessage.DisplayMessage("Folder " + pi.Path + " no longer exists. Remove from configuration?", "Warning") == true)
                        {
                            Program.logger.Info("Path" + pi.Path + " marked for removal from configuration");
                            continue;
                        }
                        else
                            Program.logger.Info("Path" + pi.Path + " no longer exists");
                    }
                }

                PathInfoList.Add(pi);
            }
        }

        /// <summary>
        /// Associate a folder with a set
        /// </summary>
        /// <param name="FolderPath">String the folder path</param>
        /// <param name="SetId">String the SetID</param>
        public void Associate(string FolderPath, string SetId)
        {
            foreach(SyncFolder sf in SyncFolders)
                if (sf.FolderPath == FolderPath)
                {
                    sf.SetId = SetId;
                    sf.SetTitle = "";
                    sf.SetDescription = "";
                }
        }

        /// <summary>
        /// Get the SetID
        /// </summary>
        /// <param name="Path">String the 'Path'</param>
        /// <returns>The SetId if it exists</returns>
        public string GetSetId(string Path)
        {
            int index=SyncFolders.IndexOf(new SyncFolder(Path));
            if (index<0)
                return "";
            else
                return ((SyncFolder) SyncFolders[index]).SetId;
        }

        /// <summary>
        /// Check if Path exists
        /// </summary>
        /// <param name="Path">String the path</param>
        /// <returns>True if the path exists; otherwise, false.</returns>
        public bool ExistsPath(string Path)
        {
            int index=PathInfoList.IndexOf(new PathInfo(Path));
            return index>=0;
        }

        /// <summary>
        /// Get the PathInfo
        /// </summary>
        /// <param name="Path">String the Path</param>
        /// <returns>PathInfo object if it exists; otherwise, null.</returns>
        public PathInfo GetPathInfo(string Path)
        {
            int index = PathInfoList.IndexOf(new PathInfo(Path));
            if (index >= 0)
                return (PathInfo)PathInfoList[index];
            else
                return null;
        }

        /// <summary>
        /// Check if a SyncFolder exists with the given path
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        public bool Exists(string Path)
        {
            return SyncFolders.IndexOf(new SyncFolder(Path))>=0;
        }

        /// <summary>
        /// Check if the given path is included in the Syncfolder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Includes(string path)
        {
            foreach (SyncFolder sf in SyncFolders)
            {
                if (sf.FolderPath.StartsWith(path,StringComparison.CurrentCultureIgnoreCase)) 
                    if (sf.FolderPath.Length>path.Length && (sf.FolderPath[path.Length]==Path.DirectorySeparatorChar || path[path.Length-1]==Path.DirectorySeparatorChar))
                        return true;
            }

            return false;
        }

        /// <summary>
        /// Getter
        /// </summary>
        /// <returns>PathInfoList</returns>
        public ArrayList GetPathInfoList()
        {
            return PathInfoList;
        }

        /// <summary>
        /// Getter
        /// </summary>
        /// <returns>SyncFolders</returns>
        public ArrayList GetSyncFolders()
        {
            return SyncFolders;
        }

        /// <summary>
        /// SyncFolder Getter by Path
        /// </summary>
        /// <param name="pFolderPath">String of Path</param>
        /// <returns>SyncFolder if it exists; otherwise, null.</returns>
        public SyncFolder GetSyncFolder(string pFolderPath)
        {
            int i=SyncFolders.IndexOf(new SyncFolder(pFolderPath));
            if (i >= 0)
                return (SyncFolder) SyncFolders[i];
            else
                return null;
        }

        /// <summary>
        /// SyncFolder Getter by SetId
        /// </summary>
        /// <param name="pFolderPath">String of Path</param>
        /// <returns>SyncFolder if it exists; otherwise, null.</returns>
        public SyncFolder GetSyncFolderBySet(string SetId)
        {
            foreach(SyncFolder sf in SyncFolders) 
                if (sf.SetId==SetId) 
                    return sf;

            return null;
        }

        #endregion
    }
}