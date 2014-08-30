using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace FlickrSync
{
    /// <summary>
    /// Class for picking folder names from the file system
    /// </summary>
    public class FolderBrowser : FolderNameEditor
    {
        private FolderNameEditor.FolderBrowser m_obBrowser = null;
        private string m_strDescription;

        /// <summary>
        /// Default constructor
        /// </summary>
        public FolderBrowser()
        {
            m_strDescription = "Select path";
            m_obBrowser = new FolderNameEditor.FolderBrowser();
        }     

        // variable with getter
        public string DirectoryPath
        {
            get { return this.m_obBrowser.DirectoryPath; }
            private set {}
        }
          

        /// <summary>
        /// Show the dialogue to pick a folder
        /// </summary>
        /// <returns>TODO: find out what this returns</returns>
        public DialogResult ShowDialog()
        {
            m_obBrowser.Description = m_strDescription;
            return m_obBrowser.ShowDialog();
        }

        /// <summary>
        /// Select from network
        /// </summary>
        public void SetNetworkSelect()
        {
            m_obBrowser.Style = FolderBrowserStyles.ShowTextBox;
            m_obBrowser.StartLocation = FolderBrowserFolder.NetworkNeighborhood;
        }

        /// <summary>
        /// set the description (of the dialogue?)
        /// </summary>
        /// <param name="pDesc">Photo description string</param>
        public void SetDescription(string pDesc)
        {
            m_strDescription = pDesc;
        }
    }
}
