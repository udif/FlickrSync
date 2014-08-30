using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace FlickrSync
{
    /// <summary>
    /// This is a singleton class for logging in to the sync service
    /// </summary>
    abstract class Authentication
    {
        /// <summary>
        /// Log in to the sync provider
        /// </summary>
        abstract public void Login();
        
        /// <summary>
        /// Provide a WebProxy to connect to Flickr (defaults as no proxy)
        /// </summary>
        /// <param name="interactive"></param>
        /// <returns></returns>
        static public WebProxy GetProxy(bool interactive)
        {
            // if not using a proxy return nothing otherwise get proxy details
            if (!Properties.Settings.Default.ProxyUse)
            {
                return null;
            }
            else //TODO: Go over this scenario and make sure it works + warn about plain text password or encrypt it
            // since a WebProxy by default uses the IE proxy settings why duplicate functionality here?
            {
                // variables + assign the saved values
                string DomainAndUser = Properties.Settings.Default.ProxyUser;
                string Password = Properties.Settings.Default.ProxyPass;
                string Domain;
                string User;

                Program.logger.Debug("Trying to use a proxy");

                // if there isn't a saved password then request user input if in interactive mode
                if (Password.Equals(""))
                {
                    if (interactive)
                    {
                        Login l = new Login(DomainAndUser, Password);

                        if (l.OKClicked != true)
                        {
                            return null;
                        }

                        if (!Properties.Settings.Default.ProxyUse)
                        {
                            return null;
                        }

                        DomainAndUser = l.GetUser();
                        Password = l.GetPass();
                    }

                    Properties.Settings.Default.ProxyUser = DomainAndUser;
                    Properties.Settings.Default.Save();
                }

                // split out the domain and user name parts of the string              
                if (DomainAndUser.Contains(@"\"))
                {
                    int pos = DomainAndUser.IndexOf('\\');
                    Domain = DomainAndUser.Substring(0, pos);
                    User = DomainAndUser.Substring(pos + 1, DomainAndUser.Length - pos - 1);
                }
                else
                {
                    Domain = "";
                    User = DomainAndUser;
                }

                // try to create and return the WebProxy
                try
                {
                    WebProxy ProxyObject = new WebProxy(Properties.Settings.Default.ProxyHost, Int16.Parse(Properties.Settings.Default.ProxyPort));
                    ProxyObject.Credentials = new NetworkCredential(User, Password, Domain);

                    return ProxyObject;
                }
                catch (Exception ex)
                {
                    FlickrSync.Error("Error connecting to Proxy", ex, ErrorType.Connect);
                    Program.logger.Error("Error connecting to Proxy");

                    return null;
                }
            }
        }
 
    }        
}
