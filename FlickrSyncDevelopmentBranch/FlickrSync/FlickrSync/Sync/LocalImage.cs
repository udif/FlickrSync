using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace FlickrSync
{
    /// <summary>
    /// This class stores information and methods for local images on a user's computer
    /// </summary>
    class LocalImage : SyncResource
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
        public LocalImage()
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

            Program.logger.Debug("Default LocalImage created.");
        }

        /// <summary>
        /// Constructor from FileName
        /// </summary>
        /// <param name="FileName">given image's file name</param>
        public LocalImage(string FileName)
        {

            this.FileName = FileName;

            // TODO: check whether need to use @ before input path here and possibly for saving methods

            // determine the filetype and therefore they type of image to create
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

            Program.logger.Debug("FileName LocalImage created.");
        }

        #endregion 

        #region Methods

        // Thumbnail code based on: http://danbystrom.se/2009/01/05/imagegetthumbnailimage-and-beyond/
        // Could have made the resizing code better and higher quality but not necessary for just checking images

        /// <summary>
        /// Create a thumbnail
        /// </summary>
        /// <param name="width">width of the generated thumbnail</param>
        /// <returns>Bitmap thumnail</returns>
        public override Bitmap GetThumbnail(int width)
        {
            // if the file is a Jpeg we can probably extract the thumbnail from the EXIF property
            if (FileType == FileTypes.Jpeg)
            {
                return GetJpgThumbnail(width);
            }

            // if it isn't a Jpeg then get the thumbnail the slower way
            return GenerateThumbnail(width);
        }

        /// <summary>
        /// Get the Exif thumnail if available otherwise get a normal one
        /// </summary>
        /// <param name="thumbWidth">width of the generated thumbnail</param>
        /// <returns>Bitmap thumnail</returns>
        private Bitmap GetJpgThumbnail(int thumbWidth)
        {
            try
            {
                // open the file
                using (FileStream fs = new FileStream(FileName, FileMode.Open))
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
                return GenerateThumbnail(thumbWidth);
            }
            catch (Exception ex)
            {
                Program.logger.ErrorException("Thumbnail file error ", ex);
            }

            // in case of error
            return new Bitmap(0, 0);           
        }

        /// <summary>
        /// Generate a thumbnail by opening the image and creating one
        /// </summary>
        /// <param name="thumbWidth">width of the generated thumbnail</param>
        /// <returns>Bitmap thumnail</returns>
        private Bitmap GenerateThumbnail(int thumbWidth)
        {
            try
            {
                // use filestream to open the image (but not lock it open) then get the thumbnail
                using (FileStream fs = new FileStream(FileName, FileMode.Open))
                {
                    using (Image img = Image.FromStream(fs, true, false))
                    {
                        // scaling
                        if (img.Height > img.Width)
                        {
                            return (Bitmap)img.GetThumbnailImage(thumbWidth * img.Width / img.Height, thumbWidth, null, IntPtr.Zero);
                        }

                        return (Bitmap)img.GetThumbnailImage(thumbWidth, thumbWidth * img.Height / img.Width, null, IntPtr.Zero);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.logger.ErrorException("Thumbnail file error ", ex);
            }

            // in case of error
            return new Bitmap(0, 0);  
        }

        // TODO: The following methods are adapted from the old ImageInfo class and may need to change

        /// <summary>
        /// Add basic image metadata from file
        /// </summary>
        /// <param name="bitmapMetadata">From the image whose metadata is being added to</param>
        private void AddMetadata(BitmapMetadata bitmapMetadata)
        {
            Title = ImageMetadata.Title;
            Description = ImageMetadata.Comment;

            if (ImageMetadata.DateTaken != null)
            {
                DateTaken = DateTime.Parse(ImageMetadata.DateTaken);
            }

            if (ImageMetadata.Keywords != null)
            {
                Tags = new ArrayList();

                foreach (string tag in ImageMetadata.Keywords)
                {
                    Tags.Add(tag);
                }
            }
        }

        /// <summary>
        /// GPS Coordinates
        /// </summary>
        /// <param name="coordinates">TODO: find out how this works</param>
        /// <returns></returns>
        private static double ConvertCoordinate(ulong[] coordinates)
        {
            int degrees;
            int minutes;
            double seconds;

            degrees = (int)splitLongAndDivide(coordinates[0]);
            minutes = (int)splitLongAndDivide(coordinates[1]);
            seconds = splitLongAndDivide(coordinates[2]);

            double coordinate = degrees + ((double)minutes / 60.0) + (seconds / 3600);

            double roundedCoordinate = Math.Floor((double)(coordinate * 1000000)) / 1000000;

            return roundedCoordinate;
        }

        /// <summary>
        /// Get GPS coordinates from stored value
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static double splitLongAndDivide(ulong number)
        {
            byte[] bytes = BitConverter.GetBytes(number);
            int int1 = BitConverter.ToInt32(bytes, 0);
            int int2 = BitConverter.ToInt32(bytes, 4);
            return ((double)int1 / (double)int2);
        }

        /// <summary>
        /// Populates properties with rich metadata about the image
        /// </summary>
        protected override void CreateJpeg()
        {
            try
            {  
                // open the image, decode as Jpeg and get metadata
                PictureStream = new FileStream(FileName, FileMode.Open, FileAccess.Read);

                JpegBitmapDecoder JpegDecoder = new JpegBitmapDecoder(PictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);

                ImageMetadata = (BitmapMetadata)JpegDecoder.Frames[0].Metadata;

                // assign the field values
                AddMetadata(ImageMetadata);               

                //City = (string)bitmapMetadata.GetQuery(@"/xmp/<xmpbag>photoshop:City");
                City = (string)ImageMetadata.GetQuery(@"/app13/irb/8bimiptc/iptc/city");
                if (City == null)
                    City = "";

                Sublocation = (string)ImageMetadata.GetQuery(@"/app13/irb/8bimiptc/iptc/sub-location");
                if (Sublocation == null)
                    Sublocation = "";

                //Country = (string)bitmapMetadata.GetQuery(@"/xmp/<xmpbag>photoshop:Country");
                Country = (string)ImageMetadata.GetQuery(@"/app13/irb/8bimiptc/iptc/country\/primary location name");
                if (Country == null)
                    Country = "";

                byte[] Version = (byte[])ImageMetadata.GetQuery(@"/app1/ifd/gps/");
                if (Version != null)
                {
                    ulong[] GeoLatInfo = (ulong[])ImageMetadata.GetQuery(@"/app1/ifd/gps/subifd:{ulong=2}");
                    string GeoLatDirection = (string)ImageMetadata.GetQuery(@"/app1/ifd/gps/subifd:{char=1}");
                    ulong[] GeoLongInfo = (ulong[])ImageMetadata.GetQuery(@"/app1/ifd/gps/subifd:{ulong=4}");
                    string GeoLongDirection = (string)ImageMetadata.GetQuery(@"/app1/ifd/gps/subifd:{char=3}");

                    if (GeoLatInfo != null && GeoLatDirection != null)
                    {
                        GeoLat = ConvertCoordinate(GeoLatInfo);
                        if (GeoLatDirection == "S")
                            GeoLat = -GeoLat;
                    }

                    if (GeoLongInfo != null && GeoLongDirection != null)
                    {
                        GeoLong = ConvertCoordinate(GeoLongInfo);
                        if (GeoLongDirection == "W")
                            GeoLong = -GeoLong;
                    }
                }

                StarRating = ImageMetadata.Rating;
            }
            catch (Exception ex)
            {
                Program.logger.ErrorException("Jpeg LocalImage file reading error ", ex);
            }
            finally
            {
                // close the file stream
                PictureStream.Close();
            }
        }

        /// <summary>
        /// Populates properties with basic metadata about the image
        /// </summary>
        protected override void CreateImage()
        {
            try
            {  
                switch(FileType) 
                {
                    case FileTypes.Tiff:
                        TiffBitmapDecoder TiffDecoder = new TiffBitmapDecoder(PictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        ImageMetadata = (BitmapMetadata)TiffDecoder.Frames[0].Metadata;
                        // Get available metadata
                        AddMetadata(ImageMetadata);
                        break;
                    case FileTypes.Gif:
                        GifBitmapDecoder GifDecoder = new GifBitmapDecoder(PictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        ImageMetadata = (BitmapMetadata)GifDecoder.Frames[0].Metadata;
                        // Get available metadata
                        AddMetadata(ImageMetadata);
                        break;

                    case FileTypes.Png:
                        PngBitmapDecoder PngDecoder = new PngBitmapDecoder(PictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        ImageMetadata = (BitmapMetadata)PngDecoder.Frames[0].Metadata;
                        // Get available metadata
                        AddMetadata(ImageMetadata);
                        break;
                    case FileTypes.Bmp:
                        BmpBitmapDecoder BmpDecoder = new BmpBitmapDecoder(PictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        ImageMetadata = (BitmapMetadata)BmpDecoder.Frames[0].Metadata;
                        // Get available metadata
                        AddMetadata(ImageMetadata);
                        break;
                }
              
            }
            catch (Exception ex)
            {
                Program.logger.ErrorException("Non-Jpeg LocalImage file reading error ", ex);
            }
            finally
            {
                // close the file stream
                PictureStream.Close();
            }            
        }

        #endregion
    }
}