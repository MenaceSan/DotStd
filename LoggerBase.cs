using System;
using System.Diagnostics;
using System.IO;

namespace DotStd
{
    public enum LogLevel
    {
        // Level of importance of what I'm logging. Defines logging severity levels.
        // similar to System.Diagnostics.EventLogEntryType
        // same as Microsoft.Extensions.Logging.LogLevel

        //
        // Summary:
        //     Logs that contain the most detailed messages. These messages may contain sensitive
        //     application data. These messages are disabled by default and should never be
        //     enabled in a production environment.
        Trace = 0,
        //
        // Summary:
        //     Logs that are used for interactive investigation during development. These logs
        //     should primarily contain information useful for debugging and have no long-term
        //     value.
        Debug = 1,
        //
        // Summary:
        //     Logs that track the general flow of the application. These logs should have long-term
        //     value.
        Information = 2,
        //
        // Summary:
        //     Logs that highlight an abnormal or unexpected event in the application flow,
        //     but do not otherwise cause the application execution to stop.
        Warning = 3,
        //
        // Summary:
        //     Logs that highlight when the current flow of execution is stopped due to a failure.
        //     These should indicate a failure in the current activity, not an application-wide
        //     failure.
        Error = 4,
        //
        // Summary:
        //     Logs that describe an unrecoverable application or system crash, or a catastrophic
        //     failure that requires immediate attention.
        Critical = 5,
        //
        // Summary:
        //     Not used for writing log messages. Specifies that a logging category should not
        //     write any messages.
        None = 6,
    }

    public class LogEntryBase
    {
        // An entry to be logged. may be logged async to producer.

        public string Message;      // Description.
        public LogLevel LevelId = LogLevel.Information;
        public int UserId = ValidState.kInvalidId;  // id for a thread of work for this user/worker. GetCurrentThreadId() ?
        public object Detail;       // extra information. that may be stored via ToString();

        public LogEntryBase()       // props to be populated later.
        { }

        public LogEntryBase(string message, LogLevel levelId = LogLevel.Information, int userId = ValidState.kInvalidId, object detail = null)
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

    public interface ILogger
    {
        // Emulate System.Diagnostics.WriteEntry
        // This can possibly be forwarded to NLog or Log4Net ? AKA Appender.
        // similar to Microsoft.Extensions.Logging.ILogger
        // NOTE: This is not async! Do any async stuff on another thread such that we don't really effect the caller.

        // Is this log message important enough to be logged?
        bool IsEnabled(LogLevel levelId = LogLevel.Information);

        // Log this. assume will also check IsEnabled().
        void LogEntry(LogEntryBase entry);
    }

    public class LoggerBase : ILogger
    {
        // Logging of events. base class.
        // Similar to System.Diagnostics.EventLog
        // NOTE: This is not async! Do any async stuff on another thread such that we don't really effect the caller.

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

        public static string GetSeparator(LogLevel levelId)
        {
            // Separator after time prefix.
            switch (levelId)
            {
                case LogLevel.Warning: return ":?:";
                case LogLevel.Error:
                case LogLevel.Critical: return ":!:";
                default:
                    return ":";
            }
        }

        public virtual void LogEntry(LogEntryBase entry)    // ILogger
        {
            // ILogger Override this
            // default behavior = debug.

            if (!IsEnabled(entry.LevelId))   // ignore this?
                return;

            if (ValidState.IsValidId(entry.UserId))
            {
            }
            if (entry.Detail != null)
            {

            }

            System.Diagnostics.Debug.WriteLine(GetSeparator(entry.LevelId) + entry.Message);
        }


        public void LogEntry(string message, LogLevel levelId = LogLevel.Information,
            int userId = ValidState.kInvalidId,
            object detail = null)
        {
            LogEntry(new LogEntryBase(message, levelId, userId, detail));
        }

        public void info(string message, int userId = ValidState.kInvalidId, object detail = null)
        {
            // Helper.
            LogEntry(message, LogLevel.Information, userId, detail);
        }
        public void warn(string message, int userId = ValidState.kInvalidId, object detail = null)
        {
            // Helper.
            LogEntry(message, LogLevel.Warning, userId, detail);
        }
        public void debug(string message, int userId = ValidState.kInvalidId, object detail = null)
        {
            LogEntry(message, LogLevel.Debug, userId, detail);
        }
        public void trace(string message, int userId = ValidState.kInvalidId, object detail = null)
        {
            LogEntry(message, LogLevel.Trace, userId, detail);
        }
        public void error(string message, int userId = ValidState.kInvalidId, object detail = null)
        {
            LogEntry(message, LogLevel.Error, userId, detail);
        }
        public void fatal(string message, int userId = ValidState.kInvalidId, object detail = null)
        {
            LogEntry(message, LogLevel.Critical, userId, detail);
        }

        public static bool IsExceptionDetailLogged(LogLevel levelId)
        {
            // Do i want to log full detail for an Exception?

            if (levelId >= LogLevel.Error)    // Always keep stack trace etc for error.
                return true;

            // Is debug mode ?

            return false;
        }

        public virtual void LogException(Exception oEx, LogLevel levelId = LogLevel.Error, int userId = ValidState.kInvalidId)
        {
            // Helper for Special logging for exceptions.

            object detail = null;
            if (IsExceptionDetailLogged(levelId))
                detail = oEx;

            LogEntry(oEx.Message, LogLevel.Critical, userId, detail);
        }
    }

    public static class LoggerUtil
    {
        public static LoggerBase LogStart;     // always log fine detail at startup.

        public static void DebugEntry(string message, int userId = ValidState.kInvalidId)
        {
            // Not officially logged. Just debug console.
            System.Diagnostics.Debug.WriteLine("Debug " + message);
            // System.Diagnostics.Trace.WriteLine();

            if (LogStart != null)
            {
                LogStart.LogEntry(message, LogLevel.Debug, userId);
            }
        }

        public static void DebugException(string subject, Exception ex, int userId = ValidState.kInvalidId)
        {
            // an exception that I don't do anything about! NOT going to call LogException
            // set a break point here if we want.

            System.Diagnostics.Debug.WriteLine("DebugException " + subject + ":" + ex?.Message);

            // Console.WriteLine();
            // System.Diagnostics.Trace.WriteLine();

            if (LogStart != null)
            {
                LogStart.LogEntry(subject, LogLevel.Error, userId);
            }
        }

        public static void CreateStartupLog(string filePath)
        {
            // For startup also check:
            // System Event Logger for Applications.
            // IIS web.config stdoutLogFile.       <aspNetCore processPath=".\FourTeAdminWeb.exe" stdoutLogEnabled="true" stdoutLogFile="C:\FourTe\stdout" hostingModel="InProcess">
            // IIS wwwroot/logs/
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-3.1

            LogStart = new LogFileBase(filePath);
        }
    }
}
