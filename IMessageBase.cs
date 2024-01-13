using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// what sort of communication medium is this ? how is it validated?
    /// Some way to communicate with the user. maybe just one way ?
    /// app_com_type used by user_com.TypeId and app_hostname
    /// </summary>
    public enum ComTypeId
    {
        Unused = 0,     // Junk?

        [Description("Internal Id")]
        InternalId = 1,     // internal messaging is reserved.

        [Description("Voice Phone")]
        VoicePhone = 3,     // land line. voice only. no SMS
        [Description("Mobile Phone")]
        MobilePhone = 4,    // Voice and text. Maybe confirmed? Use CarrierId
        SMS,            // Text only. 2 way. Maybe confirmed? Use CarrierId

        Pager,          // beeper ? one way message. NOT USED?
        Fax = 7,            // Does anyone still use this ? NOT USED?  

        // Skype = 8,           // Skype id is just a number ?

        Email = 10,           // may actually be OpenID or other via known hostname?. 

        // Server based types.
        ActiveDirectory = 11,        // The local ActiveDirectory attached to this machine.
        LDAP = 12,           // we can talk to an LDAP server via LDAP protocols. defined by email extension. (@company.com)

        // [Description("Google Hangout")]
        // GoogleHangout = 12,      // Also an email id.
        // Non email message systems. char id that is not also an email.

        // OpenID types. OpenID login = confirmed email.

        // OpenID Federated logins/validation are validated automatically. OAuth2 based ? Claim.Issuer == principal.Identity.AuthenticationType
        // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/?view=aspnetcore-2.2
        Microsoft = 20,      // OpenID Auth type. Azure is the same ?
        Google = 21,         // Google email. Auth type name.
        Facebook = 22,
        LinkedIn = 23,    // https://docs.microsoft.com/en-us/aspnet/mvc/overview/security/create-an-aspnet-mvc-5-app-with-facebook-and-google-oauth2-and-openid-sign-on
        Twitter = 24,        // NOT USED YET.
        Apple = 25,         // NOT USED YET.

        // WordPress, GitHub,  etc. https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/tree/dev/src

        Other = 50,     // unknown. Don't call this.  
    }

    /// <summary>
    /// Try to confirm the com type is actually active/authorized but user.
    /// </summary>
    public enum ConfirmStatus
    {
        Untested = 0,   // No idea if email or SMS is valid.
        Invalid = 1,       // Failed test! (timeout or external system fail)
        [Description("Confirming address")]
        Sent = 2,        // Was UnTested but confirm message was sent (at some date)  Waiting for confirm handshake.
        [Description("Confirmed address")]
        OK = 3,         // All good. got confirm back at some DateTime. (maybe old)
        [Description("Re-confirming address")]
        ReSent = 4,  // Was OK, but A New confirm message was sent and it must be acknowledge within 3 days or becomes invalid.
    }

    /// <summary>
    /// Helper class for dealing with ComTypeId in general.
    /// </summary>
    public static class ComTypes
    {
        static readonly string[] kTypeIcons =    // ComTypeId
        {
            "",   // ComTypeId.Unused  "fas fa-phone-slash" ?
            "fas fa-key",           // ComTypeId.InternalId
            "fas fa-envelope",      // ComTypeId.Email
            "fas fa-phone",         // ComTypeId.VoicePhone  
            "fas fa-mobile-alt",    // ComTypeId.MobilePhone
            "fas fa-sms",           // ComTypeId.SMS
            "fas fa-pager",         // ComTypeId.Pager
            "fas fa-fax",           // ComTypeId.Fax
        };

        public static string GetIcon(ComTypeId typeId)
        {
            int id = (int)typeId;
            if (id >= 0 && id <= (int)ComTypeId.Fax)
                return kTypeIcons[id];

            switch (typeId)
            {
                // case ComTypeId.ActiveDirectory:  return "fab fa-google-plus-g";
                // case ComTypeId.LDAP:  return "fab fa-google-plus-g";

                // case ComTypeId.GoogleHangout: return "fab fa-google-plus-g";
                // case ComTypeId.Skype: return "fab fa-skype";

                case ComTypeId.Microsoft: return "fab fa-microsoft";
                case ComTypeId.Google: return "fab fa-google";
                case ComTypeId.Facebook: return "fab fa-facebook";
                case ComTypeId.LinkedIn: return "fab fa-linkedin";
                case ComTypeId.Twitter: return "fab fa-twitter";
                case ComTypeId.Apple: return "fab fa-apple";

                default: return "fas fa-info";
            }
        }

        public static string GetHtml(ComTypeId typeId)
        {
            if (typeId == ComTypeId.Unused)
                return "";
            return $"<i class='{GetIcon(typeId)}'></i>";
        }
    }

    /// <summary>
    /// Base class to allow message sending services to be replaced/abstracted.
    /// e.g. Free SMS sending service can be replaced by Twilio etc.
    /// </summary>
    public interface IMessageBase
    {
        string Body { get; set; }
    }

    public interface IMessageSender
    {
        /// <summary>
        /// Send a message  
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>error message or StringUtil._NoErrorMsg = "" = success.</returns>
        Task<string> SendAsync(IMessageBase msg);
    }

    public interface IMessageSender<TMessage> : IMessageSender where TMessage : IMessageBase
    {
        /// <summary>
        /// Asynchronously sends a message.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <returns>
        /// Task that returns a result.
        /// </returns>
        Task<string> SendAsync(TMessage message);
    }
}
