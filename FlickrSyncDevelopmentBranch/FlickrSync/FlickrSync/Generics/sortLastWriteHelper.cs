using System.Collections;
using System.IO;

/// <summary>
/// Compare the write times of two image / file objects 
/// </summary>
/// I believe there is a bug about this http://flickrsync.codeplex.com/workitem/8187
public class sortLastWriteHelper : IComparer
{
    int IComparer.Compare(object x, object y)
    {
        FileInfo f1 = (FileInfo)x;
        FileInfo f2 = (FileInfo)y;

        if (f1.LastWriteTime > f2.LastWriteTime)
        {
            return 1;
        }
        else if (f1.LastWriteTime < f2.LastWriteTime)
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }
}
    

