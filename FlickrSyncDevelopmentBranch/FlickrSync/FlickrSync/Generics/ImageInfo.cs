using System;
using System.Collections;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Forms;

namespace FlickrSync
{
    /// <summary>
    /// ImageInfo provides Metadata information for image files
    /// Currently it is only able to get meta information from Jpeg files
    /// </summary>
    class ImageInfo
    {
        // set of possible file types
        public enum FileTypes {FileTypeUnknown=0, FileTypeJpeg, FileTypeGIF, FileTypePNG, FileTypeTiff, FileTypeBmp};

        ArrayList Tags;
        public string Title { get; private set; } // auto implemented properties (in this case read only)
        public string Description { get; private set; }
        public string City { get; private set; }
        public string Sublocation { get; private set; }
        public string Country { get; private set; }
        public DateTime DateTaken { get; private set; }
        public string FileName { get; private set; }
        double? GeoLat; // not sure about these two yet...
        double? GeoLong;
        public int StarRating { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ImageInfo()
        {
            Tags = new ArrayList();
            Title = "";
            Description = "";
            City = "";
            Sublocation = "";
            Country = "";
            DateTaken = new DateTime(2000, 1, 1);
            FileName = "";
            GeoLat = GeoLong = null;
            StarRating = 0;

            Program.logger.Debug("New ImageInfo!"); //TODO: logging
        }

        /// <summary>
        /// Load / populate file object with provided data?
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="type"></param>
        public void Load(string filename, FileTypes type)
        {
            /* is this really necessary given the above? - doesn't seem to be (candidate for deletion?) */
            Tags.Clear();
            Title = "";
            Description = "";
            City = "";
            Sublocation = "";
            Country = "";
            DateTaken = new DateTime(2000, 1, 1);
            FileName = filename;
            GeoLat = GeoLong = null;
            StarRating = 0;

            /* maybe there is something nicer to get a filetype - need to consider whether we should be supporting all these file types 
             * Flickr officially supports JPEGs, non-animated GIFs, and PNGs. You can also upload TIFFs and some other file types, but they will automatically be converted to and stored in JPEG format. http://www.flickr.com/help/photos/ so only support these + work out what happens with TIFFs since they get auto converted...
             * 
             * Use this: http://msdn.microsoft.com/en-us/library/system.io.path.getextension.aspx
             * 
             */
            if (type == FileTypes.FileTypeUnknown)
            {
                if (filename.EndsWith(".jpg", System.StringComparison.CurrentCultureIgnoreCase))
                    type = FileTypes.FileTypeJpeg;
                else if (filename.EndsWith(".jpeg", System.StringComparison.CurrentCultureIgnoreCase))
                    type = FileTypes.FileTypeJpeg;
                else if (filename.EndsWith(".gif", System.StringComparison.CurrentCultureIgnoreCase))
                    type = FileTypes.FileTypeGIF;
                else if (filename.EndsWith(".png", System.StringComparison.CurrentCultureIgnoreCase))
                    type = FileTypes.FileTypePNG;
                else if (filename.EndsWith(".tif", System.StringComparison.CurrentCultureIgnoreCase))
                    type = FileTypes.FileTypeTiff;
                else if (filename.EndsWith(".tiff", System.StringComparison.CurrentCultureIgnoreCase))
                    type = FileTypes.FileTypeTiff;
                else if (filename.EndsWith(".bmp", System.StringComparison.CurrentCultureIgnoreCase))
                    type = FileTypes.FileTypeBmp;
            }

            // opening the file and reading the metadata
            try
            {
                Stream pictureStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                BitmapMetadata bitmapMetadata; // replace with System.Drawing for Mono?

                switch (type)
                {
                    case FileTypes.FileTypeJpeg:


                        // WFP METHOD HERE...
                        JpegBitmapDecoder JpegDecoder = new JpegBitmapDecoder(pictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        bitmapMetadata = (BitmapMetadata)JpegDecoder.Frames[0].Metadata;

                        if (bitmapMetadata.Keywords != null)
                        {
                            foreach (string tag in bitmapMetadata.Keywords)
                                Tags.Add(tag);
                        }

                        Title = bitmapMetadata.Title;
                        Description = bitmapMetadata.Comment;
                        if (bitmapMetadata.DateTaken != null)
                            DateTaken = DateTime.Parse(bitmapMetadata.DateTaken);

                        //City = (string)bitmapMetadata.GetQuery(@"/xmp/<xmpbag>photoshop:City");
                        City = (string)bitmapMetadata.GetQuery(@"/app13/irb/8bimiptc/iptc/city");
                        if (City == null)
                            City = "";

                        Sublocation = (string)bitmapMetadata.GetQuery(@"/app13/irb/8bimiptc/iptc/sub-location");
                        if (Sublocation == null)
                            Sublocation = "";

                        //Country = (string)bitmapMetadata.GetQuery(@"/xmp/<xmpbag>photoshop:Country");
                        Country = (string)bitmapMetadata.GetQuery(@"/app13/irb/8bimiptc/iptc/country\/primary location name");
                        if (Country == null)
                            Country = "";

                        byte[] Version = (byte[])bitmapMetadata.GetQuery(@"/app1/ifd/gps/");
                        if (Version != null)
                        {
                            ulong[] GeoLatInfo = (ulong[])bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{ulong=2}");
                            string GeoLatDirection = (string)bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{char=1}");
                            ulong[] GeoLongInfo = (ulong[])bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{ulong=4}");
                            string GeoLongDirection = (string)bitmapMetadata.GetQuery(@"/app1/ifd/gps/subifd:{char=3}");

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

                        StarRating = bitmapMetadata.Rating;

                        break;

                    case FileTypes.FileTypeTiff:
                        TiffBitmapDecoder TiffDecoder = new TiffBitmapDecoder(pictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        bitmapMetadata = (BitmapMetadata)TiffDecoder.Frames[0].Metadata;

                        AddMetadata(bitmapMetadata);

                        break;

                    case FileTypes.FileTypeGIF:
                        GifBitmapDecoder GifDecoder = new GifBitmapDecoder(pictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        bitmapMetadata = (BitmapMetadata)GifDecoder.Frames[0].Metadata;

                        AddMetadata(bitmapMetadata);

                        break;

                    case FileTypes.FileTypePNG:
                        PngBitmapDecoder PngDecoder = new PngBitmapDecoder(pictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        bitmapMetadata = (BitmapMetadata)PngDecoder.Frames[0].Metadata;

                        AddMetadata(bitmapMetadata);

                        break;

                    case FileTypes.FileTypeBmp:
                        BmpBitmapDecoder BmpDecoder = new BmpBitmapDecoder(pictureStream, BitmapCreateOptions.None, BitmapCacheOption.None);
                        bitmapMetadata = (BitmapMetadata)BmpDecoder.Frames[0].Metadata;

                        AddMetadata(bitmapMetadata);

                        break;
                }

                pictureStream.Close();
            }
            catch (Exception)
            {
                // maybe we should do something here?
            }

            // should this be fixed here or when we are getting the relevant metadata?
            if (Title == null) Title = "";
            if (Description == null) Description = "";
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
        /// Get array of tags
        /// </summary>
        /// <returns>ArrayList of tags</returns>
        public ArrayList GetTagsArray()
        {
            return (ArrayList)Tags.Clone();
        }

        /// <summary>
        /// Get string of tags
        /// </summary>
        /// <returns>String of tag names</returns>
        public string GetTagsString()
        {
            string taglist = "";

            foreach (string tag in Tags)
            {
                if (taglist == "")
                    taglist = tag;
                else
                    taglist+= "," + tag;
            }

            return taglist;
        }

        /// <summary>
        /// Get the geographical coordinates
        /// </summary>
        /// <param name="lat"></param>
        /// <returns></returns>
        public double? GetGeo(bool lat)
        {
            if (lat) return GeoLat;
            else return GeoLong;
        }
           
        /// <summary>
        /// Add metadata - this may well change as things progress but it's a quick tidying up for now
        /// </summary>
        /// <param name="bitmapMetadata">From the image whose metadata is being added to</param>
        private void AddMetadata(BitmapMetadata bitmapMetadata)
        {
            if (bitmapMetadata.Keywords != null)
            {
                foreach (string tag in bitmapMetadata.Keywords)
                    Tags.Add(tag);
            }

            Title = bitmapMetadata.Title;
            Description = bitmapMetadata.Comment;
            if (bitmapMetadata.DateTaken != null)
                DateTaken = DateTime.Parse(bitmapMetadata.DateTaken);

            StarRating = bitmapMetadata.Rating;
        }

    }
}
