using System.ComponentModel;
using System.Threading.Tasks;

namespace DotStd
{
    public enum ComTypeId
    {
        // what sort of communication medium is this ?
        // app_com_type used by user_com.TypeId

        Unused = 0,
        [Description("Internal Id")]
        InternalId = 1,     // internal messaging is reserved.
        Email = 2,          // may be openid.
        [Description("Voice Phone")]
        VoicePhone = 3,     // land line. voice only.
        [Description("Mobile Phone")]
        MobilePhone = 4,    // Voice and text
        SMS,            // Text only. 2 way
        Pager,          // beeper ? one way.
        Fax,            // Does anyone still use this ?  
        Skype = 8,
        [Description("Google Hangout")]
        GoogleHangout = 9,      // needs an email id.
        Other = 10,     // unknown. Don't call this.  

    }

    public static class ComTypes
    {
        public static readonly string[] kTypeIcons =    // ComTypeId
        {
            "",   // ComTypeId.Unused  "fas fa-phone-slash"
            "fas fa-key",           // ComTypeId.InternalId
            "fas fa-envelope",      // ComTypeId.Email
            "fas fa-phone",         // ComTypeId.VoicePhone  
            "fas fa-mobile-alt",    // ComTypeId.MobilePhone
            "fas fa-sms",           // ComTypeId.SMS
            "fas fa-pager",         // ComTypeId.Pager
            "fas fa-fax",           // ComTypeId.Fax
            "fab fa-skype",         // ComTypeId.Skype
            "fab fa-google-plus-g",     //  
            "fas fa-info",      // ComTypeId.Other
        };
    }

    public enum ComValidId
    {
        // Test if SMS or email works. Validate.
        // used by user_com.ValidId

        Disabled = 0,       // dont use this.

        Untested = 1,           // No idea if email or SMS is valid.

        [Description("Confirming address")]
        TestMessageSent = 2,    // Waiting for confirm handshake.

        [Description("Re-confirming address")]
        ReConfirmAddress = 3,      // a re-confirm has been sent.

        [Description("Confirmed address")]
        ConfirmedAddress = 6,          // got confirm back at some DateTime. (maybe old)

        ActiveDirectory = 8,        // The local ActiveDirectory attached to this machine.
        LDAP = 9,           // we can talk to an LDAP server via LDAP protocols. defined by email extension. (@company.com)

        // OpenId Federated logins/validation are validated automatically. OAuth2 based ? Claim.Issuer == principal.Identity.AuthenticationType
        // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/?view=aspnetcore-2.2
        Microsoft = 10,      // OpenId Auth type. Azure is the same ?
        Google = 11,         // Google email. Auth type name.
        Facebook = 12,
        Twitter = 13,        // NOT USED YET.
        LinkedIn = 14,    // https://docs.microsoft.com/en-us/aspnet/mvc/overview/security/create-an-aspnet-mvc-5-app-with-facebook-and-google-oauth2-and-openid-sign-on
        Apple = 15,         // NOT USED YET.

        // WordPress, GitHub,  etc. https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/tree/dev/src
 
    }

    public interface IMessageBase
    {
        // Base class to allow message sending services to be replaced.
        // Free SMS sending service can be replaced by Twilio etc.
        string Body { get; set; }
    }

    public interface IMessageSender
    {
        // Send a message 
        // RETURN: null or "" = ok.
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
