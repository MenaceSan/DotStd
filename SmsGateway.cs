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

        // "vmobl.com"
        [Description("Virgin Mobile")]
        VirginMobile,

        // "tmomail.net"
        Mint,   

        // H2O 
        // Metro
        // Ting
        // TracPhone

    }

    public class SmsMessage : IMessageBase
    {
        public SmsCarrierId CarrierId { get; set; }
        public string ToNumber { get; set; } = string.Empty;   // International format E164 phone number.
        public string Body { get; set; } = string.Empty;    // override
    }

    /// <summary>
    /// Send messages to a phone number.
    /// Use this for 2 factor auth (2fa). (can also use email)
    /// </summary>
    public class SmsGateway : IMessageSender<SmsMessage>    // : ExternalService
    {
        /// <summary>
        /// Carrier SMS email gateways. https://www.lifewire.com/sms-gateway-from-email-to-sms-text-message-2495456
        /// </summary>
        public static readonly Dictionary<SmsCarrierId, string> _dicEmail = new()
        {
            { SmsCarrierId.ATT,  "txt.att.net"},
            { SmsCarrierId.Boost,  "smsmyboostmobile.com"},
            { SmsCarrierId.Cricket,  "sms.cricketwireless.net"},
            { SmsCarrierId.Sprint,  "messaging.sprintpcs.com"},
            { SmsCarrierId.TMobile,  "tmomail.net"},
            { SmsCarrierId.USCellular,  "email.uscc.net"},
            { SmsCarrierId.Verizon,  "vtext.com"},
            { SmsCarrierId.VirginMobile,  "vmobl.com"},
            { SmsCarrierId.Mint, "tmomail.net"},
        };

        private readonly EmailSender _mailGateway;
        private readonly string _mailFrom;       // "noreply@menasoft.com"

        public SmsGateway(EmailSender mailGateway, string mailFrom)
        {
            _mailGateway = mailGateway;
            _mailFrom = mailFrom;
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

            if (!_dicEmail.TryGetValue(carrierId, out string? carrierEmail))
            {
                return Task.FromResult("Unknown carrier for free SMS sending.");
            }

            var msg = new EmailMessage(_mailFrom, false)
            {
                Subject = subject,
                Body = body ?? string.Empty,
            };
            msg.AddMailTo(toNumber + "@" + carrierEmail);

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
