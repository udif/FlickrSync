using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Controls
{
    class TreeFoldersToSync : TreeFolders
    {
        public override bool Exists(string path)
        {
            return FlickrSync.Program.LocalLoc.Exists(path);
        }

        public override bool Includes(string path)
        {
            return FlickrSync.Program.LocalLoc.Includes(path);
        }
        public override string ToolTipText(string path)
        {
            try
            {
                return ((FlickrSync.FlickrLocation) FlickrSync.Program.RemoteLoc).GetPhotosetTitle(FlickrSync.Program.LocalLoc.GetSetId(path));
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
