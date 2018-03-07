using System;
using System.IO;

public class SearchReplace
{
    public string oldpath { get; set; }
    public string newpath { get; set; }
    public int use_count;

    // Given 2 strings, extract non-common prefixes to initialize class
    public SearchReplace(string old_folder, string new_folder)
    {
        string[] oldpaths = old_folder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        string[] newpaths = new_folder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        int oldindex = oldpaths.Length - 1;
        int newindex = newpaths.Length - 1;
        // Loop while we still have something to work with on both old and new
        while (oldindex >= 1 && newindex >= 1)
        {
            if (oldpaths[oldindex] == newpaths[newindex])
            {
                // If this part is common, it is not part of the search/replace pattern
                oldindex--;
                newindex--;
                continue;
            }
            else
            {
                break;
            }
        }
        Array.Resize<string>(ref oldpaths, oldindex + 1);
        Array.Resize<string>(ref newpaths, newindex + 1);
        oldpath = String.Join(Path.DirectorySeparatorChar.ToString(), oldpaths);
        newpath = String.Join(Path.DirectorySeparatorChar.ToString(), newpaths);
        use_count = 0;
    }
}