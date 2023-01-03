using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Can i send SMS to this phone carrier?
    /// https://www.lifewire.com/sms-gateway-from-email-to-sms-text-message-2495456
    /// https://en.wikipedia.org/wiki/Mobile_phone_industry_in_the_United_States
    /// </summary>
    public enum SmsCarrierId
    {
        Unknown = 0,  // unknown. 
        [Description("Other Carrier")]
        OtherCarrier = 1,     // Need to use some external provider to figure out the SMS routing. Twilio?

        // free US email gateways (to SMS) we know about.
        [Description("AT&T")]
        ATT,             
        Boost,
        Cricket,
        Sprint,
        [Description("T-Mobile")]
        TMobile,
        [Description("US-Cellular")]
        USCellular,
        Verizon,
        [Description("Virgin Mobile")]
        VirginMobile,

        // H2O 
        // Metro
        // Ting
        // TracPhone
        // Mint

    }

    public class SmsMessage : IMessageBase
    {
        public SmsCarrierId CarrierId { get; set; }
        public string ToNumber { get; set; } = string.Empty;   // International format E164 phone number.
        public string Body { get; set; } = string.Empty;    // override
    }

    /// <summary>
    /// Send messages to a phone number.
    /// </summary>
    public class SmsGateway : IMessageSender<SmsMessage>
    {
        // 
        // Use this for 2 factor auth (2fa). (can also use email)

        public static readonly Dictionary<SmsCarrierId, string> _dicTo = new Dictionary<SmsCarrierId, string>
        {
            { SmsCarrierId.ATT,  "{0}@txt.att.net"},
            { SmsCarrierId.Boost,  "{0}@smsmyboostmobile.com"},
            { SmsCarrierId.Cricket,  "{0}@sms.cricketwireless.net"},
            { SmsCarrierId.Sprint,  "{0}@messaging.sprintpcs.com"},
            { SmsCarrierId.TMobile,  "{0}@tmomail.net"},
            { SmsCarrierId.USCellular,  "{0}@email.uscc.net"},
            { SmsCarrierId.Verizon,  "{0}@vtext.com"},
            { SmsCarrierId.VirginMobile,  "{0}@vmobl.com"},
        };

        private EmailGateway _mailGateway;
        private string _mailFrom;       // "noreply@menasoft.com"

        public SmsGateway(EmailGateway mailGateway, string mailFrom)
        {
            _mailGateway = mailGateway;
            _mailFrom = mailFrom;
        }

        public static string? GetEmailForm(SmsCarrierId carrierId)
        {
            // Get email gateway for SMS by provider. for Format("{0}",number).
            // https://www.lifewire.com/sms-gateway-from-email-to-sms-text-message-2495456

            if (_dicTo.TryGetValue(carrierId, out string? formTo))
            {
                return formTo;
            }
            return null;
        }

        /// <summary>
        /// Send SMS via (FREE) email gateway.
        /// Via email. Will impose an odd style on the Text message.
        /// </summary>
        /// <param name="carrierId"></param>
        /// <param name="toNumber"></param>
        /// <param name="subject"></param>
        /// <param name="body">is optional</param>
        /// <returns></returns>
        public Task<string> SendAsync(SmsCarrierId carrierId, string toNumber, string subject, string? body = null)
        {
            // Will look like: (empty subject is not allowed)
            // FRM: email name
            // SUBJ: xxx
            // MSG: stuff in body. (optional)
            // Any responses will be back to the sender email.

            if (!_dicTo.TryGetValue(carrierId, out string? formTo))
            {
                return Task.FromResult("Unknown carrier for free SMS sending.");
            }

            var msg = new EmailMessage(_mailFrom, false)
            {
                Subject = subject,
                Body = body ?? string.Empty,
            };
            msg.AddMailTo(string.Format(formTo, toNumber));

            return _mailGateway.SendAsync(msg);
        }

        public Task<string> SendAsync(SmsMessage? msg)
        {
            if (msg == null)
                return Task.FromResult("Bad message type");

            return SendAsync(msg.CarrierId, msg.ToNumber, msg.Body, null);
        }

        public Task<string> SendAsync(IMessageBase? msg)
        {
            return SendAsync(msg as SmsMessage);
        }
    }
}
