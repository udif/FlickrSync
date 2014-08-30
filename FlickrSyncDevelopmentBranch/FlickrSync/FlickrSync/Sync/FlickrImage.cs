using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using FlickrNet;

namespace FlickrSync
{
    /// <summary>
    /// This class stores information and methods for images stored on Flickr
    /// </summary>
    class FlickrImage : SyncResource
    {
        #region Properties

        // Required properties of a SyncResource
        public override string FileName { get; protected set; }
        public override string Title { get; protected set; }
        public override DateTime DateTaken { get; protected set; }
        public override DateTime DateModified { get; protected set; }
        public override FileTypes FileType { get; protected set; }
        public override Stream PictureStream { get; protected set; }
        public override BitmapMetadata ImageMetadata { get; protected set; }
        public override Bitmap Thumbnail { get; protected set; } // probably don't need to create a bitmap unless its needed

        public override ArrayList Tags { get; protected set; }
        public override string Description { get; protected set; }
        public override string City { get; protected set; }
        public override string Sublocation { get; protected set; }
        public override string Country { get; protected set; }
        public override double? GeoLat { get; protected set; }
        public override double? GeoLong { get; protected set; }
        public override int StarRating { get; protected set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor 
        /// This constructor should not be used but is specified as a default case
        /// </summary>
        public FlickrImage()
        {
            FileName = "";
            Title = "";            
            DateTaken = new DateTime(2000, 1, 1);
            DateModified = new DateTime(2000, 1, 1);
            FileType = FileTypes.Unknown;
            PictureStream = null;
            ImageMetadata = null;
            Thumbnail = null;
            Tags = new ArrayList();
            Description = "";
            City = "";
            Sublocation = "";
            Country = "";
            GeoLat = null;
            GeoLong = null;
            StarRating = 0;

            Program.logger.Debug("Default FlickrImage created.");
        }

        /// <summary>
        /// Constructor from FileName
        /// </summary>
        /// <param name="FileName">given image's file name</param>
        public FlickrImage(string FileName)
        {

            this.FileName = FileName;

            // TODO: check whether need to use @ before input path here and possibly for saving methods

            /* determine the filetype and therefore they type of image to create
            switch (Path.GetExtension(FileName).ToLower())
            {
                case ".jpg":
                    FileType = FileTypes.Jpeg;
                    CreateJpeg();
                    break;
                case ".jpeg":
                    FileType = FileTypes.Jpeg;
                    CreateJpeg();
                    break;
                case ".tif":
                    FileType = FileTypes.Tiff;
                    CreateImage();
                    break;
                case ".tiff":
                    FileType = FileTypes.Tiff;
                    CreateImage();
                    break;
                case ".bmp":
                    FileType = FileTypes.Bmp;
                    CreateImage();
                    break;
                case ".gif":
                    FileType = FileTypes.Gif;
                    CreateImage();
                    break;
                case ".png":
                    FileType = FileTypes.Png;
                    CreateImage();
                    break;
                default:
                    FileType = FileTypes.Unknown;
                    break;                   
            }
             */

            Program.logger.Debug("FileName FLickrImage created.");
        }

        // so for the constructor here we want something that will take the result of the of a Flickr Query and use it to populate the data...

        /// <summary>
        /// Create a FlickrImage using details from a Standard Photo Response
        /// </summary>
        /// <param name="imageDetails">set of data about a single image from the Standard Photo Response</param>
        public FlickrImage(Collection imageDetails)
        {
            // TODO: assign values here
        }

        #endregion 

        #region Methods

        /// <summary>
        /// Retrieve thumbnail from Flickr
        /// </summary>
        /// <param name="width">width of the generated thumbnail</param>
        /// <returns>Bitmap thumnail</returns>
        public override Bitmap GetThumbnail(int width)
        {
            // 75 or 100 px from Flickr

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

            /* FlickrNet no longer downloads photos
            WebClient client = new WebClient();

            client.OpenRead(f.PhotosGetInfo(photoid).ThumbnailUrl);

            Bitmap.FromStream(FlickrSync.ri.PhotoThumbnail(FileName));
            */

            return null; // placeholder
        }

        /// <summary>
        /// Populates properties with rich metadata about the image
        /// </summary>
        protected override void CreateJpeg()
        {
            
        }

        /// <summary>
        /// Populates properties with basic metadata about the image
        /// </summary>
        protected override void CreateImage()
        {
            
        }

        #endregion
    }
}
