using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NLog;

namespace FlickrSync
{
    public partial class Preferences : Form
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Preferences()
        {
            InitializeComponent();
        }

        #region HelperMethods

        /// <summary>
        /// Reconfigure the logger based on settings changes
        /// </summary>
        private void changeLoggerType()
        {
            // if the user has a logging setting enabled create the appropriate logger type         
            if (Properties.Settings.Default.LogLevel.Equals("LogNone"))
            {
                FlickrSync.logger = LogManager.GetLogger("LogNone");
            }
            else if (Properties.Settings.Default.LogLevel.Equals("LogBasic"))
            {
                FlickrSync.logger = LogManager.GetLogger("LogBasic");
            }
            else if (Properties.Settings.Default.LogLevel.Equals("LogAll"))
            {
                FlickrSync.logger = LogManager.GetLogger("LogAll");
            }            
            else if (Properties.Settings.Default.LogLevel.Equals("LogDebug"))
            {
                FlickrSync.logger = LogManager.GetLogger("LogDebug");
            }
        }

        /// <summary>
        /// Set proxy state
        /// </summary>
        private void SetProxyState()
        {
            textBoxProxyHost.Enabled = checkBoxUseProxy.Checked;
            textBoxProxyPass.Enabled = checkBoxUseProxy.Checked;
            textBoxProxyPort.Enabled = checkBoxUseProxy.Checked;
            textBoxProxyUser.Enabled = checkBoxUseProxy.Checked;
            labelPassword.Enabled = checkBoxUseProxy.Checked;
            labelPort.Enabled = checkBoxUseProxy.Checked;
            labelUser.Enabled = checkBoxUseProxy.Checked;
        }

        #endregion

        #region EventListeners

        /// <summary>
        /// Load existing preferences from settings file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Preferences_Load(object sender, EventArgs e)
        {
            comboBoxMethod.SelectedIndex = (int) SyncFolder.StringToMethod(Properties.Settings.Default.Method);
            comboBoxOrderType.SelectedIndex = (int)SyncFolder.StringToOrderType(Properties.Settings.Default.OrderType);
            checkBoxNoDelete.Checked = Properties.Settings.Default.NoDelete;
            checkBoxNoDeleteTags.Checked = Properties.Settings.Default.NoDeleteTags;
            checkBoxShowThumbnailImages.Checked = Properties.Settings.Default.UseThumbnailImages;

            checkBoxUseProxy.Checked = Properties.Settings.Default.ProxyUse;
            SetProxyState();
            textBoxProxyHost.Text = Properties.Settings.Default.ProxyHost;
            textBoxProxyPort.Text = Properties.Settings.Default.ProxyPort;
            textBoxProxyUser.Text = Properties.Settings.Default.ProxyUser;
            textBoxProxyPass.Text = Properties.Settings.Default.ProxyPass;

            comboBoxMsgLevel.SelectedIndex=(int) FlickrSync.StringToMsgLevel(Properties.Settings.Default.MessageLevel);
            comboBoxLogLevel.SelectedIndex = (int) FlickrSync.StringToLogLevel(Properties.Settings.Default.LogLevel);
            
            // Autosave checkbox
            checkBoxAutoSave.Checked = Properties.Settings.Default.AutoSave;
        }

        /// <summary>
        /// Save preferences to settings file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, EventArgs e)
        {
            if ((FlickrSync.LogLevel)comboBoxLogLevel.SelectedIndex == FlickrSync.LogLevel.LogNone &&
                (FlickrSync.MessagesLevel)comboBoxMsgLevel.SelectedIndex == FlickrSync.MessagesLevel.MessagesNone)
            {
                if (MessageBox.Show("It is highly recommended that you keep some type of information, either on application messages or on a log file. Are you sure you want to disable both?", "Confirmation", MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                    return;
            }

            Properties.Settings.Default.Method = ((SyncFolder.Methods)comboBoxMethod.SelectedIndex).ToString();
            Properties.Settings.Default.OrderType = ((SyncFolder.OrderTypes)comboBoxOrderType.SelectedIndex).ToString();
            Properties.Settings.Default.NoDelete = checkBoxNoDelete.Checked;
            Properties.Settings.Default.NoDeleteTags = checkBoxNoDeleteTags.Checked;
            Properties.Settings.Default.UseThumbnailImages = this.checkBoxShowThumbnailImages.Checked;
            Properties.Settings.Default.ProxyUse = checkBoxUseProxy.Checked;
            Properties.Settings.Default.ProxyHost = textBoxProxyHost.Text;
            Properties.Settings.Default.ProxyPort = textBoxProxyPort.Text;
            Properties.Settings.Default.ProxyUser = textBoxProxyUser.Text;
            Properties.Settings.Default.ProxyPass = textBoxProxyPass.Text;
            Properties.Settings.Default.MessageLevel = ((FlickrSync.MessagesLevel)comboBoxMsgLevel.SelectedIndex).ToString();

            // if the log level has changed then change the setting and the Logger
            if(!Properties.Settings.Default.LogLevel.Equals(((FlickrSync.LogLevel)comboBoxLogLevel.SelectedIndex).ToString())) 
            {
                Properties.Settings.Default.LogLevel = ((FlickrSync.LogLevel)comboBoxLogLevel.SelectedIndex).ToString();
                
                changeLoggerType();
            }            

            Properties.Settings.Default.AutoSave = checkBoxAutoSave.Checked;

            Properties.Settings.Default.Save();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// Cancel Form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Set proxy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxUseProxy_CheckedChanged(object sender, EventArgs e)
        {
            SetProxyState();
        }
      
        #endregion
    }
}