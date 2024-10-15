using System;

namespace DotStd
{
    /// <summary>
    /// We are dependent on some external service that we don't directly control.
    /// Allow the system to continue to operate if this service is not up?
    /// </summary>
    public abstract class ExternalService
    {
        public abstract string Name { get; }
        public abstract string BaseURL { get; }  // General informational url for admin to look at.
        public abstract string Icon { get; }    // "<i class='fab fa-facebook'></i>"


        public bool IsConfigured = false; // was Config called successfully? i have a password, etc. Is enabling this an option?
        public bool IsEnabled = true;        // false = We know the service is down. don't use it and use whatever backup or user preemptive warning of its failure.

        public bool IsActive => IsEnabled && IsConfigured;        // this service should be good ?

        public DateTime? LastTry;       // Last UTC time we tried to use this service.
        public DateTime? LastSuccess;    // Last UTC time the service seemed to work correctly.

        public string? ErrorMessage;    // how did this service provider fail?

        /// <summary>
        /// Describe the last known status of this service.
        /// </summary>
        /// <returns></returns>
        public virtual string GetStatusStr()
        {
            // Date of failure? Dates even if disabled? Current first attempt looks like failure ?
            if (!IsConfigured)
                return "Not configured";
            if (!IsEnabled)
                return "Disabled";
            if (LastTry == null)
                return "Not attempted";
            if (LastSuccess != null && LastSuccess >= LastTry)
                return $"Success {LastSuccess}"; // FIX ME ?

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                return $"Error '{ErrorMessage}'";
            if (LastSuccess == null)
                return "No success";
            return "Failed";
        }

        public virtual string GetDescHtml()
        {
            return $"{Icon} <a href='{BaseURL}'>{Name}</a> {GetStatusStr()}";
        }

        /// <summary>
        /// We are about to try to use the service now.
        /// Expect a call to UpdateSuccess or UpdateFailure next.
        /// </summary>
        public void UpdateTry()
        {            
            LastTry = TimeNow.Utc;
        }

        /// <summary>
        /// It worked!
        /// </summary>
        public void UpdateSuccess()
        {
            LastSuccess = LastTry = TimeNow.Utc;
        }
        /// <summary>
        /// It failed. Maybe just the call and not the service itself. check for StringUtil._NoErrorMsg ?
        /// </summary>
        /// <param name="errorMsg"></param>
        public void UpdateFailure(string errorMsg)
        {
            LastTry = TimeNow.Utc;
            ErrorMessage = errorMsg;
        }
    }
}
