using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace DotStd
{
    public class LogFileBase : LoggerBase
    {
        // NOTE: Log files are in server local time zone!

        protected string _FilePath;   // Current logging filename . Opened when needed.
        protected bool _Creating = true;

        public LogFileBase(string name)
        {
            _FilePath = name;
        }

        public static int GetDayStampInt(DateTime dtLocal)
        {
            // encode date as readable int. e.g. 20180302
            // dtLocal = local time zone adjusted time.
            int iVal = dtLocal.Year;
            iVal *= 100;
            iVal += dtLocal.Month;
            iVal *= 100;
            iVal += dtLocal.Day;
            return iVal;
        }

        public const string kExt = ".log";

        public const string kDefaultPrefix = "/tmp/Log_";  // fallback log location. should be valid on most systems. try not to use this.

        protected virtual TextWriter? OpenLogFile(DateTime dtLocal)
        {
            // open a log file, append or create.
            // make sure the directory exists. May throw ?

            if (string.IsNullOrEmpty(_FilePath))
                return null;

            if (_Creating)
            {
                DirUtil.DirCreateForFile(_FilePath);

                // Trim old log files from this directory?
                // TODO FileUtil.DirEmptyOld()
                _Creating = !File.Exists(_FilePath);
            }

            StreamWriter w = File.AppendText(_FilePath);

            if (_Creating)
            {
                // All log files should have this header.
                var app = AppRoot.Instance();
                w.WriteLine($"Log File Created '{dtLocal}' ({TimeZoneInfo.Local.DisplayName}) for '{app.AppName}' v{app.AppVersionStr}");
                w.Flush();
            }
            return w;
        }

        public override void LogEntry(LogEntryBase entry)
        {
            // Write log entry line.
            // Override this
            if (!IsEnabled(entry.LevelId))   // ignore this entry?
                return;

            try
            {
                DateTime tLocalNow = DateTime.Now;       // local server time for log file stamp
                lock (this) using (var w = OpenLogFile(tLocalNow))
                    {
                        if (w == null)
                            return;
                        w.WriteLine(string.Concat(tLocalNow.ToDtString("HH:mm:ss"), GetSeparator(entry.LevelId), entry.ToString())); // local TZ
                        if (!ValidState.IsEmpty(entry.Detail))
                        {
                            w.WriteLine("\t" + entry.Detail);
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
        // Use local system time zone for text versions of time.
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

        /// <summary>
        /// Get directory and name of the log file
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static string GetPathPrefix(ConfigInfoBase? config = null)
        {
            string prefix = kDefaultPrefix;
            var app = AppRoot.Instance();

            if (config == null)
            {
                config = app.ConfigInfo;
            }

            if (config != null)
            {
                string? logDir = config.GetSetting(ConfigInfoBase.kAppsLogFileDir);
                if (!string.IsNullOrWhiteSpace(logDir))
                {
                    prefix = logDir;
                }
                prefix += config.EnvironMode;
            }

            return Path.Combine(prefix, app.AppName);
        }

        public static void PurgeOldLogs(int daysOld)
        {
            // TODO Delete any logs older than 2 weeks.

        }

        public string GetName(DateTime dtLocal)
        {
            return string.Concat(_FilePathPrefix, GetDayStampInt(dtLocal).ToString(), kExt);
        }

        protected override TextWriter? OpenLogFile(DateTime dtLocal)
        {
            // open a log file for today , append or create.
            // check for Day transition

            DateTime tDay = dtLocal.Date;

            if (tDay != _Day)
            {
                _Day = tDay;
                _FilePath = GetName(_Day);
                _Creating = true;
            }

            return base.OpenLogFile(dtLocal);
        }

        [MemberNotNullWhen(returnValue: true, member: nameof(_FilePathPrefix))]
        public override bool IsEnabled(LogLevel level = LogLevel.Information)
        {
            // ILogger
            return base.IsEnabled(level) && _FilePathPrefix != null; // Log this?
        }
    }
}
