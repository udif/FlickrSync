using System;
using System.Windows.Forms;

namespace FlickrSync 
{
    /// <summary>
    /// Display a MessageBox 
    /// (The point of this class is to seperate the View and Control from the Model, see MVC)
    /// </summary>
    public class UserMessage {

        public UserMessage()
        {
        }

        /// <summary>
        /// Display a MessageBox and return the DialogResult
        /// </summary>
        /// <param name="Message">string message 1</param>
        /// <param name="Title">string message 2</param>
        /// <returns></returns>
        public static bool DisplayMessage(string Message, string Title)
        {      
            DialogResult Result = MessageBox.Show(Message, Title, MessageBoxButtons.OKCancel);

            if (Result == DialogResult.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
