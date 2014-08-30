using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NLog;

namespace FlickrSync
{
    // Different levels of information logging (serializable to save the setting)
    [Serializable]
    public enum LogLevel
    {
        LogNone = 0,
        LogBasic,
        LogAll,
        LogDebug
    };

    // Different levels of user messages (serializable to save the setting)
    [Serializable]
    public enum MessageLevel
    {
        MessagesNone = 0,
        MessagesBasic,
        MessagesAll
    };

    // Different levels of error messages
    public enum ErrorType 
    { 
        Normal = 0, 
        FatalError, 
        Connect, 
        Info, 
        Debug 
    };        

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        // static class for the main application and other useful classes
        public static FlickrSync MainWindow;
        public static Authentication RemoteAuth;
        public static SyncLocation RemoteLoc;
        public static LocalLocation LocalLoc;

        // this is the Logger instance for the whole program, this one won't actually log to any output
        // the specific output is determined in this classes constructor depending on the user setting
        // or is changed in Preferences class following a change in the logging preference, the outputs
        // are controlled in the Nlog section of app.config
         public static Logger logger = LogManager.GetLogger("LogNone");

        /// <summary>
        /// Get the correct Logger type from the settings
        /// </summary>
        public static void getLoggerType()
        {
            // if the user has a logging setting enabled create the appropriate logger type
            switch (Properties.Settings.Default.LogLevel)
            {
                case LogLevel.LogAll:
                    logger = LogManager.GetLogger("LogAll");
                    break;
                case LogLevel.LogBasic:
                    logger = LogManager.GetLogger("LogBasic");
                    break;
                case LogLevel.LogDebug:
                    logger = LogManager.GetLogger("LogDebug");
                    break;
                case LogLevel.LogNone:
                    logger = LogManager.GetLogger("LogNone"); // this logger doesn't output anything
                    break;
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // set the right Logger up
            getLoggerType();

            MainWindow = new FlickrSync(); // instantiate class

            if (MainWindow != null)
            {
                // section for parsing and auto start
                if (args.Length > 0)
                {
                    // I don't think there is ever going to be more that 1 arg here legally?
                    // On the other hand we will want multiple args for the command line stuff so...
                    foreach (string str in args)
                    {
                        if (str.ToLower().Contains(@"/auto"))
                            FlickrSync.autorun = true;
                    }
                }

                // Start the application or fail if the run method doesn't work
                try
                {
                    Application.Run(MainWindow);
                }
                catch (Exception ex)
                {
                    FlickrSync.Error("Unknown error detected - exiting FlickrSync.", ex, ErrorType.FatalError);
                }
            }
        }
    }
}