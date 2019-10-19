using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DotStd
{
    public class EmailMessage   // : IValidatable
    {
        // Helper for System.Net.Mail.MailMessage
        // Send Emails used for : password reset, password recovery, instant reports, etc. 
        // Wrapper for MailMessage

        protected readonly MailMessage _message;

        public EmailMessage(string sMailFromAddr, bool isBodyHtml)
        {
            _message = new MailMessage { From = new MailAddress(sMailFromAddr), IsBodyHtml = isBodyHtml };
        }
        public EmailMessage(string sMailFromAddr, string sMailFromName, bool isBodyHtml)
        {
            _message = new MailMessage { From = new MailAddress(sMailFromAddr, sMailFromName), IsBodyHtml = isBodyHtml };
        }

        public virtual bool isValidMessage()
        {
            // Is this message valid?
            // allow override of this
            if (string.IsNullOrWhiteSpace(Subject) && string.IsNullOrWhiteSpace(Body))
                return false;
            if (_message.To.Count <= 0)     // not sent to any addr?
                return false;
            return true;
        }

        public MailMessage GetMailMessage()
        {
            return _message;
        }

        public string Subject
        {
            get { return _message.Subject; }
            set { _message.Subject = value; }
        }
        public string Body  // This might be HTML or not depending on previous settings.
        {
            get { return _message.Body; }
            set { _message.Body = value; }
        }

        public void AddMailTo(MailAddress a)
        {
            // Don't allow dupes.
            if (a == null)
                return;
            foreach (var x in _message.To)
            {
                if (String.Compare(x.Address, a.Address, StringComparison.OrdinalIgnoreCase) == 0)
                    return; // already here.
            }
            _message.To.Add(a);
        }
        public void AddMailTo(string sMailToX)
        {
            // can be list of addresses separated by ; new MailAddress(ToEmail, ToName)
            // NOTE: May throw System.FormatException {"The specified string is not in the form required for an e-mail address."}
            // NOTE: "Tom Smith <tsmith@contoso.com>" is also valid format.
            if (string.IsNullOrWhiteSpace(sMailToX))
                return;

            string[] aMailTo = sMailToX.Split(';');
            foreach (string sMailTo1 in aMailTo)
            {
                AddMailTo(EmailAddress.GetMailAddress(sMailTo1));
            }
        }
        public void AddMailTo(List<string> ToList)
        {
            foreach (string item in ToList)
            { AddMailTo(item); }
        }

        public void AddBCC(MailAddress a)
        {
            if (a == null)
                return;
            foreach (var x in _message.Bcc)
            {
                if (String.Compare(x.Address, a.Address, StringComparison.OrdinalIgnoreCase) == 0)
                    return; // already here.
            }
            _message.Bcc.Add(a);
        }

        public void AddBCC(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return;
            string[] aMailTo = s.Split(';');
            foreach (string sMailTo in aMailTo)
            {
                AddBCC(EmailAddress.GetMailAddress(sMailTo));
            }
        }

        public void AddAttachment(Attachment a)
        {
            if (a == null)
                return;
            _message.Attachments.Add(a);
        }

        //public static string ToHtml(string s)
        //{
        // System.Web
        // If the message body is html we need to convert plain text to html format.
        //  return WebUtility.HtmlEncode(s).Replace("\r\n", "<br />");
        //}
    }
}
