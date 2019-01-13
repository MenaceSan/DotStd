using System;
using System.IO;

namespace DotStd
{
    public class LogFileDay : LoggerBase
    {
        // Log stuff out to a file that changes daily.

        private DateTime _Day;      // What day was the last?
        private string _sFilePathDay;
        private string _sFilePathPrefix;

        public LogFileDay(string sFilePathPrefix)
        {
            // set dir and name prefix for the *.log file.  
            _sFilePathPrefix = sFilePathPrefix;
        }

        public LogFileDay()
        {
            // set dir and name prefix for the *.log file.  
            _sFilePathPrefix = GetPathPrefix();
        }

        public static string GetPathPrefix(ConfigInfoBase config = null)
        {
            // Get dir and name of the log file
            if (config == null)
            {
                config = ConfigApp.ConfigInfo;
            }
            string logDir = config.GetSetting(ConfigInfoBase.kAppsLogFileDir);
            if (logDir != null)
            {
                return Path.Combine(logDir + config.ConfigMode, ConfigApp.AppName);
            }
            return null;
        }

        public static void PurgeOldLogs()
        {
            // TODO Delete any logs older than 2 weeks.

        }

        public static int GetDayStampInt(DateTime dt)
        {
            // e.g. 20180302
            int iVal = dt.Year;
            iVal *= 100;
            iVal += dt.Month;
            iVal *= 100;
            iVal += dt.Day;
            return iVal;
        }

        TextWriter OpenLog(DateTime tNow)
        {
            // open a log file for today , append or create.
            // check for Day transition

            DateTime tDay = tNow.Date;
            if (tDay != _Day)
            {
                _Day = tDay;
                _sFilePathDay = _sFilePathPrefix + GetDayStampInt(_Day).ToString() + ".log";
                DirUtil.DirCreateForFile(_sFilePathDay);

                // Trim old log files from this directory?
                // TODO FileUtil.DirEmptyOld()
            }

            StreamWriter w = File.AppendText(_sFilePathDay);
            return w;
        }

        public override bool IsEnabled(LogLevel level = LogLevel.Information)
        {
            // ILogger
            return base.IsEnabled(level) && _sFilePathPrefix != null; // Log this?
        }

        public override void LogEntry(string message, LogLevel level = LogLevel.Information, object attached = null)
        {
            // Override this
            if (!IsEnabled(level))   // ignore this?
                return;
            try
            {
                DateTime tNow = DateTime.Now;
                lock (this) using (var w = OpenLog(tNow))
                    {
                        w.WriteLine("{0}{1}{2}", tNow.ToShortTimeString(), GetSeparator(level), message);
                        if (level >= LogLevel.Error)
                        {
                            w.Flush();
                        }
                    }
            }
            catch
            {
                // Don't throw if failed to log.
            }
        }
    }
}
