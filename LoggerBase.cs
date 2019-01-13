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

    public interface ILogger
    {
        // Emulate System.Diagnostics.WriteEntry
        // This can possibly be forwarded to NLog or Log4Net ?
        // similar to Microsoft.Extensions.Logging.ILogger

        bool IsEnabled(LogLevel eLevel = LogLevel.Information);
        void LogEntry(string sMessage, LogLevel eLevel = LogLevel.Information, object attached = null);
    }

    public class LoggerBase : ILogger
    {
        // Logging of events. base class.
        // Similar to System.Diagnostics.EventLog

        protected LogLevel _FilterLevel = LogLevel.Debug;      // Only log stuff at this level and above in importance.

        public LogLevel FilterLevel { get { return _FilterLevel; } }      // Only log stuff at this level and above in importance.

        public void SetFilterLevel(LogLevel filterLevel)
        {
            _FilterLevel = filterLevel;
        }

        public virtual bool IsEnabled(LogLevel level = LogLevel.Information)
        {
            // Quick filter check to see if this type is logged. Check this first if the rendering would be heavy.
            return (level >= _FilterLevel); // Log this?
        }

        public static string GetSeparator(LogLevel eType)
        {
            // Separator after time prefix.
            switch (eType)
            {
                case LogLevel.Warning: return ":?:";
                case LogLevel.Error:
                case LogLevel.Critical: return ":!:";
                default:
                    return ":";
            }
        }

        public virtual void LogEntry(string message, LogLevel level = LogLevel.Information, object attached = null)
        {
            // Override this
            if (!IsEnabled(level))   // ignore this?
                return;
            System.Diagnostics.Debug.WriteLine(GetSeparator(level) + message);
        }

        public void info(string message, object attached = null)
        {
            LogEntry(message, LogLevel.Information, attached);
        }
        public void warn(string message, object attached = null)
        {
            LogEntry(message, LogLevel.Warning, attached);
        }
        public void debug(string message, object attached = null)
        {
            LogEntry(message, LogLevel.Debug, attached);
        }
        public void trace(string message, object attached = null)
        {
            LogEntry(message, LogLevel.Trace, attached);
        }
        public void error(string message, object attached = null)
        {
            LogEntry(message, LogLevel.Error, attached);
        }
        public void fatal(string message, object attached = null)
        {
            LogEntry(message, LogLevel.Critical, attached);
        }

        public static void DebugException(string subject, Exception ex)
        {
            // an exception that I don't do anything about! NOT going to call LogException
            // set a break point here if we want.
            System.Diagnostics.Debug.WriteLine("DebugException " + subject + ":" + ex?.Message);
            // Console.WriteLine();
            // System.Diagnostics.Trace.WriteLine();
        }

        public virtual void LogException(Exception oEx, LogLevel level = LogLevel.Error, bool bReRaise = true)
        {
            // Special logging for exceptions.
            string strMessage;
            switch (level)
            {
                case LogLevel.Information:
                    // Normal has no logging just allows you to re-raise it to the front end.
                    strMessage = oEx.Message;
                    break;
                case LogLevel.Error:
                    // If in errors level we'll keep the error message
                    strMessage = oEx.Message;
                    LogEntry(strMessage, LogLevel.Error);
                    break;
                case LogLevel.Critical:
                    // If in debug build full error string
                    strMessage = oEx.ToString();
                    LogEntry(strMessage, LogLevel.Critical);
                    break;
                case LogLevel.Trace:
                case LogLevel.Warning:
                default:
                    // If in debug build full error string
                    strMessage = oEx.ToString();
                    LogEntry(strMessage, LogLevel.Error);
                    break;
            }

            // Should we re-raise the error?
            if (bReRaise)
            {
                // So that we see the actual error based on the Error Level create a new
                // exception object and fill it.
                var NewEx = new Exception(strMessage, oEx.InnerException);
                // Send it to the caller
                throw NewEx;
            }
        }
    }
}
