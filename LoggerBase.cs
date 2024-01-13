using System;

namespace DotStd
{
    /// <summary>
    /// Level of importance of what I'm logging. Defines logging severity levels.
    /// similar to System.Diagnostics.EventLogEntryType
    /// same as Microsoft.Extensions.Logging.LogLevel
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Logs that contain the most detailed messages. These messages may contain sensitive application data. 
        /// These messages are disabled by default and should never be enabled in a production environment.
        /// </summary>
        Trace = 0,
        /// <summary>
        /// Logs that are used for interactive investigation during development.
        /// These logs should primarily contain information useful for debugging and have no long-term value.
        /// </summary>
        Debug = 1,
        /// <summary>
        /// Logs that track the general flow of the application. These logs should have long-term value.
        /// </summary>
        Information = 2,
        /// <summary>
        /// Logs that highlight an abnormal or unexpected event in the application flow, 
        /// but do not otherwise cause the application execution to stop.
        /// </summary>
        Warning = 3,
        /// <summary>
        /// Logs that highlight when the current flow of execution is stopped due to a failure.
        /// These should indicate a failure in the current activity, not an application-wide failure.
        /// </summary>
        Error = 4,
        /// <summary>
        /// Logs that describe an unrecoverable application or system crash,
        /// or a catastrophic failure that requires immediate attention.
        /// </summary>
        Critical = 5,
        /// <summary>
        /// Not used for writing log messages. Specifies that a logging category should not write any messages.
        /// </summary>
        None = 6,
    }

    /// <summary>
    /// An entry to be logged. may be logged async to producer. (on another thread)
    /// Assume time stamp is Now.
    /// </summary>
    public class LogEntryBase
    {
        public string Message = string.Empty;   // Description of the event. NOTE: Use ToString() instead of this directly to get args.
        public LogLevel LevelId = LogLevel.Information;
        public int UserId = ValidState.kInvalidId;  // id for a thread of work for this user/worker. GetCurrentThreadId() ?
        public object? Detail;       // extra information. that may be stored via ToString();

        public LogEntryBase()       // props to be populated later.
        { }

        public LogEntryBase(string message, LogLevel levelId = LogLevel.Information, int userId = ValidState.kInvalidId, object? detail = null)
        {
            Message = message;
            LevelId = levelId;
            UserId = userId;    // ValidState.IsValidId(userId) GetCurrentThreadId()
            Detail = detail;
        }

        public override string ToString()
        {
            return Message;
        }
    }

    /// <summary>
    /// Emulate System.Diagnostics.WriteEntry
    /// This can possibly be forwarded to NLog or Log4Net ? AKA Sink.
    /// similar to Microsoft.Extensions.Logging.ILogger
    /// NOTE: This is not async! Do any async stuff on another thread such that we don't really effect the caller.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Is this log message important enough to be logged?
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns></returns>
        bool IsEnabled(LogLevel levelId = LogLevel.Information);

        /// <summary>
        /// Log this. assume will also check IsEnabled().
        /// </summary>
        /// <param name="entry"></param>        
        void LogEntry(LogEntryBase entry);
    }

    /// <summary>
    /// Logging of events. base class.
    /// Similar to System.Diagnostics.EventLog
    /// NOTE: This is not async! Do any async stuff on another thread such that we don't really effect the caller.
    /// </summary>
    public class LoggerBase : ILogger
    {
        protected LogLevel _FilterLevel = LogLevel.Debug;      // Only log stuff at this level and above in importance.

        public LogLevel FilterLevel => _FilterLevel;         // Only log stuff at this level and above in importance.

        public void SetFilterLevel(LogLevel levelId)
        {
            // Only log stuff this important or better.
            _FilterLevel = levelId;
        }

        public virtual bool IsEnabled(LogLevel levelId = LogLevel.Information)    // ILogger
        {
            // ILogger Override this
            // Quick filter check to see if this type is logged. Check this first if the rendering would be heavy.
            return levelId >= _FilterLevel; // Log this?
        }

        /// <summary>
        /// Separator after time prefix.
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns></returns>
        public static string GetSeparator(LogLevel levelId)
        {
            switch (levelId)
            {
                case LogLevel.Warning: return ":?:";
                case LogLevel.Error:
                case LogLevel.Critical: return ":!:";
                default:
                    return ":";
            }
        }

        /// <summary>
        /// ILogger Override this. default behavior = debug.
        /// </summary>
        /// <param name="entry"></param>
        public virtual void LogEntry(LogEntryBase entry)    // ILogger
        {
            if (!IsEnabled(entry.LevelId))   // ignore this?
                return;

            if (ValidState.IsValidId(entry.UserId))
            {
            }
            if (entry.Detail != null)
            {

            }

            System.Diagnostics.Debug.WriteLine(GetSeparator(entry.LevelId) + entry.ToString());
        }


        public void LogEntry(string message, LogLevel levelId = LogLevel.Information,
            int userId = ValidState.kInvalidId,
            object? detail = null)
        {
            LogEntry(new LogEntryBase(message, levelId, userId, detail));
        }

        public void info(string message, int userId = ValidState.kInvalidId, object? detail = null)
        {
            // Helper.
            LogEntry(message, LogLevel.Information, userId, detail);
        }
        public void warn(string message, int userId = ValidState.kInvalidId, object? detail = null)
        {
            // Helper.
            LogEntry(message, LogLevel.Warning, userId, detail);
        }
        public void debug(string message, int userId = ValidState.kInvalidId, object? detail = null)
        {
            LogEntry(message, LogLevel.Debug, userId, detail);
        }
        public void trace(string message, int userId = ValidState.kInvalidId, object? detail = null)
        {
            LogEntry(message, LogLevel.Trace, userId, detail);
        }
        public void error(string message, int userId = ValidState.kInvalidId, object? detail = null)
        {
            LogEntry(message, LogLevel.Error, userId, detail);
        }
        public void fatal(string message, int userId = ValidState.kInvalidId, object? detail = null)
        {
            LogEntry(message, LogLevel.Critical, userId, detail);
        }

        /// <summary>
        /// Do i want to log full detail for an Exception?
        /// </summary>
        /// <param name="levelId"></param>
        /// <returns></returns>
        public static bool IsExceptionDetailLogged(LogLevel levelId)
        {
            if (levelId >= LogLevel.Error)    // Always keep stack trace etc for error.
                return true;
            // Is debug mode ?
            return false;
        }

        /// <summary>
        /// Helper for Special logging for exceptions.
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="levelId"></param>
        /// <param name="userId"></param>
        public virtual void LogException(Exception ex, LogLevel levelId = LogLevel.Error, int userId = ValidState.kInvalidId)
        {
            object? detail = null;
            if (IsExceptionDetailLogged(levelId))
                detail = ex;

            LogEntry(ex.Message, LogLevel.Critical, userId, detail);
        }
    }

    /// <summary>
    /// Global static logging helper. Uses LogStart.
    /// </summary>
    public static class LoggerUtil
    {
        public static LoggerBase? LogStart;     // always log fine detail at startup.

        /// <summary>
        /// Not officially logged. Just debug console. Like LogLevel.Debug.. Compile this out?
        /// </summary>
        /// <param name="message"></param>
        /// <param name="userId"></param>
        public static void DebugEntry(string message, int userId = ValidState.kInvalidId)
        {
            // startup ?
            System.Diagnostics.Debug.WriteLine("Debug: " + message);
            // System.Diagnostics.Trace.WriteLine();
            if (LogStart != null)
            {
                LogStart.LogEntry(message, LogLevel.Debug, userId);
            }
        }

        /// <summary>
        /// an exception that I don't do anything about! NOT going to call LogException
        /// set a break point here if we want.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="ex">some Exception</param>
        /// <param name="userId"></param>
        public static void DebugError(string subject, Exception? ex = null, int userId = ValidState.kInvalidId)
        {
            System.Diagnostics.Debug.WriteLine("DebugException " + subject + ":" + ex?.Message);

            // Console.WriteLine();
            // System.Diagnostics.Trace.WriteLine();

            if (LogStart != null)
            {
                LogStart.LogEntry(subject, LogLevel.Error, userId);
            }
        }

        /// <summary>
        /// this should never happen. Break point this!
        /// </summary>
        public static void DebugNEVER(string subject)
        {

        }

        /// <summary>
        /// Create a high detail log for startup.
        /// For startup also check:
        /// System Event Logger for Applications.
        /// IIS web.config stdoutLogFile. <aspNetCore processPath=".\FourTeAdminWeb.exe" stdoutLogEnabled="true" stdoutLogFile="C:\FourTe\stdout" hostingModel="InProcess">
        /// IIS wwwroot/logs/
        /// https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-3.1
        /// </summary>
        /// <param name="filePath"></param>
        public static void CreateStartupLog(string filePath)
        {
            LogStart = new LogFileBase(filePath);
        }
    }
}
