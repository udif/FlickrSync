using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Net;
using FlickrNet;
using System.Xml; // FlickrNet library

namespace FlickrSync
{
    public enum FlickrPermissions { PermDefault = 0, PermPublic, PermFamilyFriends, PermFriends, PermFamily, PermPrivate };    

    /// <summary>
    /// Store information about remote Flickr sets using the FlickrNet library
    /// </summary>
    class FlickrLocation : SyncLocation
    {

        #region Properties
        
        PhotosetCollection AllSets;
        public delegate void SetProgressType(int value);
        SetProgressType progress = null;
        FlickrAuth Auth;

        // Start RemoteInfo old variables
        // Flickr f;
        Photoset[] sets;
        // public delegate void SetProgressType(int value);
        // SetProgressType progress = null;
        // End RemoteInfo old variables

        #endregion

        #region Constructors

        public FlickrLocation() 
        {
            // set the RemoteLocation to this type
            Program.RemoteLoc = this;

            // get the authentication class for this program
            Auth = (FlickrAuth) Program.RemoteAuth;

            try
            {         
                Auth.f.OnUploadProgress += Flickr_OnUploadProgress; 

                // copy photoset list across
                AllSets = Auth.f.PhotosetsGetList();

                if (AllSets == null) 
                {
                    AllSets = new PhotosetCollection();
                }

                string user = Auth.User();  //force access to check connection

                // Start RemoteInfo old code
                sets = new Photoset[AllSets.Count];

                AllSets.CopyTo(sets, 0);

                if (sets == null)
                {
                    sets = new Photoset[0];
                }
                // End RemoteInfo old code
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Error connecting to flickr", ex, ErrorType.Connect);
            }   
         
            // set the path to one specific to Flickr
            FolderSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/FlickrSync/SyncFolder.Flickr.Config.xml";

            // Load known SyncFolders
            LoadSyncFolderConfig();

            // Validate existing sets
            ValidateSets();
        } 

        #endregion

        #region Methods

        /// <summary>
        /// Reload the sets
        /// </summary>
        public void Reload()
        {
            // disable to ensure we get the latest version
            Flickr.CacheDisabled = true;

            // copy photoset list across
            PhotosetCollection CollectionOfPhotoSets = Auth.f.PhotosetsGetList();

            // Start RemoteInfo old code
            sets = new Photoset[CollectionOfPhotoSets.Count];

            CollectionOfPhotoSets.CopyTo(sets, 0);
            // End RemoteInfo old code

            // reenable cache to save on API calls
            Flickr.CacheDisabled = false;
        }  

        /// <summary>
        /// Getter Photoset - this needs to change Proof of Concept
        /// </summary>
        /// <returns>Array of Photosets</returns>
        public Photoset[] GetAllSets()
        {
            Flickr.CacheDisabled = true;

            // copy photoset list across
            PhotosetCollection CollectionOfPhotoSets = Auth.f.PhotosetsGetList();

            Flickr.CacheDisabled = false;

            Photoset[] sets = new Photoset[CollectionOfPhotoSets.Count];

            CollectionOfPhotoSets.CopyTo(sets, 0);

            if (sets == null)
                sets = new Photoset[0];

            return sets;
        }

        /// <summary>
        /// Get the photoset's Flickr thumbnail
        /// </summary>
        /// <param name="ps">a given Photoset</param>
        /// <returns>Bitmap thumbnail if it exists; otherwise, holder thumbnail.</returns>
        public Image PhotosetThumbnail(Photoset ps)
        {
            try
            {
                // FlickrNet no longer downloads photos
                WebClient client = new WebClient();

                return Bitmap.FromStream(client.OpenRead(ps.PhotosetSquareThumbnailUrl));
            }
            catch (Exception)
            {
                return Properties.Resources.Replace;
            }
        }

        /// <summary>
        /// Get specific photo's thumbnail
        /// </summary>
        /// <param name="photoid">a given Photo ID</param>
        /// <returns>Stream of a photo's thumbnail</returns>
        public Stream PhotoThumbnail(string photoid)
        {
            // FlickrNet no longer downloads photos
            WebClient client = new WebClient();

            return client.OpenRead(Auth.f.PhotosGetInfo(photoid).ThumbnailUrl);
        }

        /// <summary>
        /// Get the photos from a given set
        /// </summary>
        /// <param name="SetId">the given set</param>
        /// <returns>Array of Photos</returns>
        public Photo[] SetPhotos(string SetId)
        {
            List <Photo> PhotoList = new List<Photo>();          
            int nPage = 1;
            Photo[] zPhotoPage;

            // TODO: look into this further - it seems to work fine but I think the point
            // from observing the compiler was that FlickrSync will return only 500 photos
            // in a collection at a time - hence the need for multiple pages? except it seems to work without...

            //do {

                // copy photo list across
                PhotosetPhotoCollection CollectionOfPhotos = Auth.f.PhotosetsGetPhotos(SetId);

                zPhotoPage = new Photo[CollectionOfPhotos.Count];

                CollectionOfPhotos.CopyTo(zPhotoPage, 0);
             
                for (int i=0; i<zPhotoPage.GetLength(0); i++)
                    PhotoList.Add(zPhotoPage[i]);

             //   nPage++;
            //} while (zPhotoPage.GetLength(0)==500);

            return PhotoList.ToArray();
        }

        /// <summary>
        /// Getter specific photoset's information
        /// </summary>
        /// <param name="SetId">a given SetId</param>
        /// <returns>if found return the set; otherwise, null.</returns>
        public Photoset GetSet(string SetId)
        {
            try
            {
                return Auth.f.PhotosetsGetInfo(SetId);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get specific photoset's title
        /// </summary>
        /// <param name="SetId">a given SetId</param>
        /// <returns>String of the title if found; otherwise; null</returns>
        public string GetPhotosetTitle(string SetId)
        {
            if (SetId == "")
                return "";

            Photoset set = GetPhotoset(SetId);
            if (set == null)
                return "";
            else
                return set.Title;
        }

        /// <summary>
        /// Get specific photoset's URL
        /// </summary>
        /// <param name="SetId">a given SetId</param>
        /// <returns>String URL</returns>
        public string GetPhotosetURL(string SetId)
        {
            return Auth.f.UrlsGetUserPhotos() + "sets/" + SetId;
        }

        /// <summary>
        /// Get specific photoset's editing URL
        /// </summary>
        /// <param name="SetId">a given SetId</param>
        /// <returns>String editing URL</returns>
        public string GetPhotosetEditURL(string SetId)
        {
            return "http://www.flickr.com/photos/organize/?start_tab=one_set" + SetId;
        }

        /// <summary>
        /// Getter photoset for the sets array
        /// </summary>
        /// <param name="SetId">a given SetId</param>
        /// <returns>the photoset if it exists; otherwise, null.</returns>
        public Photoset GetPhotoset(string SetId)
        {
            foreach (Photoset ps in sets)
                if (ps.PhotosetId == SetId)
                    return ps;

            return null;
        }

        /// <summary>
        /// Get the photoset's Flickr thumbnail
        /// </summary>
        /// <param name="SetId">a given SetId</param>
        /// <returns>Image of the thumbnail</returns>
        public Image PhotosetThumbnail(string SetId)
        {
            return PhotosetThumbnail(GetPhotoset(SetId));
        }        

        /// <summary>
        /// Upload picture to Flickr
        /// </summary>
        /// <param name="filename">given filename</param>
        /// <param name="title">given title</param>
        /// <param name="description">given description</param>
        /// <param name="tags">given tags</param>
        /// <param name="permission">given permissions</param>
        /// <returns>String Id of the picture</returns>
        public string UploadPicture(string filename,string title,string description,ArrayList tags,FlickrPermissions permission)
        {

            // remove special FlickrSync control tags because we don't want them uploaded to Flickr
            ArrayList specialtags = new ArrayList();

            // get the special tags
            foreach (string tag in tags)
            {
                if (tag.ToLower().StartsWith("flickrsync:") || tag.ToLower().StartsWith(@"""flickrsync:"))
                    specialtags.Add(tag);
            }

            // remove the tags from the tags we will upload
            foreach (string stag in specialtags)
            {
                tags.Remove(stag);

                string stag2=stag.ToLower();

                // To support picasa replace colons with equal sign (to be able to use flickrsync:perm:friends instead of flickrsync:perm=friends)
                stag2=stag2.Replace(':', '=');

                // set the required permissions based on the value of the special tag
                if (stag2.Contains("flickrsync=perm=private")) permission = FlickrPermissions.PermPrivate;
                else if (stag2.Contains("flickrsync=perm=default")) permission = FlickrPermissions.PermDefault;
                else if (stag2.Contains("flickrsync=perm=familyfriends")) permission = FlickrPermissions.PermFamilyFriends;
                else if (stag2.Contains("flickrsync=perm=friends")) permission = FlickrPermissions.PermFriends;
                else if (stag2.Contains("flickrsync=perm=family")) permission = FlickrPermissions.PermFamily;
                else if (stag2.Contains("flickrsync=perm=public")) permission = FlickrPermissions.PermPublic;
            }

            // TODO: document what this does
            string tags_str = "";
            foreach (string tag in tags) 
                if (tag.Length>0 && tag[0]=='"' && tag[tag.Length-1]=='"')
                    tags_str=tags_str+tag;
                else
                    tags_str = tags_str + @"""" + tag + @""" ";

            string id = "";

            // upload the photo according to the given permissions
            if (permission==FlickrPermissions.PermDefault)
                id = Auth.f.UploadPicture(filename, title, description, tags_str);
            else 
                id=Auth.f.UploadPicture(filename,title,description,tags_str,
                    permission==FlickrPermissions.PermPublic,
                    permission==FlickrPermissions.PermFamily || permission==FlickrPermissions.PermFamilyFriends,
                    permission==FlickrPermissions.PermFriends || permission==FlickrPermissions.PermFamilyFriends);

            // remove special tags. If there is an error it will be ignored (not very relevant)
            // they were removed previously from tags_str but flickr ignores this on upload
            try
            {
                
                foreach (string stag in specialtags)
                {
                    // copy phototags list across
                    System.Collections.ObjectModel.Collection<PhotoInfoTag> CollectionOfTags = Auth.f.TagsGetListPhoto(id);

                    PhotoInfoTag[] taglist = new PhotoInfoTag[CollectionOfTags.Count];

                    CollectionOfTags.CopyTo(taglist, 0);

                    foreach (PhotoInfoTag pit in taglist)
                        if (pit.Raw == stag)
                            Auth.f.PhotosRemoveTag(pit.TagId);
                }   
            }
            catch (Exception)
            {
            }

            return id;
        }

        /// <summary>
        /// Replace an existing picture on Flickr
        /// </summary>
        /// <param name="filename">given filename</param>
        /// <param name="photoid">given photo Id</param>
        /// <param name="title">given title</param>
        /// <param name="caption">given description</param>
        /// <param name="tags">given tags</param>
        /// <param name="permission">given permissions</param>
        /// <param name="NoDeleteTags">don't delete tags on sync</param>
        /// <param name="GeoLat">latitude</param>
        /// <param name="GeoLong">longitude</param>
        /// <returns>String Id of the picture</returns>
        public string ReplacePicture(string filename,string photoid,string title,string caption,ArrayList tags,
                                        FlickrPermissions permission,bool NoDeleteTags,double? GeoLat,double? GeoLong)
        {
            string id = Auth.f.ReplacePicture(filename, photoid);
            Auth.f.PhotosSetMeta(id, title, caption);

            // as before remove special FlickrSync control tags because we don't want them uploaded to Flickr
            // TODO: make this into a seperate function?
            ArrayList specialtags = new ArrayList();
            foreach (string tag in tags)
            {
                if (tag.ToLower().StartsWith("flickrsync:") || tag.ToLower().StartsWith(@"""flickrsync:"))
                    specialtags.Add(tag);
            }

            foreach (string stag in specialtags)
            {
                tags.Remove(stag);

                string stag2 = stag.ToLower();
                // To support picasa replace colons with equal sign (to be able to use flickrsync:perm:friends instead of flickrsync:perm=friends)
                stag2=stag2.Replace(':', '=');

                if (stag2.Contains("flickrsync=perm=private")) permission = FlickrPermissions.PermPrivate;
                else if (stag2.Contains("flickrsync=perm=default")) permission = FlickrPermissions.PermDefault;
                else if (stag2.Contains("flickrsync=perm=familyfriends")) permission = FlickrPermissions.PermFamilyFriends;
                else if (stag2.Contains("flickrsync=perm=friends")) permission = FlickrPermissions.PermFriends;
                else if (stag2.Contains("flickrsync=perm=family")) permission = FlickrPermissions.PermFamily;
                else if (stag2.Contains("flickrsync=perm=public")) permission = FlickrPermissions.PermPublic;
            }

            Auth.f.PhotosSetPerms(id,
                permission == FlickrPermissions.PermPublic,
                permission == FlickrPermissions.PermFriends || permission == FlickrPermissions.PermFamilyFriends,
                permission == FlickrPermissions.PermFamily || permission == FlickrPermissions.PermFamilyFriends,
                PermissionComment.Everybody, PermissionAddMeta.Everybody);

            string user = Auth.f.AuthCheckToken(Properties.Settings.Default.FlickrToken).User.UserId;

            // get and copy the tags to the PhotoInfoTag array            
            System.Collections.ObjectModel.Collection<PhotoInfoTag> TagList = Auth.f.TagsGetListPhoto(id); //.Tags.TagCollection;

            PhotoInfoTag[] ftags = new PhotoInfoTag[TagList.Count];

            TagList.CopyTo(ftags, 0);

            if (ftags != null)
            {
                foreach (PhotoInfoTag ftag in ftags)
                {
                    if (ftag.AuthorId == user)
                    {
                        bool found = false;
                        for (int i = 0; i < tags.Count; i++)
                        {
                            if ((string)tags[i] == ftag.Raw)
                            {
                                found = true;
                                tags.RemoveAt(i);
                                break;
                            }
                        }

                        if (!found && !NoDeleteTags)
                            Auth.f.PhotosRemoveTag(ftag.TagId);
                    }
                }
            }

            foreach (string tag in tags)
            {
                if (tag.Length > 0 && tag[0] == '"' && tag[tag.Length - 1] == '"')
                    Auth.f.PhotosAddTags(id, tag);
                else
                    Auth.f.PhotosAddTags(id, @"""" + tag + @"""");
            }

            if (GeoLat != null && GeoLong != null)
            {
                try
                {
                    Auth.f.PhotosGeoSetLocation(id, (double)GeoLat, (double)GeoLong);
                }
                catch (Exception ex)
                {
                    FlickrSync.Error("Error setting Geo location: " + filename + " Lat=" + GeoLat + " Long=" + GeoLong,  ex, ErrorType.Normal);
                }
            }

            return id;
        }

        /// <summary>
        /// Delete picture from Flickr
        /// </summary>
        /// <param name="photoid">given photo Id</param>
        public void DeletePicture(string photoid)
        {
            Auth.f.PhotosDelete(photoid);
        }

        /// <summary>
        /// Add a new photo to Flickr
        /// </summary>
        /// <param name="setid">the set Id where the new photo will be placed</param>
        /// <param name="photoid">given photo Id</param>
        public void PhotosetsAddPhoto(string setid,string photoid)
        {
            Auth.f.PhotosetsAddPhoto(setid, photoid);
        }

        /// <summary>
        /// Create a new set
        /// </summary>
        /// <param name="title">new set title</param>
        /// <param name="description">new set description</param>
        /// <param name="photoid">new set thumbnail image</param>
        /// <returns>the new PhotoSet</returns>
        public Photoset CreateSet(string title, string description, string photoid)
        {
            return Auth.f.PhotosetsCreate(title, description, photoid);
        }

        /// <summary>
        /// Set ProgressBar
        /// </summary>
        /// <param name="p"></param>
        public void SetProgress(SetProgressType p)
        {
            progress = p;
        }

        /// <summary>
        /// Clear ProgressBar
        /// </summary>
        public void ClearProgress()
        {
            progress = null;
        }

        /// <summary>
        /// Find out the filesize upload limit for Flickr
        /// </summary>
        /// <returns>Long the filesize</returns>
        public long MaxFileSize()
        {
            try
            {
                return Auth.f.PeopleGetUploadStatus().FileSizeMax;
            }
            catch (Exception)
            {
                return long.MaxValue;
            }
        }

        /// <summary>
        /// Sort the set by Photo Id
        /// </summary>
        /// <param name="setid"></param>
        /// <param name="ids"></param>
        public void SortSet(string setid,string[] ids)
        {
            Auth.f.PhotosetsEditPhotos(setid, Auth.f.PhotosetsGetInfo(setid).PrimaryPhotoId, ids);
        }

        /// <summary>
        /// Check sets still exist
        /// </summary>
        public void ValidateSets()
        {
            ArrayList ToRemove = new ArrayList();

            // check if each local folder still has a corresponding set on Flickr
            foreach (SyncFolder sf in Program.LocalLoc.GetSyncFolders())
            {
                bool found = false;
                foreach (Photoset psi in GetAllSets())
                {
                    if (sf.SetId == psi.PhotosetId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found && sf.SetId != "")
                {
                    if (Properties.Settings.Default.MessageLevel != MessageLevel.MessagesNone && UserMessage.DisplayMessage("Folder " + sf.FolderPath + " is configured to synchronize with a Set that does not exist (ID " +
                        sf.SetId + "). Delete from configuration?", "Warning") == true)
                    {
                        ToRemove.Add(sf.FolderPath);
                        Program.logger.Info(sf.FolderPath + " deleted from configuration");
                    }
                    else
                        Program.logger.Info(sf.FolderPath + " configured to synchronize with a set that does not exists");
                }
            }

            foreach (string path in ToRemove)
                Program.LocalLoc.Remove(path);

            if (ToRemove.Count > 0)
                Program.LocalLoc.SaveSyncFolderConfig();
        }

        #endregion

        #region HelperMethods

  

        #endregion

        #region EventListeners

        /// <summary>
        /// Upload progress event listener
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Flickr_OnUploadProgress(object sender, UploadProgressEventArgs e)
        {
            if (progress != null)
                progress(Int32.Parse(e.Bytes.ToString()));
        }

        #endregion
    }
}