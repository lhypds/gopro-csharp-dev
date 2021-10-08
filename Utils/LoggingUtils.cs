using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using System;
using System.Diagnostics;

namespace GoProCSharpDev.Utils
{
    public class LoggingUtils
    {
        public static void Info(string text, string name)
        {
            Debug.WriteLine(text);
            try
            {
                ILog log = LogManager.GetLogger(name, name);
                log.Info(text);
            }
            catch (Exception e)
            {
                Debug.WriteLine("LogUtils, Info() exception: " + e.Message);
            }
        }

        public static void Error(string text, string name)
        {
            Debug.WriteLine(text);
            try
            {
                ILog log = LogManager.GetLogger(name, name);
                log.Error(text);
            }
            catch (Exception e)
            {
                Debug.WriteLine("LogUtils, Error() exception: " + e.Message);
            }
        }

        // Use integer ID for name
        public static void Info(string text, long loggingId)
        {
            Info(text, loggingId.ToString());
        }

        public static void Error(string text, long loggingId)
        {
            Error(text, loggingId.ToString());
        }

        public LoggingUtils()
        {
            CreateAppLogging();
        }

        public void CreateAppLogging()
        {
            System.Xml.XmlDocument objDocument = new System.Xml.XmlDocument();
            System.Xml.XmlElement objElement;
            objDocument.Load(Const.APPLOG_CONFIG_PATH);
            objElement = objDocument.DocumentElement;
            ILoggerRepository repository = LoggerManager.CreateRepository(Const.APPLOG_REPO);
            log4net.GlobalContext.Properties["applog"] = Const.APP_LOG_PATH;
            log4net.Config.XmlConfigurator.Configure(repository, objElement);
        }

        // Configure log4net programmably
        // Disbaled as using log4net config file
        // Set the level for a named logger
        public static void SetLevel(string loggerName, string levelName)
        {
            ILog log = LogManager.GetLogger(loggerName);
            Logger logger = (Logger)log.Logger;
            logger.Level = logger.Hierarchy.LevelMap[levelName];
        }

        // Create a new file appender
        public static IAppender CreateFileAppender(string name, string fileName)
        {
            // Set pattern
            var patternLayout = new PatternLayout
            {
                ConversionPattern = "%date | %level | %message%newline"
            };
            patternLayout.ActivateOptions();

            RollingFileAppender appender = new RollingFileAppender
            {
                Name = name,
                File = fileName,
                AppendToFile = true,
                PreserveLogFileNameExtension = true,
                LockingModel = new FileAppender.MinimalLock(),
                Layout = patternLayout,
                MaximumFileSize = "1MB",
                MaxSizeRollBackups = 2
            };
            appender.ActivateOptions();

            PatternLayout layout = new PatternLayout
            {
                ConversionPattern = "%date | %level | %message%newline"  // "%d [%t] %-5p %c [%x] - %m%n";
            };
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();
            return appender;
        }

        // Add an appender to a logger
        public static void AddAppender(string loggerName, IAppender appender)
        {
            ILog log = LogManager.GetLogger(loggerName);
            Logger l = (Logger)log.Logger;
            l.AddAppender(appender);
        }
    }
}
