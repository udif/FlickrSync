using System;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using System.IO;

namespace FlickrSync
{
    public class LocalInfo
    {
        ArrayList SyncFolders;
        ArrayList PathInfoList;

        public LocalInfo()
        {
            SyncFolders = new ArrayList();
            PathInfoList = new ArrayList();
        }

        public void Add(string pFolderPath)
        {
            SyncFolders.Add(new SyncFolder(pFolderPath));
        }

        public void Add(SyncFolder sf)
        {
            SyncFolders.Add(sf);
        }

        public void Remove(string pFolderPath)
        {
            SyncFolders.Remove(new SyncFolder(pFolderPath));
        }

        public void AddPath(string pPath,bool pOpen,bool pManualAdd)
        {
            PathInfoList.Add(new PathInfo(pPath,pOpen,pManualAdd));
        }

        public void AddPath(PathInfo pi)
        {
            PathInfoList.Add(pi);
        }

        public void RemovePath(string pPath)
        {
            PathInfoList.Remove(new PathInfo(pPath));
        }

        public string GetSyncXML()
        {
            string xml = "<FlickrSync>\r";

            foreach (SyncFolder sf in SyncFolders)
                xml = xml + sf.GetXml();

            foreach (PathInfo pi in PathInfoList)
                xml = xml + pi.GetXml();

            xml = xml + "</FlickrSync>\r";
            return xml;
        }

        public void SaveToXML()
        {
            Properties.Settings.Default.LocalInfoXml = GetSyncXML();
            FlickrSync.SaveConfig();
        }

        public void LoadFromXML(string xml)
        {
            // current search/replace pattern in case base directory has moved
            string oldpath = "", newpath = "";

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(xml);
            XPathNavigator nav = xmldoc.CreateNavigator();
            XPathNodeIterator iterator=nav.Select("/FlickrSync/SyncFolder");

            while (iterator.MoveNext())
            {
                SyncFolder sf=new SyncFolder();
                XPathNavigator nav2 = iterator.Current;
                sf.LoadFromXPath(nav2);

                DirectoryInfo dir = new DirectoryInfo(sf.FolderPath);
                if (!dir.Exists && newpath != "" && sf.FolderPath.StartsWith(oldpath))
                {
                    // Directory no longer exists, but it seems that we might be able to find its new location,
                    // based on an alternate directory location we got from a previously missing directory
                    string potentialFolderPath = sf.FolderPath.Replace(oldpath, newpath); // replace prefix as we did last time
                    dir = new DirectoryInfo(potentialFolderPath);
                    // Have we succeeded? silently replace prefix
                    if (dir.Exists)
                    {
                        FlickrSync.Log(FlickrSync.LogLevel.LogAll, "Replaced " + sf.FolderPath + " with " + potentialFolderPath + " based on previous alternate directory location");
                        sf.FolderPath = potentialFolderPath;
                    }
                }
                if (!dir.Exists)
                {
                    if (FlickrSync.messages_level!=FlickrSync.MessagesLevel.MessagesNone) 
                    {
                        if (MessageBox.Show("Folder " + sf.FolderPath + " no longer exists. Remove from configuration?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            FlickrSync.Log(FlickrSync.LogLevel.LogAll,sf.FolderPath + "marked for removal from configuration");
                            continue;
                        }
                        else if (MessageBox.Show("Replace Folder " + sf.FolderPath + " with a different location?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            string[] oldpaths = sf.FolderPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                            Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog cofd = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
                            cofd.InitialDirectory = oldpaths[oldpaths.Length-1];
                            cofd.Multiselect = false;
                            cofd.IsFolderPicker = true;
                            if (cofd.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
                            {
                                string[] newpaths = cofd.FileName.ToString().Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                                int oldindex = oldpaths.Length - 1;
                                int newindex = newpaths.Length - 1;
                                // Loop while we still have something to work with on both old and new
                                while (oldindex >= 1 && newindex >= 1)
                                {
                                    if (oldpaths[oldindex] == newpaths[newindex])
                                    {
                                        // If this part is common, it is not part of the search/replace pattern
                                        oldindex--;
                                        newindex--;
                                        continue;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                Array.Resize<string>(ref oldpaths, oldindex + 1);
                                Array.Resize<string>(ref newpaths, newindex + 1);
                                oldpath = String.Join(Path.DirectorySeparatorChar.ToString(), oldpaths);
                                newpath = String.Join(Path.DirectorySeparatorChar.ToString(), newpaths);
                                FlickrSync.Log(FlickrSync.LogLevel.LogAll, sf.FolderPath + "replaced by " + cofd.FileName.ToString() + " in the configuration");
                                FlickrSync.Log(FlickrSync.LogLevel.LogAll, "From now on, we will try replacing " + oldpath + " by " + newpath);
                                continue;
                            }
                            else
                            {
                                FlickrSync.Log(FlickrSync.LogLevel.LogAll, sf.FolderPath + "not replaced after all");
                                continue;
                            }
                        }
                        else
                            FlickrSync.Log(FlickrSync.LogLevel.LogAll, sf.FolderPath + "does not exists");
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

                    if (FlickrSync.messages_level != FlickrSync.MessagesLevel.MessagesNone)
                    {
                        if (MessageBox.Show("Folder " + pi.Path + " no longer exists. Remove from configuration?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            FlickrSync.Log(FlickrSync.LogLevel.LogAll, "Path" + pi.Path + " marked for removal from configuration");
                            continue;
                        }
                        else
                            FlickrSync.Log(FlickrSync.LogLevel.LogAll, "Path" + pi.Path + " no longer exists");
                    }
                }

                PathInfoList.Add(pi);
            }
        }

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

        public string GetSetId(string Path)
        {
            int index=SyncFolders.IndexOf(new SyncFolder(Path));
            if (index<0)
                return "";
            else
                return ((SyncFolder) SyncFolders[index]).SetId;
        }

        public bool ExistsPath(string Path)
        {
            int index=PathInfoList.IndexOf(new PathInfo(Path));
            return index>=0;
        }

        public PathInfo GetPathInfo(string Path)
        {
            int index = PathInfoList.IndexOf(new PathInfo(Path));
            if (index >= 0)
                return (PathInfo)PathInfoList[index];
            else
                return null;
        }

        public bool Exists(string Path)
        {
            return SyncFolders.IndexOf(new SyncFolder(Path))>=0;
        }

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

        public ArrayList GetPathInfoList()
        {
            return PathInfoList;
        }

        public ArrayList GetSyncFolders()
        {
            return SyncFolders;
        }

        public SyncFolder GetSyncFolder(string pFolderPath)
        {
            int i=SyncFolders.IndexOf(new SyncFolder(pFolderPath));
            if (i >= 0)
                return (SyncFolder) SyncFolders[i];
            else
                return null;
        }

        public SyncFolder GetSyncFolderBySet(string SetId)
        {
            foreach(SyncFolder sf in SyncFolders) 
                if (sf.SetId==SetId) 
                    return sf;

            return null;
        }
    }
}
