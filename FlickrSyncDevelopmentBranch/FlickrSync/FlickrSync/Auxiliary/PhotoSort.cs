using System;
using System.Collections;
using System.Text;
using FlickrNet;

namespace FlickrSync
{

    /// <summary>
    /// Class to allow us to compare photo objects to check for changes
    /// we can probably change this to use machine tags with the upgrade of 
    /// FlickrNet
    /// </summary>
    public class PhotoSortDateTaken : IComparer
    {
        // compare the date taken between photos
        int IComparer.Compare(object x, object y)
        {
            Photo p1 = (Photo)x;
            Photo p2 = (Photo)y;

            if (p1.DateTaken > p2.DateTaken)
                return 1;
            else if (p1.DateTaken < p2.DateTaken)
                return -1;
            else
                return 0;
        }
    }

    /// <summary>
    /// Class to compare the photo titles
    /// </summary>
    public class PhotoSortTitle : IComparer
    {
        // compare the titles between photos
        int IComparer.Compare(object x, object y)
        {
            Photo p1 = (Photo)x;
            Photo p2 = (Photo)y;

            return String.Compare(p1.Title, p2.Title, StringComparison.CurrentCulture);
        }
    }


    /// <summary>
    /// Class to compare the tags     
    /// </summary>
    /// TODO: Tag comparison section
    public class PhotoSortTag : IComparer
    {

        // refactored in a function not entirely sure what this does but I think
        // it's something to do with custom Flickrsync tags so we can 
        // probably change this to use machine ones when we upgrade FlickrNet
        void GetOrder(Photo p, int order)
        {
            // create variables to store the tags from the collection
            String pct = "";
            string[] TagsArray = new string[p.Tags.Count];

            // copy the tags to this array
            p.Tags.CopyTo(TagsArray, 0);

            // go through the array appending the string with the current array element
            foreach (string s in TagsArray)
            {
                pct += " " + s.ToLower();
            }

            pct = pct.Replace("flickrsync:order:", "flickrsync:order=");

            // FlickrNet is not yet supporting MachineTags fields so we use CleanTags
            int pos = pct.IndexOf("flickrsync:order=");
            if (pos >= 0)
            {
                string str = pct.Substring(pos + 17);
                str.Trim();

                pos = str.IndexOf(' ');

                if (pos > 0)
                    str = str.Remove(pos);
                str.Trim();

                order = Convert.ToInt32(str);
            }
            
        }

        // compare the tags
        int IComparer.Compare(object x, object y)
        {
            Photo p1 = (Photo)x;
            Photo p2 = (Photo)y;
            int order1 = Int32.MaxValue, order2 = Int32.MaxValue; // why are we using the maximum values here?

            // get order methods
            GetOrder(p1, order1);
            GetOrder(p2, order2);

            if (order1 < order2)
                return -1;
            else if (order1 > order2)
                return 1;
            else
                return 0;
        }
    }
}
