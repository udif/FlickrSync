using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using FlickrNet;
using System.IO;

namespace FlickrSync
{
    /// <summary>
    /// The Flickr authentication class
    /// </summary>
    class FlickrAuth : Authentication
    {
        #region Properties

        public Flickr f;
        private string Token="";        

        #endregion

        #region Constructors

        public FlickrAuth() {

            // Assign this class as the authentication class being used
            Program.RemoteAuth = this;
       
            // load settings here?        
        }

        #endregion

        #region Methods

        override public void Login() {
        
            // Legacy code, left just in case removing it breaks something        
            // probably not needed but just to make sure no message from previous versions is shown
            if (Properties.Settings.Default.MessageId.CompareTo("090130_0000")<0)
            {
                Properties.Settings.Default.MessageId = "090130_0000";
                Properties.Settings.Default.Save();
            }
            
            // check if there already is an authentication token, e.g. a set user account   
            try {
                Token=Properties.Settings.Default.FlickrToken;
            }
            catch(Exception ex)
            {
                Program.logger.Debug("No saved user authentication token");
            }

            // if there isn't one then we need to ask the user to authenticate
            if (Token.Equals(""))
            {
                Program.logger.Debug("No existing user token so attempting to get a new one");

                FlickrGetToken();
            }
            else
            {
                // create a new instance of Flickr passing the required login credentials
                f = new Flickr(Properties.Settings.Default.FlickrApiKey, Properties.Settings.Default.FlickrShared, Properties.Settings.Default.FlickrToken);              
            }

            // the below stuff should be in the class which is instantiating the program not the login class e.g. the FlickrSync load method
            // - will say that this class also instantiates the program

            // can call general methods from the classes in Program and then if need specific Flickr functionality then cast the class to call it (FlickrAuth) ClassInstance.eg()

            // Create the LocalLocation first so stuff can be compared against...
            new LocalLocation();

            // Then create the RemoteLocation
            new FlickrLocation();
             
            // Load the information into the main window
            // Ideally would not call an existing UI class from here, but this was the quickest way of doing it
            Program.MainWindow.Reload();
          
            Program.logger.Warn("Application loaded");
        }

        /// <summary>
        /// Get authentication token from Flickr
        /// </summary>
        /// <returns>string of the token if can get one; otherwise, an empty string</returns>
        private string FlickrGetToken()
        {
            try
            {
                // create a new instance of Flickr passing the API key and shared secret
                f = new Flickr(Properties.Settings.Default.FlickrApiKey, Properties.Settings.Default.FlickrShared);

                // set up a web proxy (in case it is required)
                f.Proxy = GetProxy(true);

                // Frob to identify the login session (http://www.flickr.com/services/api/auth.howto.desktop.html)
                // Create a Url with the permissions we require
                string Frob = f.AuthGetFrob();
                string Url = f.AuthCalcUrl(Frob, AuthLevel.Read | AuthLevel.Write | AuthLevel.Delete);
                
                // launch the webbrowser to the generated Url for the user to authenticate the application with their account
                System.Diagnostics.Process.Start(Url);

                // ask the user to confirm they have done this so we can proceed
                // TODO: MessageBox method in FlickrSync we can call to display a message box... not the cleanest way of doing this so mention that in writeup
                if (Properties.Settings.Default.MessageLevel == MessageLevel.MessagesNone || UserMessage.DisplayMessage("Please authorize FlickrSync to access your Flickr account in the automatically opened browser window, then click OK to confirm this.", "Confirmation") == true)
                {
                    // get and set the authentication token
                    Auth Auth = f.AuthGetToken(Frob);

                    Properties.Settings.Default.FlickrToken = Auth.Token;

                    SaveApplicationConfig(); // save the token 
                    
                    return Properties.Settings.Default.FlickrToken;
                }
            }
            catch (Exception Ex)
            {
                // error message if can't get the token
                if (Properties.Settings.Default.FlickrToken == "") 
                {
                    FlickrSync.Error("Unable to obtain Flickr Token", Ex, ErrorType.Connect);
                }                    
                else
                {
                    FlickrSync.Error("Error obtaining Flickr Token", Ex, ErrorType.Normal);
                }    
            }    
            
            return null;       
        }  

        #endregion

        #region HelperMethods

        /// <summary>
        /// Save the application config file
        /// </summary>
        public void SaveApplicationConfig()
        {
            try
            {
                Properties.Settings.Default.Save();
            }
            catch (Exception Ex)
            {
                FlickrSync.Error("Error saving configuration", Ex, ErrorType.Normal);
            }
        }

        /// <summary>
        /// Authenticate user name
        /// </summary>
        /// <returns>String check token</returns>
        public string User()
        {
            return f.AuthCheckToken(Properties.Settings.Default.FlickrToken).User.UserName;
        }

        /// <summary>
        /// Authenticate user ID
        /// </summary>
        /// <returns>String check token</returns>
        public string UserId()
        {
            return f.AuthCheckToken(Properties.Settings.Default.FlickrToken).User.UserId;
        }

        /// <summary>
        /// Try to get the Flickr user name
        /// </summary>
        public void UpdateUser()
        {
            try
            {
                // user = ((FlickrAuth)Program.RemoteAuth).User();
                // Text = "FlickrSync (" + user + ")";
            }
            catch (Exception ex)
            {
                FlickrSync.Error("Error obtaining user name", ex, ErrorType.Normal);
            }
        }

        #endregion
    }        
}