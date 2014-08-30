using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using FlickrNet;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Windows.Forms.Design;
using NLog;

namespace FlickrSync
{
    // TODO: use regions to denote various components of a class and split stuff which doesn't belong in to new classes

    /// <summary>
    /// Main program class
    /// </summary>
    public partial class FlickrSync : Form
    {
        // should all of these be here?
        public enum Permissions { PermDefault = 0, PermPublic, PermFamilyFriends, PermFriends, PermFamily, PermPrivate};
        public enum SyncPropertiesStatus { Default = 0, MultipleUndef, OKAll, CancelAll };
        public enum ErrorType { Normal = 0, FatalError, Connect, Info, Debug };
        public enum MessagesLevel { MessagesNone = 0, MessagesBasic, MessagesAll };
        public enum LogLevel { LogNone = 0, LogBasic, LogAll, LogDebug}; 

        // variables for local and remote set information
        static public LocalInfo li;
        static public RemoteInfo ri;

        // user and settings variables - 
        // TODO: should these be public?
        static public string user;
        static public bool message_tested=false;
        static public bool autorun = false;
        static public MessagesLevel messages_level = MessagesLevel.MessagesAll;
        static public LogLevel log_level = LogLevel.LogNone;
        static public ArrayList HashUsers;
        Point MouseDownPos;

        static private SyncPropertiesStatus syncprop_status=SyncPropertiesStatus.Default;
        static private string proxy_password="";

        // this is the Logger instance for the whole program, this one won't actually log to any output
        // the specific output is determined in this classes constructor depending on the user setting
        // or is changed in Preferences class following a change in the logging preference, the outputs
        // are controlled in the Nlog section of app.config
        static public Logger logger = LogManager.GetLogger("LogNone");

        /// <summary>
        /// Default and only constructor
        /// </summary>
        public FlickrSync()
        {
            // create the window
            InitializeComponent();

            // set the right Logger up
            getLoggerType();
        }

        #region HelperMethods

        /// <summary>
        /// Get the correct Logger type from the settings
        /// </summary>
        private void getLoggerType()
        {
            // if the user has a logging setting enabled create the appropriate logger type
            if (Properties.Settings.Default.LogLevel.Equals("LogAll"))
            {
                logger = LogManager.GetLogger("LogAll");
            }
            else if (Properties.Settings.Default.LogLevel.Equals("LogBasic"))
            {
                logger = LogManager.GetLogger("LogBasic");
            }
            else if (Properties.Settings.Default.LogLevel.Equals("LogDebug"))
            {
                logger = LogManager.GetLogger("LogDebug");
            }
        }

        #endregion

        #region ErrorsAndLogging

        /// <summary>
        /// Button to open Preferences from the error message
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static bool ButtonCallPref(int id)
        {
            Preferences pref = new Preferences();
            pref.ShowDialog();
            return false;
        }

        /// <summary>
        /// Give an error message
        /// </summary>
        /// <param name="msg">the message</param>
        /// <param name="e">the exception</param>
        /// <param name="type">the error type</param>
        public static void Error(string msg, Exception e, ErrorType type)
        {

            string exception_message = "";

            // if there is an exception
            if (e != null)
            {
                exception_message = "Error: " + e.Message;
            }
                
            // if there is an error and it is FatalError or Connect then..
            if (messages_level != MessagesLevel.MessagesNone &&
                !(messages_level == MessagesLevel.MessagesBasic && type != ErrorType.FatalError && type != ErrorType.Connect))
            {
                // if the error is a connection output the following
                if (type == ErrorType.Connect)
                {
                    //Use specific Error dialog to allow changing of preferences
                    CustomMsgBox msgbox = new CustomMsgBox(msg + "\n" + exception_message, "FlickrSync Error");
                    msgbox.AddButton("OK", 75, 1, msgbox.ButtonCallOK);
                    msgbox.AddButton("Preferences", 75, 2, ButtonCallPref);
                    msgbox.ShowDialog();
                }
                else // otherwise output exception message
                {
                    MessageBox.Show(msg + "\n" + exception_message, "Error");
                }
                    
            }

            // add the exception message to the log string
            string logmsg=msg+"; "+exception_message;

            // if fatal or connection error then log it
            if (type == ErrorType.FatalError || type == ErrorType.Connect)
            {
                logger.Fatal(logmsg);
            }
            else
            {
               logger.Info(logmsg);
            }

            // and quit...
            if (type == ErrorType.FatalError || type == ErrorType.Connect)
            { 
                Application.Exit();
            }
                
        }

        /// <summary>
        /// Message level
        /// </summary>
        /// <param name="str">given MessageLevel</param>
        /// <returns>MessageLevel whichever is appropriate</returns>
        public static MessagesLevel StringToMsgLevel(string str)
        {
            if (str == "MessagesNone")
                return MessagesLevel.MessagesNone;
            if (str == "MessagesBasic")
                return MessagesLevel.MessagesBasic;
            if (str == "MessagesAll")
                return MessagesLevel.MessagesAll;

            return MessagesLevel.MessagesAll;
        }

        /// <summary>
        /// Log level
        /// </summary>
        /// <param name="str">given loglevel</param>
        /// <returns>LogLevel whichever is appropriate</returns>
        public static LogLevel StringToLogLevel(string str)
        {
            if (str == "LogNone")
                return LogLevel.LogNone;
            if (str == "LogBasic")
                return LogLevel.LogBasic;
            if (str == "LogAll")
                return LogLevel.LogAll;
            if (str == "LogDebug")
                return LogLevel.LogDebug;

            return LogLevel.LogNone;
        }

        #endregion

        #region InitialLoad

        // TODO: Move user authentication to a seperate class

        /// <summary>
        /// Load the settings and authenticate the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlickrSync_Load(object sender, EventArgs e)
        {
            // probably not needed but just to make sure no message from previous versions is shown
            if (Properties.Settings.Default.MessageId.CompareTo("090130_0000")<0)
            {
                Properties.Settings.Default.MessageId = "090130_0000";
                Properties.Settings.Default.Save();
            }

            HashUsers = new ArrayList();

            // authentication token?
            string token="";
            try {
                token=Properties.Settings.Default.FlickrToken;
            }
            catch(Exception)
            {
            }

            // if there isn't one then we need to ask the user to authenticate
            if (token == "")
            {
                FlickrGetToken();
            }                

            // set the message and log levels to whatever the settings are set to
            messages_level = FlickrSync.StringToMsgLevel(Properties.Settings.Default.MessageLevel);
            log_level = FlickrSync.StringToLogLevel(Properties.Settings.Default.LogLevel);

            // TODO: autorun code
            if (autorun)
            {
                messages_level = MessagesLevel.MessagesNone;
            }
                
            // ri is needed to get the user. TODO: Get User without using ri
            ri = new RemoteInfo();

            // load and set up program
            UpdateUser();
            LoadConfig();
            Reload();
            FlickrSync.logger.Warn("Application loaded");

            // TODO: autorun code
            if (autorun)
                WindowState = FormWindowState.Minimized;
            else
                WindowState = FormWindowState.Maximized;
            
            // TODO: find out what this does
            if (!message_tested)
            {
                try
                {
                    webBrowser1.Navigate(Properties.Settings.Default.MessageUrl + "?version=" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                }
                catch (Exception)
                {
                }
            }

            // TODO: autorun code
            if (autorun)
            {
                ViewAndSync();
                Close();
            }
        }

        /// <summary>
        // TODO: confirm this is the first set up of sets?
        /// </summary>
        /// <param name="progress"></param>
        public void FillListSet(ProgressBar progress)
        {
            int photos = 0;

            // load all the sets from Flickr
            Photoset[] allsets = ri.GetAllSets();

            // set up the progressbar to the length of the sets
            if (progress != null)
            {
                progress.Minimum = 0;
                progress.Maximum = allsets.Length;
                progress.Value = 0;
            }

            // Setup the image list first to speed things up.
            Image[] tempimgs = new Image[allsets.Length];
            Image[] tempimgsmall = new Image[allsets.Length];

            // Go through all the sets getting the set default image?
            for (int i = 0; i < allsets.Length; i++)
            {
                // use a fixed temporary image
                tempimgs[i] = global::FlickrSync.Properties.Resources.Default;
                tempimgsmall[i] = global::FlickrSync.Properties.Resources.Default;
            }

            // set the images to display on the UI
            imageListLarge.Images.AddRange(tempimgs);
            imageListSmall.Images.AddRange(tempimgsmall);

            // TODO: Investigate further - get the default photoset image?
            for (int i = 0; i < allsets.Length; i++)
            {
                Photoset psi = allsets[i];
                Image img = ri.PhotosetThumbnail(psi);
                imageListLarge.Images[i] = img;
                imageListLarge.Images.SetKeyName(i, psi.PhotosetId);
                imageListSmall.Images[i] = img.GetThumbnailImage(16, 16, null, IntPtr.Zero);
                imageListSmall.Images.SetKeyName(i, psi.PhotosetId);

                ListViewItem lvi = listSets.Items.Add(psi.PhotosetId, psi.Title, psi.PhotosetId);
                lvi.SubItems.Add(psi.NumberOfPhotos.ToString());
                lvi.SubItems.Add(psi.Description);

                // update the progress meter
                if (progress != null)
                {
                    progress.Value = i;
                }

                // update photos counter
                photos += psi.NumberOfPhotos;
            }

            labelStatus.Text = string.Format("Sets on Flickr: {0}       Photos on Flickr: {1}", allsets.Length, photos);
        }

        /// <summary>
        /// Check sets still exist
        /// </summary>
        public void ValidateSets()
        {
            ArrayList ToRemove=new ArrayList();

            // check if each local folder still has a corresponding set on Flickr
            foreach (SyncFolder sf in li.GetSyncFolders())
            {
                bool found = false;
                foreach (Photoset psi in ri.GetAllSets())
                {
                    if (sf.SetId == psi.PhotosetId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found && sf.SetId!="")
                {
                    if (messages_level != MessagesLevel.MessagesNone && MessageBox.Show("Folder " + sf.FolderPath + " is configured to synchronize with a Set that does not exist (ID " +
                        sf.SetId + "). Delete from configuration?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        ToRemove.Add(sf.FolderPath);
                        logger.Info(sf.FolderPath + " deleted from configuration");
                    }
                    else
                        logger.Info(sf.FolderPath + " configured to synchronize with a set that does not exists");
                }
            }

            foreach (string path in ToRemove) 
                li.Remove(path);

            if (ToRemove.Count > 0)
                li.SaveToXML();
        }

        /// <summary>
        /// Get authentication token from Flickr
        /// </summary>
        /// <returns>string of the token if can get one; otherwise, an empty string</returns>
        private string FlickrGetToken()
        {
            try
            {
                // use the FlickrNet libarary with the API key
                Flickr f = new Flickr(Properties.Settings.Default.FlickrApiKey, Properties.Settings.Default.FlickrShared);
                f.Proxy = GetProxy(true);

                // Frob to identify the login session (http://www.flickr.com/services/api/auth.howto.desktop.html)
                string Frob = f.AuthGetFrob();
                string url = f.AuthCalcUrl(Frob, AuthLevel.Read | AuthLevel.Write | AuthLevel.Delete);
                
                // launch the webbrowser on the for the user to authenticate the application with their account
                System.Diagnostics.Process.Start(url);

                // ask the user to confirm the have done this so we can proceed
                if (messages_level==MessagesLevel.MessagesNone || MessageBox.Show("Please authorize FlickrSync to access your Flickr account in the automatically opened browser window, then click OK to confirm this.", "Confirmation", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    // get and set the authentication token
                    Auth auth = f.AuthGetToken(Frob);
                    Properties.Settings.Default.FlickrToken = auth.Token;
                    SaveConfig(); // save the token 
                    return Properties.Settings.Default.FlickrToken;
                }
            }
            catch (Exception e)
            {
                // error message if can't get the token
                if (Properties.Settings.Default.FlickrToken == "")
                    Error("Unable to obtain Flickr Token", e, ErrorType.Connect);
                else
                    Error("Error obtaining Flickr Token", e, ErrorType.Normal);
            }

            return "";
        }


        #endregion

        #region Methods

        /// <summary>
        /// Load the config file
        /// </summary>
        public static void LoadConfig()
        {
            XmlDocument FlickrSyncXml = new XmlDocument();

            try
            {
                // TODO: document new file location for settings and logs
                string xmlfilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/FlickrSync/FlickrSync.Config." + user + ".XML";

                if (File.Exists(xmlfilepath))
                {
                    FlickrSyncXml.Load(xmlfilepath); // TODO: why @xmlfilepath - will this still work?
                }
            }
            catch (Exception ex)
            {
                Error("Problem loading configuration file.",ex,ErrorType.FatalError);
            }

            Properties.Settings.Default.LocalInfoXml = FlickrSyncXml;
        }

        /// <summary>
        /// Save the config file
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                Properties.Settings.Default.Save();
                
                // if the user doesn't exist then don't save
                if (user==null || user== "") return;

                string xmlfilepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/FlickrSync/FlickrSync.Config."+user+".XML";

                Properties.Settings.Default.LocalInfoXml.Save(@xmlfilepath);
            }
            catch (Exception ex)
            {
                Error("Error saving configuration", ex, ErrorType.Normal);
            }
        }

        /// <summary>
        /// Try to get the Flickr user name
        /// </summary>
        private void UpdateUser()
        {
            try
            {
                user = ri.User();
                Text = "FlickrSync (" + user + ")";
            }
            catch (Exception ex)
            {
                Error("Error obtaining user name", ex, ErrorType.Normal);
            }
        }

        /// <summary>
        /// Reload information about local folders and trees and Flickr
        /// </summary>
        public void Reload()
        {
            // try to to get information about local files
            try
            {
                li = new LocalInfo();
                li.LoadFromXML(Properties.Settings.Default.LocalInfoXml);
                
                treeFolders.Initialize();
                foreach (PathInfo pi in li.GetPathInfoList())
                    if (pi.ManualAdd)
                        treeFolders.AddBaseNode(pi.Path);

                ExpandOpenFolders();
            }
            catch (Exception ex)
            {
                Error("Error initializing local folders Window", ex, ErrorType.FatalError);
            }

            // try to get remote
            try
            {
                // TODO: window which appears at start - this startup process will need to change to make the program independent from Flickr
                AboutBox about = new AboutBox("PLEASE WAIT... LOADING YOUR SETS");
                
                // TODO: autorun code
                if (!autorun)
                    about.Show();

                Application.DoEvents(); // http://msdn.microsoft.com/en-us/library/system.windows.forms.application.doevents.aspx

                Flickr.CacheDisabled = true;
                ri = new RemoteInfo();
                Flickr.CacheDisabled = false; // ri already has the sets lists. Cache can be used for thumbnails
                
                // clear display elements
                listSets.Items.Clear();
                imageListLarge.Images.Clear();
                imageListSmall.Images.Clear();

                // TODO: autorun code
                if (autorun)
                    FillListSet(null);
                else
                    FillListSet(about.GetProgressBar());

                // check the sets
                ValidateSets();

                Application.DoEvents();

                // close the about window
                about.Close();
            }
            catch (Exception ex)
            {
                Error("Error obtaining flickr sets", ex, ErrorType.FatalError);
            }

            CalcTooltips();
            UpdateStatusBar();
        }

        /// <summary>
        /// Create tooltips of folder path
        /// </summary>
        public void CalcTooltips()
        {
            for (int i = 0; i < listSets.Items.Count; i++)
            {
                string ttt = "";
                SyncFolder sf = FlickrSync.li.GetSyncFolderBySet(listSets.Items[i].Name);
                if (sf!=null) 
                    ttt = sf.FolderPath;

                listSets.Items[i].ToolTipText = ttt;
            }
        }

        /// <summary>
        /// Update status bar (TODO: fix duplicated code from other methods)
        /// </summary>
        public void UpdateStatusBar()
        {
            int photos = 0;
            Photoset[] allsets = ri.GetAllSets();

            for (int i = 0; i < allsets.Length; i++)
                photos += allsets[i].NumberOfPhotos;

            labelStatus.Text = string.Format("Sets on Flickr: {0}       Photos on Flickr: {1}", allsets.Length, photos);
        }

        /// <summary>
        /// Add new sets
        /// </summary>
        /// <param name="SetIds">Ids of sets to be added</param>
        public void AddNewSets(ArrayList SetIds)
        {

            // TODO: duplicated code so rewrite as a function
            foreach (string SetId in SetIds)
            {
                Flickr.CacheDisabled = true;
                Photoset psi = ri.GetSet(SetId);
                Flickr.CacheDisabled = false;
                if (psi != null)
                {
                    Image img = ri.PhotosetThumbnail(psi);
                    imageListLarge.Images.Add(SetId, img);
                    imageListSmall.Images.Add(SetId, img.GetThumbnailImage(16, 16, null, IntPtr.Zero));
                    
                    ListViewItem lvi = listSets.Items.Insert(0, SetId, psi.Title, SetId);
                    lvi.SubItems.Add(psi.NumberOfPhotos.ToString());
                    lvi.SubItems.Add(psi.Description);
                }
            }

            listSets.EnsureVisible(0);

            ri.Reload();
            CalcTooltips();
            UpdateStatusBar();
        }

        /// <summary>
        /// Get list of images
        /// </summary>
        /// <returns>ImageList of large images</returns>
        public ImageList GetImageList()
        {
            return imageListLarge;
        }

        /// <summary>
        /// View and sync - brings up the SyncView
        /// </summary>
        /// <param name="sf_col"></param>
        public void ViewAndSync(ArrayList sf_col)
        {
            if (sf_col.Count > 0)
            {
                // TODO: Create a progress meter when generating sync tasks
                labelCalc.Visible = true;
                labelCalc.BringToFront();

                // create a new SyncView object passing the array of SyncFolders
                SyncView sv = new SyncView(sf_col);
                
                // hide the message again
                labelCalc.Visible = false;

                // if there is a valid SyncView then display it
                if (sv!=null && !sv.IsDisposed)
                    sv.ShowDialog();

                this.Visible = true;
            }
        }

        /// <summary>
        /// Default view and sync (this is duplication we could just have the
        /// above function and pass it a null and check this to do what the
        /// below function does)
        /// </summary>
        public void ViewAndSync()
        {

            labelCalc.Visible = true;
            labelCalc.BringToFront();
            SyncView sv = new SyncView(li.GetSyncFolders());
            labelCalc.Visible = false;

            if (sv != null && !sv.IsDisposed)
                sv.ShowDialog(this);
            this.Visible = true;
        }

        /// <summary>
        /// TODO: it looks like this method is where the class gets the login details but the name doesn't suggest this
        /// </summary>
        /// <param name="interactive"></param>
        /// <returns></returns>
        static public WebProxy GetProxy(bool interactive)
        {
            if (!Properties.Settings.Default.ProxyUse)
                return null;

            string domain_user = Properties.Settings.Default.ProxyUser;
            string pass = Properties.Settings.Default.ProxyPass;
            if (pass=="")
                pass = proxy_password;

            if (pass=="") {
                if (interactive)
                {
                    Login l = new Login(domain_user, pass);
                    if (l.ShowDialog() != DialogResult.OK)
                        return null;

                    if (!Properties.Settings.Default.ProxyUse)
                        return null;

                    domain_user = l.GetUser();
                    pass = l.GetPass();
                }

                Properties.Settings.Default.ProxyUser = domain_user;
                Properties.Settings.Default.Save();

                proxy_password = pass;
            }

            string domain;
            string proxyuser;

            if (domain_user.Contains(@"\")) {
                int pos=domain_user.IndexOf('\\');
                domain = domain_user.Substring(0,pos);
                proxyuser = domain_user.Substring(pos + 1, domain_user.Length - pos - 1);
            }
            else 
            {
                domain="";
                proxyuser=domain_user;
            }

            try {
                WebProxy proxyObject = new WebProxy(Properties.Settings.Default.ProxyHost, Int16.Parse(Properties.Settings.Default.ProxyPort));
                proxyObject.Credentials = new NetworkCredential(proxyuser, pass, domain);
                return proxyObject;
            } 
            catch(Exception ex) 
            {
                FlickrSync.Error("Error connecting to Proxy", ex, ErrorType.Connect);
                return null;
            }
        }

        /// <summary>
        /// Calculate which folders are open in the TreeList
        /// </summary>
        private void CalcOpenFolders()
        {
            ArrayList to_remove = new ArrayList();

            // reset all information about open folders to current view
            foreach (PathInfo pi in li.GetPathInfoList())
            {
                pi.Open = false;
                if (pi.IsEmpty())
                    to_remove.Add(pi.Path);
            }

            foreach (string str in to_remove)
                li.RemovePath(str);

            foreach (TreeNode node in treeFolders.Nodes)
                if (node.IsExpanded)

                    // set open folders as open?
                    SetOpenFolders(node);
        }

        /// <summary>
        /// Set the PathInfo for open folders to open
        /// </summary>
        /// <param name="node">given TreeNode</param>
        private void SetOpenFolders(TreeNode node)
        {
            PathInfo pi = li.GetPathInfo(node.Name);

            // either add the path name or set it to open
            if (pi == null)
                li.AddPath(node.Name, true, false);
            else
                pi.Open = true;

            foreach (TreeNode subnode in node.Nodes)
                if (subnode.IsExpanded)

                    // recurse through sub nodes in the TreeList
                    SetOpenFolders(subnode);
        }

        /// <summary>
        /// Expand open folders in the TreeList
        /// </summary>
        private void ExpandOpenFolders()
        {
            foreach (PathInfo pi in li.GetPathInfoList())
            {
                if (pi.Open)
                {
                    foreach (TreeNode node in treeFolders.Nodes.Find(pi.Path, true))
                        node.Expand();
                }
            }

            if (treeFolders.Nodes.Count > 0)
                treeFolders.Nodes[0].EnsureVisible();
        }

        #endregion

        #region Events

        // Events do belong in this class but maybe some of the code in the methods should be moved out where
        // applicable

        /// <summary>
        /// TODO: tree folders stuff to investigate if time permits
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeFolders_AfterCheck(object sender, TreeViewEventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            if (e.Node.Checked)
            {
                if (syncprop_status == SyncPropertiesStatus.CancelAll)
                {
                    e.Node.Checked = false;

                    treeFolders.RefreshTreeNodes(treeFolders.Nodes);
                    CalcTooltips();
                    Cursor = Cursors.Default;
                    return;
                }

                SyncFolder sf = new SyncFolder(e.Node.Name);
                
                //try to match Set name
                foreach (Photoset ps in FlickrSync.ri.GetAllSets())
                {
                    if (ps.Title.Equals(Path.GetFileName(e.Node.Name),StringComparison.CurrentCultureIgnoreCase))
                    {
                        sf.SetId = ps.PhotosetId;
                        break;
                    }
                }
                if (sf.SetId == "")
                    sf.SetTitle = Path.GetFileName(e.Node.Name);

                if (syncprop_status == SyncPropertiesStatus.OKAll)
                {
                    li.Add(sf);
                }
                else
                {
                    SyncFolderForm sff = new SyncFolderForm(sf);
                    if (syncprop_status == SyncPropertiesStatus.MultipleUndef)
                        sff.SetMultiple(true);

                    DialogResult dr=sff.ShowDialog();

                    if (dr == DialogResult.OK || dr==DialogResult.Yes)  //Yes means OK to All
                        li.Add(sf);
                    else
                        e.Node.Checked = false;

                    if (dr == DialogResult.Yes && syncprop_status == SyncPropertiesStatus.MultipleUndef)
                        syncprop_status = SyncPropertiesStatus.OKAll;

                    if (dr == DialogResult.Abort) //CancelAll
                    {
                        syncprop_status = SyncPropertiesStatus.CancelAll;

                        treeFolders.RefreshTreeNodes(treeFolders.Nodes);
                        CalcTooltips();
                        Cursor = Cursors.Default;
                        return;
                    }
                }
            }
            else
                li.Remove(e.Node.Name);

            treeFolders.RefreshTreeNodes(treeFolders.Nodes);
            CalcTooltips();
            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Open the folder in Windows explorer which has been double clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeFolders_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = e.Node.Name;
            proc.Start();
        }

        /// <summary>
        /// Begin a drag and drop operation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeFolders_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }

        /// <summary>
        /// Show list dragging effect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listSets_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        /// <summary>
        /// Drag a folder from the TreeList over the thumbnail of a Flickr set to associate them
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listSets_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("System.Windows.Forms.TreeNode", false))
            {
                Point p = listSets.PointToClient(new Point(e.X, e.Y));
                ListViewItem lv = listSets.GetItemAt(p.X, p.Y);
                TreeNode node=(TreeNode)e.Data.GetData("System.Windows.Forms.TreeNode");
                if (lv != null && node != null)
                {
                    li.Associate(node.Name, lv.Name);
                    string setname = ri.GetSet(lv.Name).Title;
                    MessageBox.Show(node.Name + " associated to set " + setname, "Information");
                }
            }
        }

        /// <summary>
        /// Double click on a Flickr set thumbnail opens the set in a web browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listSets_DoubleClick(object sender, EventArgs e)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = ri.GetPhotosetURL(listSets.FocusedItem.Name);
            proc.Start();
        }

        /// <summary>
        /// Make the SyncView calculating message appear in the centre of the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlickrSync_Resize(object sender, EventArgs e)
        {
            labelCalc.Left = this.Width / 2 - labelCalc.Width/2;
            labelCalc.Top = this.Height / 2 - labelCalc.Height/2;
        }

        /// <summary>
        /// Enable View Log menu item on the File menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewLogMenuItem_Paint(object sender, PaintEventArgs e)
        {
            ViewLogMenuItem.Enabled = (log_level != LogLevel.LogNone);
        }

        /// <summary>
        /// Get the right click mouse position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeFolders_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                MouseDownPos = new Point(e.X, e.Y);
        }

        #endregion

        // TODO: investigate these UI methods more closely

        #region Menu Options        
        
        /// <summary>
        /// Create a new user by going through set up steps again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newUserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FlickrGetToken();

            // ri is needed to get the user. TODO: Get User without using ri
            ri = new RemoteInfo();
            
            UpdateUser();
            LoadConfig();
            Reload();
        }

        /// <summary>
        /// Save configuration 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CalcOpenFolders();
            li.SaveToXML();
        }

        /// <summary>
        /// Exit the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {           
            if (Properties.Settings.Default.AutoSave)
            {
                // save syncfolder settings
                li.SaveToXML();
            }

            this.Close();
        }

        /// <summary>
        /// Event listener when the form is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlickrSync_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Properties.Settings.Default.AutoSave)
            {
                // save syncfolder settings
                li.SaveToXML();
            }
        }

        /// <summary>
        /// View and Sync All
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewAndSync();
        }

        /// <summary>
        /// Reload all the Sets from Flickr
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void reloadSetsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reload();
        }

        /// <summary>
        /// Show the About box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox("");
            about.Show();
        }

        /// <summary>
        /// Show the set config window when a node is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void configToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node;
            if (MouseDownPos == null)
            {
                Point org = new Point(contextMenuLocalFolder.Left, contextMenuLocalFolder.Top);
                Point p = treeFolders.PointToClient(org);
                node = treeFolders.GetNodeAt(p.X, p.Y);
            }
            else
                node = treeFolders.GetNodeAt(MouseDownPos.X, MouseDownPos.Y);

            SyncFolder sf = null;

            if (node != null)
                sf = li.GetSyncFolder(node.Name);

            if (sf != null)
            {
                SyncFolderForm sff = new SyncFolderForm(sf);
                sff.ShowDialog();
            }
        }

        /// <summary>
        /// Open the local folder configuration window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void configSetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point org = new Point(contextMenuSets.Left, contextMenuSets.Top);
            Point p = listSets.PointToClient(org);
            ListViewItem lvi = listSets.GetItemAt(p.X, p.Y);
            SyncFolder sf = null;

            if (lvi != null)
                sf = FlickrSync.li.GetSyncFolderBySet(lvi.Name);

            if (sf != null)
            {
                SyncFolderForm sff = new SyncFolderForm(sf);
                sff.ShowDialog();
            }
        }

        /// <summary>
        /// View and Sync selected nodes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewAndSyncToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArrayList sf_col = new ArrayList();
            foreach (TreeNode tn in treeFolders.SelectedNodes)
            {
                SyncFolder sf = li.GetSyncFolder(tn.Name);
                if (sf != null)
                    sf_col.Add(sf);
            }

            ViewAndSync(sf_col);
        }

        /// <summary>
        /// View and Sync selected sets
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewAndSyncSetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArrayList sf_col=new ArrayList();

            foreach(ListViewItem lvi in listSets.SelectedItems) {
                SyncFolder sf=FlickrSync.li.GetSyncFolderBySet(lvi.Name);
                if (sf != null)
                    sf_col.Add(sf);
            }

            ViewAndSync(sf_col);
        }

        /// <summary>
        /// Build a list of child nodes to sync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuitemSyncChildren_Click(object sender, EventArgs e)
        {
            ArrayList sf_col = new ArrayList();
            foreach (TreeNode tn in treeFolders.SelectedNodes)
                BuildSyncChildrenList(sf_col, tn);

            ViewAndSync(sf_col);
        }

        /// <summary>
        /// Add a new Sync Folder based on given path
        /// </summary>
        /// <param name="list">list of folders to sync</param>
        /// <param name="path">path of folder to add</param>
        private void AddSyncFolder(ArrayList list, String path)
        {
            SyncFolder sf = li.GetSyncFolder(path);
            if (sf != null)
            {
                bool new_item=true;
                foreach(SyncFolder sf_org in list)
                    if (sf.Equals(sf_org))
                    {
                        new_item = false;
                        break;
                    }

                if (new_item)
                    list.Add(sf);
            }
        }

        /// <summary>
        /// Build list of children to sync
        /// </summary>
        /// <param name="list">list of folders to sync</param>
        /// <param name="tn">treenode to check</param>
        private void BuildSyncChildrenList(ArrayList list, TreeNode tn)
        {
            treeFolders.ExpandNode(tn);
            AddSyncFolder(list, tn.Name);

            for (int i = 0; i < tn.Nodes.Count; i++) {
                AddSyncFolder(list, tn.Nodes[i].Name);

                treeFolders.ExpandNode(tn.Nodes[i]);
                if (tn.Nodes[i].Nodes.Count > 0 && tn.Nodes[i].Nodes[0].Text!="") 
                    BuildSyncChildrenList(list, tn.Nodes[i]);
            }
        }

        /// <summary>
        /// Open the preferences window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Preferences pref = new Preferences();
            if (pref.ShowDialog() == DialogResult.OK)
            {
                messages_level = FlickrSync.StringToMsgLevel(Properties.Settings.Default.MessageLevel);
                log_level = FlickrSync.StringToLogLevel(Properties.Settings.Default.LogLevel);
                if (autorun)
                    messages_level = MessagesLevel.MessagesNone;
            }
        }

        /// <summary>
        /// Open the Help menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.HelpMain);
        }

        /// <summary>
        /// Event handler to control the list view format for the Flickr sets in the right hand pane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuitemViews_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            menuitemDetails.Checked = false;
            menuitemLargeIcons.Checked = false;
            menuitemSmallIcons.Checked = false;
            menuitemList.Checked = false;
            menuitemTile.Checked = false;

            ToolStripMenuItem tsmi = e.ClickedItem as ToolStripMenuItem;
            if (tsmi != null) {
                tsmi.Checked = true;
                listSets.View = (View)Enum.Parse(typeof(View), (string)tsmi.Tag);
            }

        }

        /// <summary>
        /// Event handler to control the list view format for the Flickr sets in the right hand pane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuitemViews_ButtonClick(object sender, EventArgs e)
        {
            int nNewView = (int)listSets.View;
            nNewView++;
            nNewView %= ((int)View.Tile) + 1;
            listSets.View = (View)nNewView;

            menuitemDetails.Checked = listSets.View == View.Details;
            menuitemLargeIcons.Checked = listSets.View == View.LargeIcon;
            menuitemSmallIcons.Checked = listSets.View == View.SmallIcon;
            menuitemList.Checked = listSets.View == View.List;
            menuitemTile.Checked = listSets.View == View.Tile;
        }

        /// <summary>
        /// Check folder and all sub folders
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int maxlevel = 0;

            syncprop_status = SyncPropertiesStatus.MultipleUndef;

            if (treeFolders.SelectedNode != null) 
                foreach(TreeNode tn in treeFolders.SelectedNodes) 
                    CheckAllChildren(tn, true, 0, ref maxlevel);

            syncprop_status = SyncPropertiesStatus.Default;
        }

        /// <summary>
        /// Uncheck all selected items on the TreeList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void uncheckAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int maxlevel = 0;

            syncprop_status = SyncPropertiesStatus.MultipleUndef;

            if (treeFolders.SelectedNode != null) 
                if (MessageBox.Show("Are you sure you want to uncheck all children folders?","Confirmation",MessageBoxButtons.OKCancel)==DialogResult.OK)
                    foreach (TreeNode tn in treeFolders.SelectedNodes)
                        CheckAllChildren(tn, false, 0, ref maxlevel);

            syncprop_status = SyncPropertiesStatus.Default;
        }


        /// <summary>
        /// Check all child nodes (need to confirm)
        /// maxlevel<=0 - ask if you want to check just the 1st level
        /// </summary>
        /// <param name="tn"></param>
        /// <param name="bValue"></param>
        /// <param name="level"></param>
        /// <param name="maxlevel"></param>
        private void CheckAllChildren(TreeNode tn, bool bValue, int level, ref int maxlevel)
        {
            treeFolders.ExpandNode(tn);
            level++;

            for (int i = 0; i < tn.Nodes.Count; i++) {
                if (tn.Nodes[i].Checked!=bValue)
                    tn.Nodes[i].Checked = bValue;

                if (level < maxlevel || maxlevel <= 0)
                {
                    treeFolders.ExpandNode(tn.Nodes[i]);

                    if (tn.Nodes[i].Nodes.Count > 0 && tn.Nodes[i].Nodes[0].Text != "")
                    {
                        bool syncnext = true;
                        if (level == 1 && maxlevel <= 0)
                        {
                            string msg = String.Format("Do you want to {0}check all folder levels (folders within folders)?", bValue ? "" : "un");

                            if (MessageBox.Show(msg, "Confirmation", MessageBoxButtons.YesNo) == DialogResult.No)
                            {
                                syncnext = false;
                                maxlevel = 1;
                            }
                            else
                                maxlevel = System.Int32.MaxValue;
                        }

                        if (syncnext)
                            CheckAllChildren(tn.Nodes[i], bValue, level, ref maxlevel);
                    }
                }
            }
        }

        /// <summary>
        /// Automatically select the right node on right click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextMenuLocalFolder_Opening(object sender, CancelEventArgs e)
        {
            TreeNode tn = treeFolders.GetNodeAt(treeFolders.PointToClient(Cursor.Position));
            if (tn != null)
                treeFolders.SelectedNode = tn;

            // Enable Properties only if node is checked
            ToolStripItem[] tsi=contextMenuLocalFolder.Items.Find("configToolStripMenuItem", false);
            if (tsi.GetLength(0)>0 && tn!=null)
                tsi[0].Enabled = tn.Checked;

            // Enable View And Sync if any selected node is checked
            tsi = contextMenuLocalFolder.Items.Find("viewAndSyncToolStripMenuItem", false);
            if (tsi.GetLength(0) > 0)
            {
                bool isselected = false;
                foreach (TreeNode tn2 in treeFolders.SelectedNodes)
                    if (tn2.Checked)
                    {
                        isselected = true;
                        break;
                    }

                tsi[0].Enabled = isselected;
            }

            // Hide "Remove Path" menu is this is not a manually added network path
            tsi = contextMenuLocalFolder.Items.Find("removePathToolStripMenuItem", false);
            PathInfo pi=li.GetPathInfo(tn.Name);
            tsi[0].Visible = (pi != null && pi.ManualAdd);
        }

        /// <summary>
        /// Enable items only if they are already known about
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextMenuSets_Opening(object sender, CancelEventArgs e)
        {
            Point org = new Point(contextMenuSets.Left, contextMenuSets.Top);
            Point p = listSets.PointToClient(org);
            ListViewItem lvi = listSets.GetItemAt(p.X, p.Y);
            SyncFolder sf = null;

            if (lvi != null)
                sf = FlickrSync.li.GetSyncFolderBySet(lvi.Name);

            // Enable Properties only if set is under FlickrSync configuration
            ToolStripItem[] tsi=contextMenuSets.Items.Find("ConfigSetToolStripMenuItem", false);
            if (tsi.GetLength(0)>0)
                tsi[0].Enabled = (sf!=null);

            // Enable View and Sync only if set is under FlickrSync configuration
            tsi = contextMenuSets.Items.Find("ViewAndSyncSetToolStripMenuItem", false);
            if (tsi.GetLength(0) > 0)
                tsi[0].Enabled = (sf != null);
        }

        /// <summary>
        /// Save Config button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            li.SaveToXML();
        }

        /// <summary>
        /// View and Sync all button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewSync_Click(object sender, EventArgs e)
        {
            ViewAndSync();
        }

        /// <summary>
        /// Add a network path to include in the folder list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addNetworkPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select path to include on folder list";
            fbd.ShowNewFolderButton = false;
            fbd.ShowDialog();*/

            FolderBrowser fb = new FolderBrowser();
            fb.SetDescription("Select path to include on folder list");
            fb.SetNetworkSelect();

            if (fb.ShowDialog() == DialogResult.OK)
                if (fb.DirectoryPath == "")
                    Error("This network path is not valid for FlickrSync", null, ErrorType.Normal);
                else
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(fb.DirectoryPath);
                        if (!di.Exists)
                            Error("Network path is not accesible", null, ErrorType.Normal);
                        else
                        {
                            PathInfo pi = li.GetPathInfo(fb.DirectoryPath);
                            if (pi == null)
                                li.AddPath(fb.DirectoryPath, false, true);
                            else
                                pi.ManualAdd = true;

                            treeFolders.AddBaseNode(fb.DirectoryPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Error("Invalid network path", ex, ErrorType.Normal);
                    }
                }
        }

        /// <summary>
        /// Remove path 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node;
            if (MouseDownPos == null)
            {
                Point org = new Point(contextMenuLocalFolder.Left, contextMenuLocalFolder.Top);
                Point p = treeFolders.PointToClient(org);
                node = treeFolders.GetNodeAt(p.X, p.Y);
            }
            else
                node = treeFolders.GetNodeAt(MouseDownPos.X, MouseDownPos.Y);

            if (node != null)
            {
                li.RemovePath(node.Name);
                treeFolders.RemoveBaseNode(node.Name);

                ArrayList to_remove=new ArrayList();

                foreach (SyncFolder sf in li.GetSyncFolders())
                {
                    bool still_linked = false;

                    foreach (TreeNode bnode in treeFolders.Nodes)
                    {
                        if (sf.FolderPath.StartsWith(bnode.Name, StringComparison.CurrentCultureIgnoreCase)) {
                            still_linked = true;
                            break;
                        }
                    }

                    if (!still_linked)
                    {
                        if (MessageBox.Show("Folder " + sf.FolderPath + " is no longer visible. It should be removed from configuration. Do you want to remove it?", "Confirmation", MessageBoxButtons.OKCancel) == DialogResult.OK)
                            to_remove.Add(sf);
                    }
                }

                foreach (SyncFolder sf in to_remove)
                    li.Remove(sf.FolderPath);

                if (to_remove.Count > 0)
                    li.SaveToXML();
            }
        }

        /// <summary>
        /// Show autorun instructions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutorunMenuItem_Click(object sender, EventArgs e)
        {
            string cmd;
            cmd = "\"" + Application.ExecutablePath + "\" /auto";

            MessageBox.Show("To run automatically with no user interface, execute the following command:\n"+cmd+"\nThis information is already copied to the clipboard. Just paste it where needed","Information");
            Clipboard.SetText(cmd);
        }

        /// <summary>
        /// Show the log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewLogMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/FlickrSync/FlickrSyncLog.txt");
        }

        /// <summary>
        /// Open Flickr page to organise set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void organizeSetOnFlickrToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Point org = new Point(contextMenuSets.Left, contextMenuSets.Top);
            Point p = listSets.PointToClient(org);
            ListViewItem lvi = listSets.GetItemAt(p.X, p.Y);

            if (lvi != null)
                Process.Start(ri.GetPhotosetEditURL(lvi.Name));
        }

        /// <summary>
        /// Open donate page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DonateMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("To donate, please use Paypal. Any donation is welcomed to support future FlickrSync development and costs. If you donate, please send an email to flickrsync@gmail.com and any further donation requests will be hidden.\nThanks\nFlickrSync Team");
            Process.Start(Properties.Settings.Default.DonateUrl);
        }
        #endregion

        #region Check out Messages

        /// <summary>
        /// Get the parameter fromt he given strings
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        private string FindParam(string doc, string str)
        {
            try
            {
                int pos = doc.IndexOf(str);
                if (pos < 0) return "";
                pos += str.Length;

                int pos2 = doc.IndexOf('"', pos);
                if (pos2 < 0) return "";

                return doc.Substring(pos, pos2 - pos);
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// TODO: Navigate webpage - I think this is to do with getting the authenication token
        /// for logging in to FlickrSync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            try
            {
                FlickrSync.message_tested = true;

                string mess_id = FindParam(webBrowser1.DocumentText, @"<META name=""mess_id"" content=""message_id=");
                string mess_valid = FindParam(webBrowser1.DocumentText, @"<META name=""mess_valid"" content=""message_valid=");
                string mess_text = FindParam(webBrowser1.DocumentText, @"<META name=""mess_text"" content=""message_text="); 

                string hash_users = FindParam(webBrowser1.DocumentText, @"<META name=""hash_users"" content=""hash_users=");
                string[] hash_list = hash_users.Split(',');
                foreach (string str in hash_list)
                    HashUsers.Add(str);

                if (mess_valid == "1" && mess_id.CompareTo(Properties.Settings.Default.MessageId)>0)
                {
                    Properties.Settings.Default.MessageId = mess_id;
                    Properties.Settings.Default.Save();

                    if (mess_text != "" && mess_text != "none")
                    {
                        if (messages_level!=MessagesLevel.MessagesNone)
                            MessageBox.Show(mess_text, "FlickrSync Information");
                    }
                    else
                    {
                        System.Diagnostics.Process.Start(Properties.Settings.Default.MessageUrl);
                        Thread.Sleep(10000);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion
        
    }
}
