using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace FlickrSync
{

    public enum SyncPropertiesStatus { Default = 0, MultipleUndef, OKAll, CancelAll };

    abstract class SyncLocation
    {
        #region Properties

        protected static string FolderSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/FlickrSync/SyncFolder.Generic.Config.xml";

        #endregion

        #region HelperMethods

        /// <summary>
        /// Load the sync folders config file
        /// </summary>
        public static void LoadSyncFolderConfig()
        {
            try
            {
                if (File.Exists(FolderSavePath))
                {
                     Properties.Settings.Default.LocalInfoXml.Load(FolderSavePath);
                }
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Problem loading configuration file.", ex, ErrorType.FatalError);
            }
        }

        /// <summary>
        /// Save the config file
        /// </summary>
        public void SaveSyncFolderConfig()
        {
            try
            {
                Properties.Settings.Default.LocalInfoXml = Program.LocalLoc.GetSyncXML();
                Properties.Settings.Default.LocalInfoXml.Save(FolderSavePath);             
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Error saving configuration", ex, ErrorType.Normal);
            }
        }

        #endregion
    }
}
