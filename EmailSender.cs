using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Email Send/SmtpClient settings.
    /// my settings from the config file ConfigInfo used to populate SmtpClient for sending emails. ConfigInfoBase.kSmtp
    /// https://developer.telerik.com/featured/new-configuration-model-asp-net-core/
    /// Similar to System.Net.Configuration.SmtpNetworkElement from MailSettingsSectionGroup, 
    /// like .NET framework <system.net> GetMailSettings as default for SmtpClient. no action required.
    /// system.net/mailSettings/smtp
    /// </summary>
    public class EmailGatewaySettings : ExternalService  // IOptions
    {
        public const int kSMTP_Port = 993;      // default SMTP port.
        public string Host { get; set; } = ""; // AKA host, 
        public int Port { get; set; } = kSMTP_Port;   // (ushort) for SMTP 587 or 25.
        public string Username { get; set; } = "";    // AKA userName
        public string Password { get; set; } = "";   // AKA password. might be encrypted? SecureString
        public bool EnableSsl { get; set; } = true;

        public IValidatorT<string>? SendToFilter = null;    // Filter who we can and cannot send emails to. White/Black list email addresses.

        public override string Name => Host;
        public override string BaseURL => UrlUtil.kHttps + Host;
        public override string Icon => "<i class='fas fa-mail-bulk'></i>";

        public void Init(IPropertyGetter config)
        {
            // like config._Configuration.Bind(this);
            PropertyUtil.InjectProperties(this, config, ConfigInfoBase.kSmtp);
            IsConfigured = !string.IsNullOrWhiteSpace(Host);
        }

        public EmailGatewaySettings(ConfigInfoBase config, IValidatorT<string>? sendToFilter)
        {
            // No need to manually get GetMailSettings MailSettingsSectionGroup in .NET Framework. That is automatic.
            // https://hassantariqblog.wordpress.com/2017/03/20/asp-net-core-sending-email-with-gmail-account-using-asp-net-core-services/

            SendToFilter = sendToFilter;     // Can we send email to anybody ? white list in non prod mode?

            // Search for email config in AppSettings.json for .NET Core.
            if (config.GetSetting(ConfigInfoBase.kSmtp + "Host") != null)
            {
                Init(config);
            }
        }
    }

    /// <summary>
    /// Send email(s).
    /// helper for System.Net.Mail.SmtpClient For use with EmailMessage
    /// https://dotnetcoretutorials.com/2017/08/20/sending-email-net-core-2-0/
    /// </summary>
    public class EmailSender : IMessageSender<EmailMessage>
    {
        public EmailGatewaySettings? Settings { get; set; }  // only used if not already set by 'system.net/mailSettings/smtp'

        private SmtpClient? _client;     // Create a client to send mail.

        public EmailSender(EmailGatewaySettings? settings)
        {
            Settings = settings;
        }

        [MemberNotNullWhen(returnValue: true, member: nameof(_client))]
        protected bool IsValidClient()
        {
            if (_client == null)
                return false;
            if (string.IsNullOrWhiteSpace(_client.Host))
                return false;
            if (_client.Port <= 0)
                return false;
            return true;
        }

        [MemberNotNullWhen(returnValue: true, member: nameof(_client))]
        protected bool InitClient()
        {
            if (_client == null)
            {
                _client = new SmtpClient();     // pulls default params from <system.net> on .NET Framework
                if (this.Settings != null)      // Manually configure it?
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
            return IsValidClient();
        }

        /// <summary>
        /// Is this EmailMessage valid to send ?
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        MailMessage? PrepareToSend(EmailMessage? msg)
        {
            if (msg == null)
                return null;
            if (!msg.IsValidMessage())
                return null;   // not valid to send.

            MailMessage mailMsg = msg.GetMailMessage(); // convert to .NET format.

            // filter external emails for debug/test mode.
            if (Settings?.SendToFilter != null)
            {
                for (int i = mailMsg.To.Count - 1; i >= 0; i--)
                {
                    if (!Settings.SendToFilter.IsValid(mailMsg.To[i].Address))
                    {
                        // cant send email to this address in Test modes. NOT SAFE!
                        // Debug.Log
                        mailMsg.To.RemoveAt(i);
                    }
                }
            }

            if (mailMsg.To.Count <= 0)     // don't sent to no-one !
            {
                return null;       // not valid to send if it has no dest.
            }

            return mailMsg;
        }

        /// <summary>
        /// Send email. This can throw on error.
        /// Make sure at least one Valid point of contact is provided
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>error message or StringUtil._NoErrorMsg = "" = success.</returns>
        public string Send(EmailMessage msg)
        {
            var mailMsg = PrepareToSend(msg);
            if (mailMsg == null)      // Is it valid?
                return "Incomplete email cannot be sent";
            if (!InitClient())
                return "The system is not configured to send email";
            Settings?.UpdateTry();
            _client.Send(mailMsg);
            Settings?.UpdateSuccess();
            return StringUtil._NoErrorMsg;    // ok
        }

        /// <summary>
        /// safe send. no throw.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>error message or StringUtil._NoErrorMsg = "" = success.</returns>
        public string SendSafe(EmailMessage msg)
        {
            try
            {
                return Send(msg);
            }
            catch (Exception ex)
            {
                // Why did it fail to send ?
                Settings?.UpdateFailure(ex.ToString());
                LoggerUtil.DebugError("EmailMessage.SendSafe", ex);
                return ex.Message;
            }
        }

        /// <summary>
        /// Send email. This can throw on error.
        /// Make sure at least one Valid point of contact is provided
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>error message or StringUtil._NoErrorMsg = "" = success.</returns>
        public async Task<string> SendAsync(EmailMessage? msg)
        {
            var mailMsg = PrepareToSend(msg);
            if (mailMsg == null)      // Is it valid?
                return "Incomplete email cannot be sent";
            if (!InitClient() || _client == null)
                return "The system is not configured to send email";
            Settings?.UpdateTry();
            await _client.SendMailAsync(mailMsg);
            Settings?.UpdateSuccess();
            return StringUtil._NoErrorMsg;    // ok
        }

        /// <summary>
        /// safe send. no throw.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>error message or StringUtil._NoErrorMsg = "" = success.</returns>
        public async Task<string> SendSafeAsync(EmailMessage? msg)
        {
            try
            {
                return await SendAsync(msg);
            }
            catch (Exception ex)
            {
                // Why did it fail to send ?
                Settings?.UpdateFailure(ex.ToString());
                LoggerUtil.DebugError("EmailMessage.SendSafeAsync", ex);
                return ex.Message;
            }
        }

        public Task<string> SendAsync(IMessageBase msg)
        {
            return SendAsync(msg as EmailMessage);
        }
    }
}
