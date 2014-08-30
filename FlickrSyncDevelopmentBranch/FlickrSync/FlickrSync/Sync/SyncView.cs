using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using FlickrNet;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Imaging; // for EXIF thumbnail from JPEG

namespace FlickrSync
{

    /// <summary>
    /// The view window for the sync changes
    /// </summary>
    public partial class SyncView : Form
    {
        // 100 matches the Flickr thumbnail size
        const int ThumbnailSize = 100;

        struct ThumbnailTask
        {
            public string org;
            public string key;
            public bool local;
            public SyncItem.Actions action;
        };

        ArrayList SyncFolders;
        ArrayList SyncItems;
        static DateTime SyncDate;
        string AdsUrlPath;

        bool SyncStarted = false;
        bool SyncAborted = false;
        bool Finished = false;
        Thread SyncThread = null;
        Thread ThumbnailThread = null;

        ArrayList Tasks;
        ArrayList NewSets;

        private delegate void ChangeProgressBarCallBack(ProgressBars pb, ProgressValues type, int value, string status);
        private delegate void RefreshImagesCallBack(int index);
        private delegate void FinishCallBack();

        enum ProgressBars { PBSync = 0, PBPhoto };
        enum ProgressValues { PBValue = 0, PBMinimum, PBMaximum };

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pSyncFolders">SyncFolders to sync</param>
        public SyncView(ArrayList pSyncFolders)
        {
            InitializeComponent();
            SyncItems=new ArrayList();
            Tasks = new ArrayList();
            NewSets = new ArrayList();
            SyncFolders = pSyncFolders;
            AdsUrlPath = "";

            CalcSync();
        }

        // Thumbnail code based on: http://danbystrom.se/2009/01/05/imagegetthumbnailimage-and-beyond/
        // Could have made the resizing code better and higher quality but not necessary for just checking images

        /// <summary>
        /// Generate a thumbnail by opening the image and creating one
        /// </summary>
        /// <param name="filename">filename to extract thumbnail from</param>
        /// <param name="thumbWidth">width of the generated thumbnail</param>
        /// <returns>Bitmap thumnail</returns>
        private Bitmap GenerateThumbnail(string filename, int thumbWidth)
        {
            // use filestream to open the image (but not lock it open) then get the thumbnail
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (Image img = Image.FromStream(fs, true, false))
                {
                    // scaling
                    if (img.Height > img.Width)
                    {
                        return (Bitmap)img.GetThumbnailImage(thumbWidth * img.Width / img.Height , thumbWidth, null, IntPtr.Zero);
                    }

                    return (Bitmap)img.GetThumbnailImage(thumbWidth, thumbWidth * img.Height / img.Width, null, IntPtr.Zero);
                } 
            }    
        }

        /// <summary>
        /// Get the Exif thumnail if available otherwise get a normal one
        /// </summary>
        /// <param name="filename">filename to extract thumbnail from</param>
        /// <param name="thumbWidth">width of the generated thumbnail</param>
        /// <returns>Bitmap thumnail</returns>
        private Bitmap GetJpgThumbnail(string filename, int thumbWidth)
        {
            // open the file
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (Image img = Image.FromStream(fs, true, false))
                {
                    // go through the EXIF properties
                    foreach (PropertyItem pi in img.PropertyItems)

                        // if EXIF thumnail found then resize and return
                        if (pi.Id == 20507)
                        {
                            // scaling
                            if (img.Height > img.Width)
                            {
                                return new Bitmap(((Bitmap)Image.FromStream(new MemoryStream(pi.Value))), thumbWidth * img.Width / img.Height, thumbWidth);
                            }

                            return new Bitmap(((Bitmap)Image.FromStream(new MemoryStream(pi.Value))), thumbWidth, thumbWidth * img.Height / img.Width);
                        }
                }
            }
            
            // call method to generate thumnail if no EXIF
            return GenerateThumbnail(filename, thumbWidth);
        }
        
        /// <summary>
        /// Create a thumbnail
        /// </summary>
        /// <param name="filename">filename to extract thumbnail from</param>
        /// <param name="thumbWidth">width of the generated thumbnail</param>
        /// <returns>Bitmap thumnail</returns>
        private Bitmap GetThumbnail(string filename, int thumbWidth)
        {
            // check whether it is a JPG
            String extension = Path.GetExtension(filename);

            // in which case we can probably extract the thumnail from the EXIF property
            if (extension.ToLower().Equals(".jpg") || extension.ToLower().Equals(".jpeg"))
            {
                return GetJpgThumbnail(filename, thumbWidth);
            }
            
            // if it isn't a JPG then get the thumbnail the slower way
            return GenerateThumbnail(filename, thumbWidth);
        }

        /// <summary>
        /// Update the thumbnails
        /// </summary>
        private void UpdateThumbnails()

        {
            try
            {
               
                foreach (ThumbnailTask tt in Tasks)
                {
                    int index = imageList1.Images.IndexOfKey(tt.key);
                    if (index < 0) continue;

                    if (tt.local)
                    {
                        
                        try
                        {
                            // create the thumbnail and add it to the list
                            imageList1.Images[index] = GetThumbnail(tt.org, ThumbnailSize);
                        }
                        catch (Exception)
                        {
                        }  
                    }
                    else
                    {
                        try
                        {
                            /* Getting images from Flickr using could be sped up by using threading. In testing the below code was
                             * between 1x and 3x the speed of doing it sequentially. Usually it was at least twice as fast.
                             * 
                                Thread t = new Thread(delegate()
                                {
                                    imageList1.Images[index] = Bitmap.FromStream(FlickrSync.ri.PhotoThumbnail(tt.org));
                                    this.Invoke(new RefreshImagesCallBack(RefreshImages), new object[] { index });
                                });

                                // Kick off a new thread
                                t.IsBackground = true; // so it won't keep running if the application is closed...
                                t.Start(); // start the thread (it will end when its code its method is finished)
                             * 
                             * This link has more information on threading : http://www.albahari.com/threading/
                             * 
                             * The reason this is not enabled is due to the request limit for each Flickr API key of 3600 requests an
                             * hour PER APPLICATION KEY (http://www.flickr.com/services/developer/api/). This code currently (through
                             * FlickrNet) does 1 query per thumbnail so if there were enough users it could get banned. One alternative
                             * may be to get the urls of the entire set using http://code.flickr.com/blog/2008/08/19/standard-photos-response-apis-for-civilized-age/
                             * then do the processing of the relevant ones here - downloading an image doesn't count as a query AFAIK.
                             */                         
                          
                            imageList1.Images[index] = Bitmap.FromStream(FlickrSync.ri.PhotoThumbnail(tt.org));
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (tt.action == SyncItem.Actions.ActionNone)
                    {
                        Image img = (Image) imageList1.Images[index].Clone();
                        Graphics g = Graphics.FromImage(img);
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(192, Color.White)), new Rectangle(0, 0, ThumbnailSize, ThumbnailSize));
                        imageList1.Images[index] = img;
                    }

                    this.Invoke(new RefreshImagesCallBack(RefreshImages), new object[] { index });
                }
            }
            catch(Exception)
            {
            }
        }

        /// <summary>
        /// Thumbnail information
        /// </summary>
        /// <param name="org">the image URL on Flickr</param>
        /// <param name="key">the photo's position?</param>
        /// <param name="local">whether the image is local</param>
        /// <param name="action">action to be taken</param>
        /// <returns>the approriate placeholder thumbnail image</returns>
        private Image Thumbnail(string org,string key,bool local,SyncItem.Actions action)
        {
            // create thumbnail task
            ThumbnailTask tt;
            tt.org = org;
            tt.key = key;
            tt.local = local;
            tt.action = action;

            // add it to the tasks list
            Tasks.Add(tt);

            // return the appropriate placeholder thumbnail image depending on the action
            switch (tt.action)
            {
                case SyncItem.Actions.ActionUpload:
                    return Properties.Resources.New;
                case SyncItem.Actions.ActionReplace:
                    return Properties.Resources.Replace;
                case SyncItem.Actions.ActionDelete:
                    return Properties.Resources.Delete;
                case SyncItem.Actions.ActionNone:
                    return Properties.Resources.None;
            }

            return Properties.Resources.Flickrsync;
        }

        /// <summary>
        /// Repaint images to the window
        /// </summary>
        /// <param name="index">the ListView to refresh</param>
        private void RefreshImages(int index)
        {
            try
            {
                listViewToSync.Invalidate(listViewToSync.Items[index].Bounds);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Calculate what syncing operations are required
        /// </summary>
        private void CalcSync()
        {
            // set the cursor to the wait icon
            this.Cursor = Cursors.WaitCursor;
            this.Refresh();

            // disable the Flickr cache to make sure queries are relevant
            Flickr.CacheDisabled = true;
            
            // set the sync time to now
            SyncDate = DateTime.Now;

            // images counter and maximum file size Flickr will accept
            int count_images = 0;
            long max_size = FlickrSync.ri.MaxFileSize();

            // check settings whether thumbnails are enabled
            if (Properties.Settings.Default.UseThumbnailImages)
            {
                imageList1.Images.Clear();
                imageList1.ImageSize = new System.Drawing.Size(ThumbnailSize, ThumbnailSize);
            }

            // for each SyncFolder in our ArrayList of folders to sync
            foreach (SyncFolder sf in SyncFolders)
            {
                // Allows the UI to respond to events such as user clicks whilst continuing task
                // see: http://msdn.microsoft.com/en-us/library/system.windows.forms.application.doevents.aspx
                Application.DoEvents();

                // check if there is an associated set
                if (sf.SetId == "" && sf.SetTitle == "")
                {
                    this.Cursor = Cursors.Default;
                    if (FlickrSync.messages_level==FlickrSync.MessagesLevel.MessagesAll)
                        MessageBox.Show(sf.FolderPath + " has no associated Set", "Warning");
                    FlickrSync.logger.Info(sf.FolderPath + " has no associated set");
                    this.Cursor = Cursors.WaitCursor;
                    continue;
                }

                // make an array list of the photos in the SyncFolder
                ArrayList photos = new ArrayList();

                // if it is valid then add a PhotoInfo object for each photo to the photos ArrayList
                if (sf.SetId != "")
                {
                    try
                    {
                        foreach (Photo p in FlickrSync.ri.SetPhotos(sf.SetId))
                        {
                            // workaround since media type and p.MachineTags is not working on FlickrNet 2.1.5
                            if (p.Tags != null)
                            {

                                // TODO: fix this Tags is not giving the same output as CleanTags after FlickrNet upgrade

                                // create variables to store the tags from the collection
                                String pct = "";
                                string[] TagsArray = new string[p.Tags.Count];

                                // copy the tags to this array
                                p.Tags.CopyTo(TagsArray, 0);

                                // go through the array appending the string with the current array element
                                foreach (string s in TagsArray)
                                {
                                    pct += " " + s.ToLower();
                                }

                                if (pct.Contains("flickrsync:type=video") ||
                                    pct.Contains("flickrsync:cmd=skip") ||
                                    pct.Contains("flickrsync:type:video") ||
                                    pct.Contains("flickrsync:cmd:skip"))
                                    continue;
                            }

                            // create and set the values of the PhotoInfo object
                            PhotoInfo pi = new PhotoInfo();
                            pi.Title = p.Title;
                            pi.DateTaken = p.DateTaken;
                            pi.DateSync = sf.LastSync;
                            pi.DateUploaded = p.DateUploaded;
                            pi.PhotoId = p.PhotoId;
                            pi.Found = false;

                            // add to the list
                            photos.Add(pi);
                        }
                    }
                    catch (Exception ex)
                    {
                        FlickrSync.Error("Error loading information from Set " + sf.SetId, ex, FlickrSync.ErrorType.Normal);
                        Close();
                    }
                }

                // check the local file info about the photo objects
                FileInfo[] files ={ };

                try
                {
                    DirectoryInfo dir = new DirectoryInfo(sf.FolderPath);
                    if (!dir.Exists)
                    {
                        if (FlickrSync.messages_level != FlickrSync.MessagesLevel.MessagesNone)
                        {
                            if (MessageBox.Show("Folder " + sf.FolderPath + " no longer exists. Remove from configuration?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                FlickrSync.li.Remove(sf.FolderPath);
                                FlickrSync.logger.Info(sf.FolderPath + " deleted from configuration");
                            }
                            else
                                FlickrSync.logger.Info(sf.FolderPath + " does not exist");
                        }

                        continue;
                    }

                    int count = Properties.Settings.Default.Extensions.Count;
                    int total = 0;
                    FileInfo[][] files_ext = new FileInfo[count][];

                    foreach (string ext in Properties.Settings.Default.Extensions)
                    {
                        count--;
                        files_ext[count] = dir.GetFiles("*." + ext);
                        total += files_ext[count].GetLength(0);
                    }

                    files = new FileInfo[total];
                    total = 0;
                    foreach (string ext in Properties.Settings.Default.Extensions)
                    {
                        files_ext[count].CopyTo(files, total);
                        total += files_ext[count].GetLength(0);
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    FlickrSync.Error("Error accessing path: " + sf.FolderPath, ex, FlickrSync.ErrorType.Normal);
                    this.Close();
                }

                ImageInfo ii = new ImageInfo();

                string[] ftags = sf.FilterTags.Split(';');
                for (int i = 0; i < ftags.GetLength(0); i++)
                {
                    ftags[i] = ftags[i].Trim().ToLower();
                }

                // create a group for each individual set when displaying in the SyncView
                ListViewGroup group;

                if (sf.SetId == "")
                    group = new ListViewGroup("Folder: " + sf.FolderPath + "; Set: " + sf.SetTitle);
                else
                {
                    try
                    {
                        group = new ListViewGroup("Folder: " + sf.FolderPath + "; Set: " + FlickrSync.ri.GetPhotoset(sf.SetId).Title);
                    }
                    catch (Exception)
                    {
                        group = new ListViewGroup("Folder: " + sf.FolderPath + "; Set: " + sf.SetId);
                    }
                }

                // add the group to the ListView
                listViewToSync.Groups.Add(group);

                Array.Sort(files, new sortLastWriteHelper());

                // check the file against various criteria
                foreach (FileInfo fi in files)
                {
                    bool include = true;

                    if (sf.FilterType == SyncFolder.FilterTypes.FilterIncludeTags ||
                        sf.FilterType == SyncFolder.FilterTypes.FilterStarRating || 
                        sf.SyncMethod == SyncFolder.Methods.SyncDateTaken || 
                        sf.SyncMethod == SyncFolder.Methods.SyncTitleOrFilename)
                        ii.Load(fi.FullName, ImageInfo.FileTypes.FileTypeUnknown);

                    if (sf.FilterType == SyncFolder.FilterTypes.FilterIncludeTags)
                    {
                        include = false;

                        foreach (string tag in ii.GetTagsArray())
                        {
                            foreach (string tag2 in ftags)
                                if (tag.ToLower() == tag2)
                                {
                                    include = true;
                                    break;
                                }

                            if (include)
                                break;
                        }
                    }

                    if (sf.FilterType == SyncFolder.FilterTypes.FilterStarRating) 
                        if (sf.FilterStarRating>ii.StarRating)
                            include=false;

                    /*if (fi.Length > max_size)
                        include = false;*/

                    if (!include)
                        continue;

                    string name = fi.Name;
                    foreach (string ext in Properties.Settings.Default.Extensions)
                        if (fi.Name.EndsWith("." + ext, StringComparison.CurrentCultureIgnoreCase))
                            name = name.Remove(name.Length - 4);

                    if (name.EndsWith(" "))  // flickr removes ending space - need to replace with something else
                        name=name.Remove(name.Length-1).Insert(name.Length-1,@"|");

                    // Matching photos
                    int pos = -1;
                    bool found = false;

                    // TODO: various methods for matching local and remote photos - will need to modify this
                    foreach (PhotoInfo pi in photos)
                    {
                        pos++;

                        if (sf.SyncMethod == SyncFolder.Methods.SyncFilename && pi.Title == name)
                        {
                            found = true;
                            break;
                        }

                        if (sf.SyncMethod == SyncFolder.Methods.SyncDateTaken && pi.DateTaken == ii.DateTaken)
                        {
                            found = true;
                            break;
                        }

                        if (sf.SyncMethod == SyncFolder.Methods.SyncTitleOrFilename)
                        {
                            string title = ii.Title;
                            if (title==null || title == "") title = name;

                            if (pi.Title == title)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    // what to do if photo isn't found
                    if (!found)
                    {
                        SyncItem si = new SyncItem();
                        si.Action = SyncItem.Actions.ActionUpload;

                        if (fi.Length > max_size)
                            si.Action = SyncItem.Actions.ActionNone;
                        
                        si.Filename = fi.FullName;
                        si.SetId = sf.SetId;
                        si.SetTitle = sf.SetTitle;
                        si.SetDescription = sf.SetDescription;

                        if (ii.FileName != fi.FullName)
                            ii.Load(fi.FullName, ImageInfo.FileTypes.FileTypeUnknown);

                        if (ii.Title != "" && sf.SyncMethod != SyncFolder.Methods.SyncFilename)
                            si.Title = ii.Title;
                        else
                            si.Title = name;

                        si.Description = ii.Description;
                        si.Tags = ii.GetTagsArray();

                        if (ii.City != "")
                            si.Tags.Add(ii.City);

                        if (ii.Country != "")
                            si.Tags.Add(ii.Country);

                        si.GeoLat = ii.GetGeo(true);
                        si.GeoLong = ii.GetGeo(false);

                        si.FolderPath = sf.FolderPath;
                        si.Permission = sf.Permission;

                        int position = 0;

                        if (Properties.Settings.Default.UseThumbnailImages)
                        {
                            imageList1.Images.Add(count_images.ToString(), Thumbnail(fi.FullName, count_images.ToString(), true, si.Action));
                            position = count_images;
                            count_images++;
                        }

                        ListViewItem lvi;
                        if (si.Action != SyncItem.Actions.ActionNone)
                        {
                            lvi = listViewToSync.Items.Add("NEW: " + fi.Name, position);
                            lvi.ForeColor = Color.Blue;
                        }
                        else
                        {
                            lvi = listViewToSync.Items.Add("SKIP: " + fi.Name, position);
                            lvi.ForeColor = Color.LightGray;
                        }

                        lvi.ToolTipText = lvi.Text + " " + group.Header;
                        group.Items.Add(lvi);

                        si.item_id = lvi.Index;
                        SyncItems.Add(si);
                    }
                    else
                    {
                        ((PhotoInfo)photos[pos]).Found = true;

                        // Compare time is based on local info DateSync because flickr clock could be misaligned with local clock
                        DateTime compare = ((PhotoInfo)photos[pos]).DateSync;
                        if (compare == new DateTime(2000, 1, 1))
                            compare = ((PhotoInfo)photos[pos]).DateUploaded;

                        if (compare < fi.LastWriteTime)
                        {
                            // TODO: duplicated code from above so should be able to write seperate function to handle this
                            SyncItem si = new SyncItem();
                            si.Action = SyncItem.Actions.ActionReplace;

                            if (sf.LastSync == (new DateTime(2000, 1, 1)) && sf.NoInitialReplace)
                                si.Action = SyncItem.Actions.ActionNone;

                            si.Filename = fi.FullName;
                            si.PhotoId = ((PhotoInfo)photos[pos]).PhotoId;
                            si.SetId = sf.SetId;
                            si.SetTitle = "";
                            si.SetDescription = "";
                            si.Permission = sf.Permission;
                            si.NoDeleteTags = sf.NoDeleteTags;

                            if (ii.FileName != fi.FullName)
                                ii.Load(fi.FullName, ImageInfo.FileTypes.FileTypeUnknown);

                            if (ii.Title != "" && sf.SyncMethod != SyncFolder.Methods.SyncFilename)
                                si.Title = ii.Title;
                            else
                                si.Title = name;

                            si.Description = ii.Description;
                            si.Tags = ii.GetTagsArray();

                            if (ii.City != "")
                                si.Tags.Add(ii.City);

                            if (ii.Country != "")
                                si.Tags.Add(ii.Country);

                            si.GeoLat = ii.GetGeo(true);
                            si.GeoLong = ii.GetGeo(false);

                            int position = 0;
                            if (Properties.Settings.Default.UseThumbnailImages)
                            {
                                imageList1.Images.Add(count_images.ToString(), Thumbnail(fi.FullName, count_images.ToString(), true, SyncItem.Actions.ActionReplace));
                                position = count_images;
                                count_images++;
                            }

                            ListViewItem lvi;
                            if (si.Action != SyncItem.Actions.ActionNone)
                            {
                                lvi = listViewToSync.Items.Add("REPLACE: " + fi.Name, position);
                                lvi.ForeColor = Color.Black;
                            }
                            else
                            {
                                lvi = listViewToSync.Items.Add("SKIP: " + fi.Name, position);
                                lvi.ForeColor = Color.LightGray;
                            }
                            lvi.ToolTipText = lvi.Text+" "+group.Header;
                            group.Items.Add(lvi);

                            si.item_id = lvi.Index;
                            SyncItems.Add(si);
                        }
                    }
                }

                if (!sf.NoDelete)
                {
                    foreach (PhotoInfo pi in photos)
                    {
                        if (!pi.Found)
                        {
                            SyncItem si = new SyncItem();
                            si.Action = SyncItem.Actions.ActionDelete;
                            si.PhotoId = pi.PhotoId;
                            si.Title = pi.Title;
                            si.SetId = sf.SetId;

                            int position = 0;
                            if (Properties.Settings.Default.UseThumbnailImages)
                            {
                                imageList1.Images.Add(count_images.ToString(), Thumbnail(pi.PhotoId, count_images.ToString(), false, SyncItem.Actions.ActionDelete));
                                position = count_images;
                                count_images++;
                            }

                            ListViewItem lvi = listViewToSync.Items.Add("DELETE: " + pi.Title,position);
                            lvi.ForeColor = Color.Red;
                            lvi.ToolTipText = lvi.Text + " " + group.Header;
                            group.Items.Add(lvi);

                            si.item_id = lvi.Index;
                            SyncItems.Add(si);
                        }
                    }
                }
            }

            FlickrSync.logger.Info("Prepared Synchronization successful");

            Flickr.CacheDisabled = false;
            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Load the SyncView window. If there isn't anything to sync then close it, otherwise
        /// create a thread to update the thumbnails
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncView_Load(object sender, EventArgs e)
        {
            if (SyncItems.Count == 0)
            {
                if (FlickrSync.messages_level != FlickrSync.MessagesLevel.MessagesNone) 
                    MessageBox.Show("Nothing to do", "FlickrSync Information");

                FlickrSync.logger.Warn("Nothing to do");

                this.Close();
                return;
            }

            Program.MainFlickrSync.Visible = false;
            WindowState = FormWindowState.Maximized;          

            ThreadStart ts = new ThreadStart(UpdateThumbnails);
            ThumbnailThread = new Thread(ts);
            ThumbnailThread.IsBackground = true;
            ThumbnailThread.Start();
        }

        /// <summary>
        /// Update the progress bar
        /// </summary>
        /// <param name="pb"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        private void ChangeProgressBar(ProgressBars pb, ProgressValues type, int value)
        {
            ChangeProgressBar(pb, type, value, null);
        }

        /// <summary>
        /// Update progress bars with image upload progress
        /// </summary>
        /// <param name="pb"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="status"></param>
        private void ChangeProgressBar(ProgressBars pb, ProgressValues type, int value, string status)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new ChangeProgressBarCallBack(ChangeProgressBar), new object[] { pb, type, value, status });
                }
                else
                {
                    if ((status != null) && (status.Length > 0))
                    {
                        labelSync.Text = "Synchronizing: " + status;
                    }
                    ProgressBar p;
                    if (pb == ProgressBars.PBSync)
                        p = progressBarSync;
                    else
                        p = progressBarPhoto;

                    switch (type)
                    {
                        case ProgressValues.PBValue:
                            if (value < p.Minimum)
                                value = p.Minimum;
                            if (value > p.Maximum)
                                value = p.Maximum;

                            p.Value = value;
                            break;
                        case ProgressValues.PBMinimum:
                            p.Minimum = value;
                            break;
                        case ProgressValues.PBMaximum:
                            p.Maximum = value;
                            break;
                    }

                    if (pb == ProgressBars.PBSync)
                    {
                        int percent = 0;
                        if (p.Maximum != 0)
                            percent = p.Value * 100 / p.Maximum;

                        if (percent < 0)
                            percent = 0;
                        if (percent > 100)
                            percent = 100;

                        this.Text = "FlickrSync Synchronizing..." + percent.ToString() + @"%";
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Change Flickr upload progress
        /// </summary>
        /// <param name="value"></param>
        private void FlickrProgress(int value)
        {
            ChangeProgressBar(ProgressBars.PBPhoto, ProgressValues.PBValue, value);
        }

        /// <summary>
        /// Mark thumbnail action as completed e.g. draw a check mark
        /// </summary>
        /// <param name="index">the image ID</param>
        private void MarkCompleted(int index)
        {
            try
            {
                ListViewItem lvi=listViewToSync.Items[index];

                lvi.ForeColor = SystemColors.WindowText;
                lvi.BackColor = Color.FromArgb(220, 250, 220);

                if (Properties.Settings.Default.UseThumbnailImages)
                {
                    Image img = (Image)lvi.ImageList.Images[lvi.ImageIndex].Clone();
                    Graphics g = Graphics.FromImage(img);
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(192, Color.White)), new Rectangle(0, 0, ThumbnailSize, ThumbnailSize));
                    g.DrawImage(Properties.Resources.Check, ThumbnailSize, ThumbnailSize);
                    lvi.ImageList.Images[lvi.ImageIndex] = img;
                }

                listViewToSync.Invalidate(listViewToSync.Items[index].Bounds);
            }
            catch (Exception)
            {
            }
        }

        #region - Execute Sync -

        /// <summary>
        /// Gets SyncFolder from ArrayList of folders to sync
        /// </summary>
        /// <param name="SetId">the SetId of the desired SyncFolder</param>
        /// <returns>the requested SyncFolder if it is in the ArrayList; otherwise, null</returns>
        private SyncFolder GetSyncFolder(string SetId)
        {
            for (int i = 0; i < SyncFolders.Count; i++)
                if (((SyncFolder)SyncFolders[i]).SetId == SetId)
                    return (SyncFolder)SyncFolders[i];

            return null;
        }

        /// <summary>
        /// Get the FileInfo for the filename
        /// </summary>
        /// <param name="filename">given filename</param>
        /// <returns>the FileInfo object for the filename if it exists; otherwise, null</returns>
        private FileInfo GetFileInfo(string filename)
        {
            FileInfo fi = null;

            try
            {
                fi = new FileInfo(filename);
            }
            catch (Exception)
            {
            }

            return fi;
        }

        /// <summary>
        /// Sorts a provided SyncFolder by its sorting criteria
        /// </summary>
        /// <param name="sf">the SyncFolder to sort</param>
        private void Sort(SyncFolder sf)
        {
            if (sf.OrderType==SyncFolder.OrderTypes.OrderDefault)
                return;

            try
            {
                Photo[] photolist = FlickrSync.ri.SetPhotos(sf.SetId);
                ArrayList photo_array=new ArrayList();
                foreach (Photo p in photolist)
                    photo_array.Add(p);

                switch (sf.OrderType)
                {
                    case SyncFolder.OrderTypes.OrderDateTaken:
                        photo_array.Sort(new PhotoSortDateTaken());
                        break;
                    case SyncFolder.OrderTypes.OrderTitle:
                        photo_array.Sort(new PhotoSortTitle());
                        break;
                    case SyncFolder.OrderTypes.OrderTag:
                        photo_array.Sort(new PhotoSortTag());
                        break;
                }

                string[] ids = new string[photo_array.Count];

                for (int i = 0; i < photo_array.Count; i++)
                    ids[i] = ((Photo)photo_array[i]).PhotoId;

                FlickrSync.ri.SortSet(sf.SetId, ids);
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Error sorting set "+sf.SetTitle+" ("+sf.SetId+")", ex, FlickrSync.ErrorType.Info);
            }
        }

        // TODO: examine logic for file synchronisations here

        /// <summary>
        /// Execute the sync process
        /// </summary>
        /// TODO: if there is the time could this be made to use multiple threads for speed?
        /// Flickr seems to set upload limits which are lower than a decent upload speed 
        /// can provide so multiple uploads might speed it up - need to check how this would
        /// fit in with their guidelines however...        
        public void ExecuteSync()
        {
            try
            {
                // reset the ProgressBar
                ChangeProgressBar(ProgressBars.PBSync, ProgressValues.PBMinimum, 0);
                ChangeProgressBar(ProgressBars.PBSync, ProgressValues.PBMaximum, SyncItems.Count);
                int pos = 0;

                string CurrentSetId = "";
                bool ReplaceError = false;

                DateTime SyncProgressDate = new DateTime(2000, 1, 1);

                // set the date to the last Sync data of the current SyncFolder
                if (SyncFolders.Count>0)
                    SyncProgressDate=((SyncFolder)SyncFolders[0]).LastSync;

                // go through each SyncItem performing required operations
                foreach (SyncItem si in SyncItems)
                {
                    // abort the loop and finish the sync
                    if (SyncAborted)
                    {
                        Finish();
                        return;
                    }

                    // logging messages
                    string logmsg="";
                    switch(si.Action) {
                        case SyncItem.Actions.ActionDelete:
                            logmsg = "To Execute: Delete " + si.Title;
                            break;
                        case SyncItem.Actions.ActionNone:
                            logmsg="To Execute: Skip "+si.Filename;
                            break;
                        case SyncItem.Actions.ActionReplace:
                            logmsg="To Execute: Replace "+si.Filename;
                            break;
                        case SyncItem.Actions.ActionUpload:
                            logmsg="To Execute: New "+si.Filename;
                            break;
                    }

                    FlickrSync.logger.Info(logmsg);

                    // if the action isn't to delete the image then get the FileInfo
                    FileInfo fi = null;
                    if (si.Action != SyncItem.Actions.ActionDelete)
                        fi = GetFileInfo(si.Filename);


                    if (si.SetId != CurrentSetId)
                    {
                        if (ReplaceError)
                            ReplaceError = false;
                        else
                        {
                            // CurrentSetId synchronization is finished on previous set
                            SyncFolder sfCurrent = GetSyncFolder(CurrentSetId);
                            if (sfCurrent != null)
                            {
                                sfCurrent.LastSync = SyncDate;
                                Sort(sfCurrent);
                            }

                            SyncFolder sfNew = GetSyncFolder(si.SetId);
                            if (sfNew != null)
                                SyncProgressDate = sfNew.LastSync;
                        }

                        CurrentSetId = si.SetId;
                    }
                    else
                    {
                        // Since replaces are controlled from the date of last synchronization, when there is an error on replace, 
                        // sync should not continue. Otherwise the file in error would never get updated
                        if (ReplaceError)
                        {
                            continue;
                        }
                        else if (si.Action != SyncItem.Actions.ActionDelete)
                        {
                            SyncFolder sfCurrent = GetSyncFolder(CurrentSetId);

                            if (sfCurrent!=null && fi!=null && SyncProgressDate < fi.LastWriteTime) //Only updates LastSync to the last date if the new file is more recent than the last one
                                sfCurrent.LastSync = SyncProgressDate;
                        }
                    }

                    ChangeProgressBar(ProgressBars.PBPhoto, ProgressValues.PBMinimum, 0, si.SetTitle + " " + si.Filename);
                    if (fi != null)
                        ChangeProgressBar(ProgressBars.PBPhoto, ProgressValues.PBMaximum, (int)fi.Length);

                    FlickrSync.ri.SetProgress(new RemoteInfo.SetProgressType(FlickrProgress));

                    switch (si.Action)
                    {
                        case SyncItem.Actions.ActionUpload:
                            string PhotoId = "";

                            bool retry = true;
                            int retrycount=0;

                            //sometimes upload may fail, so retry a few times before giving up
                            while (retry)
                            {
                                try
                                {
                                    PhotoId = FlickrSync.ri.UploadPicture(si.Filename, si.Title, si.Description, si.Tags, si.Permission);
                                    retry = false;
                                }
                                catch (Exception ex)
                                {
                                    if (retrycount <= 1)
                                        retrycount++;
                                    else
                                    {
                                        FlickrSync.Error("Error uploading picture to flickr: " + si.Filename, ex, FlickrSync.ErrorType.Normal);
                                        SyncAborted = true;
                                        retry = false;
                                        break;
                                    }
                                }
                            }

                            if (SyncAborted)
                                break;

                            if (si.SetId != "")
                            {
                                retry = true; //associating the photo might fail if it's done right after uploading
                                retrycount = 0;

                                while (retry)
                                {
                                    try
                                    {
                                        FlickrSync.ri.PhotosetsAddPhoto(si.SetId, PhotoId);
                                        retry = false;
                                    }
                                    catch (FlickrApiException ex)
                                    {
                                        if (ex.Code == 3)    // Code 3 means Photo already in set - not really a problem
                                        {
                                            retry = false;
                                        }
                                        else
                                        {                       //only ask for user confirmation after retrying (retry several times if messages are disabled).
                                            if ((retrycount>0 && FlickrSync.messages_level == FlickrSync.MessagesLevel.MessagesAll) ||
                                                (retrycount>3 && FlickrSync.messages_level!=FlickrSync.MessagesLevel.MessagesAll))
                                            {
                                                DialogResult resp = DialogResult.Abort;
                                                if (FlickrSync.messages_level == FlickrSync.MessagesLevel.MessagesAll)
                                                    resp=MessageBox.Show("Error associating photo to set: " + si.Filename + "\n" + ex.Message + "\nDo you want to continue?", "Error", MessageBoxButtons.AbortRetryIgnore);
                                                FlickrSync.logger.Error("Error associating photo to set: " + si.Filename + "; " + ex.Message);

                                                if (resp == DialogResult.Abort)
                                                {
                                                    SyncAborted = true;
                                                    break;
                                                }

                                                if (resp == DialogResult.Ignore)
                                                    retry = false;
                                            }
                                            else
                                                Thread.Sleep(5000);

                                            retrycount++;
                                        }
                                    }
                                    catch (Exception ex2) // all other exceptions do the same
                                    {                     //only ask for user confirmation after retrying (retry several times if messages are disabled).
                                        if ((retrycount>0 && FlickrSync.messages_level == FlickrSync.MessagesLevel.MessagesAll) ||
                                            (retrycount>3 && FlickrSync.messages_level!=FlickrSync.MessagesLevel.MessagesAll))
                                        {
                                            DialogResult resp = DialogResult.Abort;
                                            if (FlickrSync.messages_level == FlickrSync.MessagesLevel.MessagesAll)
                                                resp=MessageBox.Show("Error associating photo to set: " + si.Filename + "\n" + ex2.Message + "\nDo you want to continue?", "Error", MessageBoxButtons.AbortRetryIgnore);
                                            FlickrSync.logger.Error("Error associating photo to set: " + si.Filename + "; " + ex2.Message);

                                            if (resp == DialogResult.Abort)
                                            {
                                                SyncAborted = true;
                                                break;
                                            }

                                            if (resp == DialogResult.Ignore)
                                                retry = false;
                                        }
                                        else
                                            Thread.Sleep(5000);

                                        retrycount++;
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    Photoset ps = FlickrSync.ri.CreateSet(si.SetTitle, si.SetDescription, PhotoId);
                                    if (ps != null)
                                    {
                                        CurrentSetId = ps.PhotosetId;
                                        string Title = si.SetTitle;
                                        string Description = si.SetDescription;

                                        for (int i = 0; i < SyncItems.Count; i++)
                                        {
                                            SyncItem si2 = (SyncItem)SyncItems[i];

                                            if (si2.SetTitle == Title && si2.SetDescription == Description)
                                            {
                                                si2.SetId = ps.PhotosetId;
                                                si2.SetTitle = "";
                                                si2.SetDescription = "";
                                            }
                                        }

                                        FlickrSync.li.Associate(si.FolderPath, ps.PhotosetId);
                                        NewSets.Add(ps.PhotosetId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    FlickrSync.logger.Error("Error creating set: " + si.SetTitle + "; " + ex.Message);
                                    if (FlickrSync.messages_level!=FlickrSync.MessagesLevel.MessagesAll || MessageBox.Show("Error creating set: " + si.SetTitle + "\n" + ex.Message + "\nDo you want to continue?", "Error", MessageBoxButtons.OKCancel) != DialogResult.OK)
                                    {
                                        SyncAborted = true;
                                        break;
                                    }
                                }

                            }

                            break;

                        case SyncItem.Actions.ActionReplace:
                            try
                            {
                                Flickr.CacheDisabled = true;
                                FlickrSync.ri.ReplacePicture(si.Filename, si.PhotoId, si.Title, si.Description, si.Tags, si.Permission, si.NoDeleteTags, si.GeoLat, si.GeoLong);
                                Flickr.CacheDisabled = false;
                            }
                            catch (Exception ex)
                            {
                                FlickrSync.Error("Error replacing picture: " + si.Filename + " - Skipping this Set", ex, FlickrSync.ErrorType.Normal);
                                ReplaceError = true;
                            }

                            break;

                        case SyncItem.Actions.ActionDelete:
                            try
                            {
                                FlickrSync.ri.DeletePicture(si.PhotoId);
                            }
                            catch (Exception ex)
                            {
                                FlickrSync.Error("Error deleting picture: " + si.PhotoId, ex, FlickrSync.ErrorType.Normal);
                            }
                            break;

                    } // end switch on action

                    FlickrSync.ri.ClearProgress();

                    if (!SyncAborted)
                    {
                        try
                        {
                            pos++;
                            ChangeProgressBar(ProgressBars.PBSync, ProgressValues.PBValue, pos);

                            //if (Properties.Settings.Default.UseThumbnailImages) {
                            this.Invoke(new RefreshImagesCallBack(MarkCompleted), new object[] { si.item_id });
                            //}
                        }
                        catch (Exception)
                        {
                        }

                        if (!ReplaceError && si.Action!=SyncItem.Actions.ActionDelete && fi!=null)
                            SyncProgressDate = fi.LastWriteTime;
                    }
                } // end for each syncItem

                if (!ReplaceError)
                {
                    // CurrentSetId synchronization is finished
                    SyncFolder sfCurrent = GetSyncFolder(CurrentSetId);
                    if (sfCurrent != null)
                    {
                        sfCurrent.LastSync = SyncDate;
                        Sort(sfCurrent);
                    }
                }

                Finish();
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Error during synchronization", ex, FlickrSync.ErrorType.Normal);
                SyncAborted = true;
                Finish();
            }
        }
        #endregion

        /// <summary>
        /// View the log 
        /// </summary>
        /// <param name="id">???</param>
        /// <returns>false</returns>
        /// (not sure why this takes an Id which it does nothing with and why it 
        /// has to return something which can't change...)
        private bool ViewLog(int id)
        {
            Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/FlickrSync/FlickrSyncLog.txt");
            return false;
        }

        /// <summary>
        /// Operations to finish a sync process
        /// </summary>
        private void Finish()
        {
            if (InvokeRequired)
            {
                // recursion
                BeginInvoke(new FinishCallBack(Finish));
            }
            else
            {
                if (Finished)
                    return;

                Finished = true;

                if (SyncStarted)
                {
                    if (ThumbnailThread != null && ThumbnailThread.IsAlive)
                        ThumbnailThread.Abort();

                    string msg;
                    if (SyncAborted)
                        msg = "Synchronization aborted.";
                    else
                        msg = "Synchronization finished.";

                    try
                    {
                        FlickrSync.li.SaveToXML();
                        labelSync.Text = "";

                        FlickrSync.logger.Warn(msg + " Updated configuration saved");

                        if (FlickrSync.messages_level != FlickrSync.MessagesLevel.MessagesNone)
                        {
                            //MessageBox.Show(msg + " Updated configuration saved.", "Sync");
                            CustomMsgBox msgbox = new CustomMsgBox(msg + " Updated configuration saved.","FlickrSync");
                            msgbox.AddButton("OK", 75, 1,msgbox.ButtonCallOK);
                            if (FlickrSync.log_level!=FlickrSync.LogLevel.LogNone)
                                msgbox.AddButton("View Log", 75, 2, ViewLog);
                            msgbox.ShowDialog();
                        }
                    }
                    catch (Exception ex)
                    {
                        FlickrSync.Error(msg + " Error saving configuration", ex, FlickrSync.ErrorType.Normal);
                    }

                    if (NewSets.Count > 0)
                        Program.MainFlickrSync.AddNewSets(NewSets);
                }

                this.Close();
            }
        }

        /// <summary>
        /// Start the sync process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSync_Click(object sender, EventArgs e)
        {
            labelSync.Text = "Synchronizing. Please Wait...";
            buttonSync.Visible=false;

            //show ads
            bool show=true;
            string hash = FlickrSync.ri.UserId().GetHashCode().ToString("X");
            foreach (string str in FlickrSync.HashUsers)
            {
                if (str == hash)
                {
                    show = false;
                    break;
                }

                if (str == "VERSION" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version)
                {
                    show = false;
                    break;
                }
            }

            if (show)
            {
                webBrowserAds.Height = 70;
                webBrowserAds.Width = listViewToSync.Width;
                webBrowserAds.Visible = true;
                listViewToSync.Height = listViewToSync.Height - webBrowserAds.Height - 5;
            }

            this.Text = "FlickrSync Synchronizing...0%";

            ThreadStart ts = new ThreadStart(ExecuteSync);
            SyncThread = new Thread(ts);
            SyncStarted = true;
            SyncThread.Start();
        }

        /// <summary>
        /// Cancel the sync process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            SyncAborted = true;
            Finish();
        }

        /// <summary>
        /// Cancel the sync process (if the window is closed)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncView_FormClosed(object sender, FormClosedEventArgs e)
        {
            SyncAborted = true;
            Finish();
        }
                
        /// <summary>
        /// listViewToSync_DrawItem is not currently used since listViewToSync is not ownerDraw
        /// this is maintained for future extension
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewToSync_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Rectangle r=listViewToSync.GetItemRect(e.ItemIndex, ItemBoundsPortion.Icon);
            r.X = r.X + (r.Width - ThumbnailSize) / 2;
            r.Y = r.Y + (r.Height - ThumbnailSize) / 2;
            r.Width = ThumbnailSize;
            r.Height = ThumbnailSize;

            e.Graphics.DrawImage(e.Item.ImageList.Images[e.Item.ImageIndex], r);

            if (listViewToSync.View != View.Details)
            {
                StringFormat fmt = new StringFormat();
                fmt.Trimming = StringTrimming.EllipsisCharacter;
                fmt.FormatFlags = StringFormatFlags.LineLimit;
                fmt.Alignment = StringAlignment.Center;
 
                Rectangle r2 = e.Item.Bounds;
                r2.Y = r.Y + r.Height + 1;
                r2.Height = e.Item.Bounds.Bottom - r2.Y;

                e.Graphics.DrawString(e.Item.Text, listViewToSync.Font, new SolidBrush(e.Item.ForeColor), r2, fmt);
            }
        }

        /// <summary>
        /// Advertising
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void webBrowserAds_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (webBrowserAds.Visible && e.Url.ToString()!=Properties.Settings.Default.AdUrl && e.Url.LocalPath!=AdsUrlPath)
            {
                e.Cancel = true;
                System.Diagnostics.Process.Start(e.Url.ToString());
            }

            if (AdsUrlPath == "")
                AdsUrlPath = e.Url.LocalPath;
        }
    }
}
