using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    public enum PhoneCarrierId
    {
        // Can i send SMS to this phone ?
        // https://www.lifewire.com/sms-gateway-from-email-to-sms-text-message-2495456

        Unknown = 0,  // unknown.
        LandLine,       // No SMS

        // free US email gateways (to SMS) we know about.
        ATT,
        Boost,
        Cricket,
        Sprint,
        TMobile,
        USCellular,
        Verizon,
        VirginMobile,

        OtherCarrier = 255,     // Need to use some external provider to figure out the SMS routing.
    }

    public class SmsGateway
    {
        // Send messages to a phone number.
        // Use this for 2 factor auth.

        public static string GetEmail(PhoneCarrierId carrierId)
        {
            // Get email gateway for SMS by provider. for Format("{0}",number).
            // https://www.lifewire.com/sms-gateway-from-email-to-sms-text-message-2495456

            switch (carrierId)
            {
                case PhoneCarrierId.ATT: return "{0}@txt.att.net";
                case PhoneCarrierId.Boost: return "{0}@smsmyboostmobile.com";
                case PhoneCarrierId.Cricket: return "{0}@sms.cricketwireless.net";
                case PhoneCarrierId.Sprint: return "{0}@messaging.sprintpcs.com";
                case PhoneCarrierId.TMobile: return "{0}@tmomail.net";
                case PhoneCarrierId.USCellular: return "{0}@email.uscc.net";
                case PhoneCarrierId.Verizon: return "{0}@vtext.com";
                case PhoneCarrierId.VirginMobile: return "{0}@vmobl.com";
            }
            return null;
        }

        //public async Task SendSmsAsync(string number, string message)
        //{
        //  return null;
        //}

        public void SendSms(PhoneCarrierId carrierId, string number, string subject, string body)
        {
            // Via email. Will impose an odd style on the Text message.
            // Will look like: (empty subject is not allowed)
            // FRM: email name
            // SUBJ: xxx
            // MSG: stuff in body. (optional)
            // Any responses will be back to the sender email.

        }
    }
}
