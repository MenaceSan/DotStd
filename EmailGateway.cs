using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DotStd
{
    public enum EmailStatusId
    {
        // Test if SMS or email works.
        // used by user_phone.StatusId, user_email.StatusId

        Untested = 0,           // No idea if email or SMS is valid.

        TestMessageSent = 1,    // Waiting for confirm handshake.
        ReConfirmAddress = 2,      // a re-confirm has been sent.
        ConfirmedAddress = 3,          // got confirm back at some DateTime. (maybe old)

        // OpenId Federated logins/validation are validated automatically. OAuth2 based ? Claim.Issuer == principal.Identity.AuthenticationType
        // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/?view=aspnetcore-2.2

        Microsoft = 5,      // Auth type. Azure is the same ?
        Google = 6,         // Auth type name.
        Facebook = 7,
        Twitter = 8,        // NOT USED YET.
        LinkedIn = 9,    // https://docs.microsoft.com/en-us/aspnet/mvc/overview/security/create-an-aspnet-mvc-5-app-with-facebook-and-google-oauth2-and-openid-sign-on

        // etc.
    }

    public class EmailGatewaySettings
    {
        // Send email settings.
        // my settings from the config file ConfigInfo used to populate SmtpClient for sending emails.
        // https://developer.telerik.com/featured/new-configuration-model-asp-net-core/
        // Similar to System.Net.Configuration.SmtpNetworkElement from MailSettingsSectionGroup, 
        // like .NET framework <system.net> GetMailSettings as default for SmtpClient. no action required.
        // system.net/mailSettings/smtp

        public string Host { get; set; }  // AKA host
        public int Port { get; set; }       // 587; or 25
        public string Username { get; set; }    // AKA userName
        public string Password { get; set; }    // AKA password. might be encrypted??

        public IValidatable<string> AllowedFilter = null;    // Filter who we can and cannot send emails to. White list email addresses.

        public void Init(IPropertyGetter config)
        {
            // like config._Configuration.Bind(this);
            PropertyUtil.InjectProperties(this, config, "Smtp:");
        }

        public EmailGatewaySettings(ConfigInfoBase config = null, IValidatable<string> allowedFilter = null)
        {
            // No need to manually get GetMailSettings MailSettingsSectionGroup in .NET Framework. That is automatic.
            // https://hassantariqblog.wordpress.com/2017/03/20/asp-net-core-sending-email-with-gmail-account-using-asp-net-core-services/

            if (config == null)
            {
                config = ConfigApp.ConfigInfo;
            }

            AllowedFilter = config.isConfigModeProd() ? null : allowedFilter;     // Can we send email to anybody ? ignore white list in prod mode.

            // Search for email config in AppSettings.json for .NET Core.
            if (config.GetSetting("Smtp:Host") != null)
            {
                Init(config);
            }
        }
    }

    public class EmailGateway
    {
        // Send email.
        // helper for System.Net.Mail.SmtpClient For use with EmailMessage
        // https://dotnetcoretutorials.com/2017/08/20/sending-email-net-core-2-0/

        public EmailGatewaySettings Settings { get; set; }  // only used if not already set by system.net/mailSettings/smtp

        private SmtpClient _client;     // Create a client to send mail.

        public EmailGateway(EmailGatewaySettings settings)
        {
            Settings = settings;
        }

        public EmailGateway(ConfigInfoBase config, IValidatable<string> allowedFilter)
        {
            Settings = new EmailGatewaySettings(config, allowedFilter);
        }

        protected bool InitClient()
        {
            if (_client == null)
            {
                _client = new SmtpClient();     // pulls default params from <system.net> on .NET Framework

                if (this.Settings != null)      // Manually configure it.
                {
                    if (!String.IsNullOrWhiteSpace(this.Settings.Host))
                    {
                        _client.Host = this.Settings.Host;
                    }
                    if (this.Settings.Port > 0)  // just use default.
                    {
                        _client.Port = this.Settings.Port; // override default.
                    }
                    _client.EnableSsl = true;
                    if (!String.IsNullOrWhiteSpace(this.Settings.Username))
                    {
                        // Security needed for some SMTP servers. // decrypt Pass. 
                        _client.UseDefaultCredentials = false; // use System.Net.CredentialCache.DefaultCredentials?  // MUST be set in proper order !!! This violates normal rules of properties.
                        _client.Credentials = new System.Net.NetworkCredential(this.Settings.Username, this.Settings.Password);     // Domain ??
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(_client.Host))
                return false;
            if (_client.Port <= 0)
                return false;

            return true;
        }

        public bool PrepareToSend(EmailMessage oEmail)
        {
            // Is this email valid to send ?
            // allow override of this. 

            if (!oEmail.isValidMessage())
                return false;   // not valid to send.

            // filter external emails for debug/test mode.
            MailMessage oMessage = oEmail.GetMailMessage();
            if (Settings.AllowedFilter != null)
            {
                for (int i = oMessage.To.Count - 1; i >= 0; i--)
                {
                    if (!Settings.AllowedFilter.IsValid(oMessage.To[i].Address))
                    {
                        // cant send email to this address in Test modes. NOT SAFE!
                        // Debug.Log
                        oMessage.To.RemoveAt(i);
                    }
                }
            }

            if (oMessage.To.Count <= 0)     // sent to no-one !
            {
                return false;       // not valid to send.
            }

            return true;
        }

        public string Send(EmailMessage oEmail)
        {
            // This can throw on error.
            // Make sure at least one Valid point of contact is provided
            // return error message or null = success.

            if (!PrepareToSend(oEmail))      // Is it valid?
                return "Incomplete email cannot be sent";
            if (!InitClient())
                return "The system is not configured to send email";
            _client.Send(oEmail.GetMailMessage());
            return null;    // ok
        }

        public string SendSafe(EmailMessage oEmail)
        {
            // safe send. no throw.
            // return error message or null = success.
            try
            {
                return Send(oEmail);
            }
            catch (Exception ex)
            {
                // Why did it fail to send ?
                LoggerBase.DebugException("EmailMessage.SendSafe", ex);
                return ex.Message;
            }
        }

        public async Task<string> SendAsync(EmailMessage oEmail)
        {
            // return error message or null = success.
            if (!PrepareToSend(oEmail))      // Is it valid?
                return "Incomplete email cannot be sent";
            if (!InitClient())
                return "The system is not configured to send email";
            await _client.SendMailAsync(oEmail.GetMailMessage());
            return null;    // ok
        }

        public async Task<string> SendSafeAsync(EmailMessage oEmail)
        {
            // safe send.  no throw.
            // return error message or null = success.
            try
            {
                return await SendAsync(oEmail);
            }
            catch (Exception ex)
            {
                // Why did it fail to send ?
                LoggerBase.DebugException("EmailMessage.SendSafeAsync", ex);
                return ex.Message;
            }
        }
    }
}
