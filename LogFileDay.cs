using System;
using System.IO;

namespace DotStd
{
    public class LogFileBase : LoggerBase
    {
        protected string _FilePath;   // Current logging filename . Opened when needed.
        protected bool _Created = true;

        public LogFileBase(string name)
        {
            _FilePath = name;
        }

        public static int GetDayStampInt(DateTime dt)
        {
            // encode date as int. e.g. 20180302
            int iVal = dt.Year;
            iVal *= 100;
            iVal += dt.Month;
            iVal *= 100;
            iVal += dt.Day;
            return iVal;
        }

        public const string kExt = ".log";

        public const string kDefaultPrefix = "/tmp/Log_";  // try not to use this.

        protected virtual TextWriter OpenLogFile(DateTime dt)
        {
            // open a log file, append or create.
            // make sure the directory exists. May throw ?

            if (string.IsNullOrEmpty(_FilePath))
                return null;

            if (_Created)
            {
                DirUtil.DirCreateForFile(_FilePath);

                // Trim old log files from this directory?
                // TODO FileUtil.DirEmptyOld()
                _Created = !File.Exists(_FilePath);
            }

            StreamWriter w = File.AppendText(_FilePath);

            if (_Created)
            {
                // All log files should have this header.
                w.WriteLine($"Log File Created '{dt}' for '{ConfigApp.AppName}' v{ConfigApp.AppVersionStr}");
                w.Flush();
            }
            return w;
        }

        public override void LogEntry(LogEntryBase entry)
        {
            // Override this
            if (!IsEnabled(entry.LevelId))   // ignore this entry?
                return;

            try
            {
                DateTime tNow = DateTime.Now;       // local server time.
                lock (this) using (var w = OpenLogFile(tNow))
                    {
                        if (w == null)
                            return;
                        w.WriteLine(string.Concat(tNow.ToString("HH:mm:ss"), GetSeparator(entry.LevelId), entry.Message));
                        if (!ValidState.IsEmpty(entry.Detail))
                        {
                            w.WriteLine("\t" + entry.ToString());
                        }
                        if (entry.LevelId >= LogLevel.Error || LoggerUtil.LogStart != null)  // important messages should be flushed immediately. In case we crash.
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

    public class LogFileDay : LogFileBase
    {
        // Log stuff out to a file that changes daily.
        // Thread safe.

        protected readonly string _FilePathPrefix;
        private DateTime _Day;      // What day was the last? Date

        public LogFileDay(string filePathPrefix) : base("")
        {
            // set directory and name prefix for the *.log file.  
            _FilePathPrefix = filePathPrefix;
        }

        public LogFileDay() : base("")
        {
            // set directory and name prefix for the *.log file.  
            _FilePathPrefix = GetPathPrefix();
        }

        public static string GetPathPrefix(ConfigInfoBase config = null)
        {
            // Get directory and name of the log file

            string prefix = kDefaultPrefix;

            if (config != null)
            {
                config = ConfigApp.ConfigInfo;
            }

            if (config != null)
            {
                string logDir = config.GetSetting(ConfigInfoBase.kAppsLogFileDir);
                if (!string.IsNullOrWhiteSpace(logDir))
                {
                    prefix = logDir;
                }
                if (!string.IsNullOrWhiteSpace(config.EnvironMode))
                {
                    prefix += config.EnvironMode;
                }
            }

            return Path.Combine(prefix, ConfigApp.AppName);
        }

        public static void PurgeOldLogs(int daysOld)
        {
            // TODO Delete any logs older than 2 weeks.

        }

        public string GetName(DateTime dt)
        {
            return _FilePathPrefix + GetDayStampInt(dt).ToString() + kExt;
        }

        protected override TextWriter OpenLogFile(DateTime tNow)
        {
            // open a log file for today , append or create.
            // check for Day transition

            DateTime tDay = tNow.Date;

            if (tDay != _Day)
            {
                _Day = tDay;
                _FilePath = GetName(_Day);
                _Created = true;
            }

            return base.OpenLogFile(tNow);
        }

        public override bool IsEnabled(LogLevel level = LogLevel.Information)
        {
            // ILogger
            return base.IsEnabled(level) && _FilePathPrefix != null; // Log this?
        }
    }
}
