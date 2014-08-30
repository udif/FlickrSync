using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using FlickrNet;
using System.IO;

namespace FlickrSync
{
    /// <summary>
    /// Controls settings for each SyncFolder
    /// </summary>
    public partial class SyncFolderForm : Form
    {
        SyncFolder sf;
        bool allow_multiple = false;
        int star_rating = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pSyncFolder">the syncfolder which is being modified</param>
        public SyncFolderForm(SyncFolder pSyncFolder)
        {
            InitializeComponent();
            sf=pSyncFolder;
        }

        /// <summary>
        /// Changes the star rating for the sync filter
        /// </summary>
        private void UpdateStarRating()
        {
            bool showStarRating = ((SyncFolder.FilterTypes)comboBoxFilterType.SelectedIndex == SyncFolder.FilterTypes.FilterStarRating);

            pictureBoxStarRating0.Visible = showStarRating;
            pictureBoxStarRating1.Visible = showStarRating;
            pictureBoxStarRating2.Visible = showStarRating;
            pictureBoxStarRating3.Visible = showStarRating;
            pictureBoxStarRating4.Visible = showStarRating;
            pictureBoxStarRating5.Visible = showStarRating;
            labelStarRating.Visible = showStarRating;

            if (star_rating >= 1) 
                pictureBoxStarRating1.Image = Properties.Resources.StarPink; 
            else 
                pictureBoxStarRating1.Image = Properties.Resources.StarBlue;
            if (star_rating >= 2)
                pictureBoxStarRating2.Image = Properties.Resources.StarPink;
            else
                pictureBoxStarRating2.Image = Properties.Resources.StarBlue;
            if (star_rating >= 3)
                pictureBoxStarRating3.Image = Properties.Resources.StarPink;
            else
                pictureBoxStarRating3.Image = Properties.Resources.StarBlue;
            if (star_rating >= 4)
                pictureBoxStarRating4.Image = Properties.Resources.StarPink;
            else
                pictureBoxStarRating4.Image = Properties.Resources.StarBlue;
            if (star_rating >= 5)
                pictureBoxStarRating5.Image = Properties.Resources.StarPink;
            else
                pictureBoxStarRating5.Image = Properties.Resources.StarBlue;
        }

        /// <summary>
        /// Load the form information
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncFolderForm_Load(object sender, EventArgs e)
        {
            labelFolderPath.Text = sf.FolderPath;
            textBoxTitle.Text = sf.SetTitle;
            textBoxDescription.Text = sf.SetDescription;
            comboBoxMethod.SelectedIndex = (int) sf.SyncMethod;
            comboBoxFilterType.SelectedIndex = (int) sf.FilterType;
            comboBoxPermissions.SelectedIndex = (int)sf.Permission;
            checkBoxNoDelete.Checked = sf.NoDelete;
            checkBoxNoDeleteTags.Checked = sf.NoDeleteTags;
            comboBoxOrderType.SelectedIndex = (int)sf.OrderType;
            checkBoxNoInitialReplace.Checked = sf.NoInitialReplace;

            if (sf.FilterType == SyncFolder.FilterTypes.FilterIncludeTags)
            {
                labelTags.Visible = true;
                textBoxTags.Visible = true;
                textBoxTags.Text = sf.FilterTags;
                buttonTagList.Visible = true;
            }

            star_rating = sf.FilterStarRating;
            if (sf.FilterType == SyncFolder.FilterTypes.FilterStarRating)
                UpdateStarRating();

            listViewSet.LargeImageList = Program.MainWindow.GetImageList();

            try
            {
                foreach (Photoset psi in ((FlickrLocation)Program.RemoteLoc).GetAllSets())
                {
                    ListViewItem lvi = listViewSet.Items.Add(psi.PhotosetId, psi.Title, psi.PhotosetId);
                    if (psi.PhotosetId == sf.SetId)
                        lvi.Selected = true;
                }
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Error loading", ex, ErrorType.Normal);
                this.Close();
            }
        }

        /// <summary>
        /// Change title
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxTitle_TextChanged(object sender, EventArgs e)
        {
            if (textBoxTitle.Text != "")
            {
                listViewSet.Enabled = true;
                listViewSet.SelectedItems.Clear();
                labelSetTitle.Text = textBoxTitle.Text;
            }
            else
            {
                listViewSet.Enabled = true;
                if (listViewSet.SelectedItems.Count > 0)
                    labelSetTitle.Text = listViewSet.SelectedItems[0].Text;
                else
                    labelSetTitle.Text = "Choose one from the left or create a new one below (to be created on next sync)";
            }
        }

        /// <summary>
        // TODO: investigate what this does
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listViewSet_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewSet.SelectedItems.Count > 0)
            {
                labelSetTitle.Text = listViewSet.SelectedItems[0].Text;
                textBoxTitle.Text = "";
            }
            else
                if (textBoxTitle.Text != "")
                    labelSetTitle.Text = textBoxTitle.Text;
                else
                    labelSetTitle.Text = "Choose one from the left or create a new one below (to be created on next sync)";

        }

        /// <summary>
        /// Select Flickr set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (textBoxTitle.Text == "" && listViewSet.SelectedItems.Count == 0)
            {
                MessageBox.Show("You must select an existing flickr Set or create a new one");
                return;
            }

            if (textBoxTitle.Text != "")
            {
                sf.SetTitle = textBoxTitle.Text;
                sf.SetDescription = textBoxDescription.Text;
                sf.SetId = "";
            }
            else
            {
                sf.SetTitle = "";
                sf.SetDescription = "";
                sf.SetId = listViewSet.SelectedItems[0].Name;
            }
             
            sf.SyncMethod = (SyncFolder.Methods) comboBoxMethod.SelectedIndex;
            sf.Permission = (FlickrPermissions)comboBoxPermissions.SelectedIndex;
            sf.FilterType = (SyncFolder.FilterTypes)comboBoxFilterType.SelectedIndex;
            sf.FilterTags = textBoxTags.Text;
            sf.FilterStarRating = star_rating;
            sf.NoDelete = checkBoxNoDelete.Checked;
            sf.NoDeleteTags = checkBoxNoDeleteTags.Checked;
            sf.OrderType = (SyncFolder.OrderTypes)comboBoxOrderType.SelectedIndex;
            sf.NoInitialReplace = checkBoxNoInitialReplace.Checked;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Cancel dialogue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        // TODO: investigate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxFilterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool showFilterTag = ((SyncFolder.FilterTypes)comboBoxFilterType.SelectedIndex == SyncFolder.FilterTypes.FilterIncludeTags);

            labelTags.Visible = showFilterTag;
            textBoxTags.Visible = showFilterTag;
            buttonTagList.Visible = showFilterTag;

            UpdateStarRating();
        }

        /// <summary>
        // TODO: investigate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonTagList_Click(object sender, EventArgs e)
        {
            ArrayList taglist = new ArrayList();
            FileInfo[] files ={ };
            ImageInfo ii = new ImageInfo();

            try
            {
                DirectoryInfo dir = new DirectoryInfo(sf.FolderPath);

                string lookfor = "*.jpg;*.jpeg;*.gif;*.png;*.tif;*.tiff;*.bmp";
                string[] extensions = lookfor.Split(new char[] { ';' });

                ArrayList myfileinfos = new ArrayList();
                foreach (string ext in extensions)
                    myfileinfos.AddRange(dir.GetFiles(ext));

                files = (FileInfo[]) myfileinfos.ToArray(typeof(FileInfo));
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Error accessing path: " + sf.FolderPath, ex, ErrorType.Normal);
            }

            this.Cursor = Cursors.WaitCursor;

            foreach (FileInfo fi in files)
            {
                try
                {
                    ii.Load(fi.FullName, ImageInfo.FileTypes.FileTypeUnknown);
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    FlickrSync.Error("Error loading image information: " + fi.FullName, ex, ErrorType.Normal);
                    return;
                }

                foreach (string tag in ii.GetTagsArray())
                {
                    bool exists = false;
                    foreach (string tagexist in taglist)
                        if (tag == tagexist)
                        {
                            exists = true;
                            break;
                        }

                    if (!exists)
                        taglist.Add(tag);
                }
            }

            this.Cursor = Cursors.Default;

            SelectFromList listForm = new SelectFromList(taglist);
            listForm.ShowDialog();

            if (textBoxTags.Text == "")
                textBoxTags.Text = listForm.Selected;
            else
                textBoxTags.Text += "; " + listForm.Selected;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonHelp_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Properties.Settings.Default.HelpProperties);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mult"></param>
        public void SetMultiple(bool mult)
        {
            allow_multiple = mult;
            buttonCancelAll.Visible = allow_multiple;
            buttonOKAll.Visible = allow_multiple;
        }

        /// <summary>
        /// Set star rating to filter by
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBoxStarRating0_Click(object sender, EventArgs e)
        {
            star_rating = 0;
            UpdateStarRating();
        }

        private void pictureBoxStarRating1_Click(object sender, EventArgs e)
        {
            star_rating = 1;
            UpdateStarRating();
        }

        private void pictureBoxStarRating2_Click(object sender, EventArgs e)
        {
            star_rating = 2;
            UpdateStarRating();
        }

        private void pictureBoxStarRating3_Click(object sender, EventArgs e)
        {
            star_rating = 3;
            UpdateStarRating();
        }

        private void pictureBoxStarRating4_Click(object sender, EventArgs e)
        {
            star_rating = 4;
            UpdateStarRating();
        }

        private void pictureBoxStarRating5_Click(object sender, EventArgs e)
        {
            star_rating = 5;
            UpdateStarRating();
        }
    }
}