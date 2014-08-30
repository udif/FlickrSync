using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FlickrSync
{
    /// <summary>
    /// Handle the start up of the program and allow the user to log in, in a 
    /// service agnostic manner
    /// </summary>
    partial class Startup : Form
    {
        #region Properties

        // change this variable to keep the application open e.g. when a sync service is selected
        bool ExitProgramOnFormClose = true;

        #endregion

        #region Constructor

        public Startup()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        #endregion

        #region EventListeners

        private void Startup_Load(object sender, EventArgs e)
        {            

        }

        /// <summary>
        /// Form Closed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Startup_Closed(object sender, FormClosedEventArgs e)
        {
            // if the form closes and this has not been made false e.g. when a button event is triggered then
            // the application will exit
            if (ExitProgramOnFormClose)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// Login using Flickr
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void flickrButton_Click(object sender, EventArgs e)
        {
            // Create a new Flickr authentication class (it will assign itself as the RemoteAuth provider)
            new FlickrAuth();

            // Call the login method
            Program.RemoteAuth.Login();

            // Don't exit the application when this form closes
            ExitProgramOnFormClose = false;

            // Close this window
            this.Dispose();
        }

        /// <summary>
        /// Access preferences to change settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void preferencesButton_Click(object sender, EventArgs e)
        {
            // need to make sure we have loaded preferences - do this in preferences window?
            Preferences pref = new Preferences();
            pref.ShowDialog();
        }

        /// <summary>
        /// Get the progress bar
        /// </summary>
        /// <returns>the progress bar</returns>
        public ProgressBar GetProgressBar()
        {
            return progressBar;
        }

        #endregion        
    }
}