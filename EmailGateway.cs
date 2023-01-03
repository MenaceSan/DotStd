using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Security;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Email Send settings.
    /// my settings from the config file ConfigInfo used to populate SmtpClient for sending emails.
    /// https://developer.telerik.com/featured/new-configuration-model-asp-net-core/
    /// Similar to System.Net.Configuration.SmtpNetworkElement from MailSettingsSectionGroup, 
    /// like .NET framework <system.net> GetMailSettings as default for SmtpClient. no action required.
    /// system.net/mailSettings/smtp
    /// </summary>
    public class EmailGatewaySettings   // IOptions
    {
        public string Host { get; set; } = ""; // AKA host, 
        public int Port { get; set; }       // (ushort) for SMTP 587 or 25.
        public string Username { get; set; } = "";    // AKA userName
        public string Password { get; set; } = "";   // AKA password. might be encrypted? SecureString
        public bool EnableSsl { get; set; } = true;

        public IValidatorT<string>? AllowedFilter = null;    // Filter who we can and cannot send emails to. White list email addresses.

        public void Init(IPropertyGetter config)
        {
            // like config._Configuration.Bind(this);
            PropertyUtil.InjectProperties(this, config, ConfigInfoBase.kSmtp);
        }

        public EmailGatewaySettings(ConfigInfoBase? config = null, IValidatorT<string>? allowedFilter = null)
        {
            // No need to manually get GetMailSettings MailSettingsSectionGroup in .NET Framework. That is automatic.
            // https://hassantariqblog.wordpress.com/2017/03/20/asp-net-core-sending-email-with-gmail-account-using-asp-net-core-services/

            var app = ConfigApp.Instance();
            if (config == null)
            {
                config = app.ConfigInfo;
            }

            AllowedFilter = config.IsEnvironModeProd() ? null : allowedFilter;     // Can we send email to anybody ? ignore white list in prod mode.

            // Search for email config in AppSettings.json for .NET Core.
            if (config.GetSetting(ConfigInfoBase.kSmtp + "Host") != null)
            {
                Init(config);
            }
        }
    }

    public class EmailGateway : IMessageSender<EmailMessage>
    {
        // Send email.
        // helper for System.Net.Mail.SmtpClient For use with EmailMessage
        // https://dotnetcoretutorials.com/2017/08/20/sending-email-net-core-2-0/

        public EmailGatewaySettings Settings { get; set; }  // only used if not already set by system.net/mailSettings/smtp

        private SmtpClient? _client;     // Create a client to send mail.

        public EmailGateway(EmailGatewaySettings settings)
        {
            Settings = settings;
        }

        public EmailGateway(ConfigInfoBase config, IValidatorT<string>? allowedFilter)
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

        public bool PrepareToSend(EmailMessage? msg)
        {
            // Is this EmailMessage valid to send ?
            if (msg == null)
                return false;
            if (!msg.isValidMessage())
                return false;   // not valid to send.

            // filter external emails for debug/test mode.
            MailMessage oMessage = msg.GetMailMessage();
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
                return false;       // not valid to send if it has no dest.
            }

            return true;
        }

        public string Send(EmailMessage msg)
        {
            // This can throw on error.
            // Make sure at least one Valid point of contact is provided
            // return error message or null = success.

            if (!PrepareToSend(msg))      // Is it valid?
                return "Incomplete email cannot be sent";
            if (!InitClient() || _client == null)
                return "The system is not configured to send email";
            _client.Send(msg.GetMailMessage());
            return StringUtil._NoErrorMsg;    // ok
        }

        public string SendSafe(EmailMessage msg)
        {
            // safe send. no throw.
            // return error message or null = success.
            try
            {
                return Send(msg);
            }
            catch (Exception ex)
            {
                // Why did it fail to send ?
                LoggerUtil.DebugException("EmailMessage.SendSafe", ex);
                return ex.Message;
            }
        }

        public async Task<string> SendAsync(EmailMessage? msg)
        {
            // return error message or null = success.
            if (!PrepareToSend(msg) || msg == null)      // Is it valid?
                return "Incomplete email cannot be sent";
            if (!InitClient() || _client == null)
                return "The system is not configured to send email";
            await _client.SendMailAsync(msg.GetMailMessage());
            return StringUtil._NoErrorMsg;    // ok
        }

        public async Task<string> SendSafeAsync(EmailMessage? msg)
        {
            // safe send.  no throw.
            // return error message or StringUtil._NoErrorMsg = success.
            try
            {
                return await SendAsync(msg);
            }
            catch (Exception ex)
            {
                // Why did it fail to send ?
                LoggerUtil.DebugException("EmailMessage.SendSafeAsync", ex);
                return ex.Message;
            }
        }

        public Task<string> SendAsync(IMessageBase msg)
        {
            return SendAsync(msg as EmailMessage);
        }
    }
}
