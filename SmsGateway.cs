using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    public class SmsGateway
    {
        // Send messages to a phone number.
        // Use this for 2 factor auth.

        public static string GetEmail(PhoneCarrierId carrierId)
        {
            // Get email gateway.
            // https://www.lifewire.com/sms-gateway-from-email-to-sms-text-message-2495456

            return null;
        }

        //public async Task SendSmsAsync(string number, string message)
        //{
        //  return null;
        //}

        public void SendSms(PhoneCarrierId carrierId, string number, string subject, string body)
        {
            // Via email. 
            // Will look like: (empty subject is not allowed)
            // FRM: email name
            // SUBJ: xxx
            // MSG: stuff in body.
            // Any responses will be back to the sender email.

        }
    }
}
