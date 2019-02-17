using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DotStd
{
    public class EmailAddress : IValidatable<string>
    {
        // helper to support System.Net.Mail.MailAddress
        // parse the email to get First and Last Names, display names.

        public const int kMaxLen = 128;   // there are no valid email addresses > kMaxLen

        public static bool IsEmailAddress(string email)
        {
            // http://emailregex.com/
            // General format seems like an email address ? renamed from IsValidEmail()
            // ASSUME will not throw if passed to System.Net.Mail.MailAddress(email)
            // Sample valid emails:
            //  jim+somecoolshop22@example.com

            if (string.IsNullOrWhiteSpace(email) || email.Length > kMaxLen)
                return false;

            // "^[_a-z0-9-]+(.[a-z0-9-]+)@[a-z0-9-]+(.[a-z0-9-]+)*(.[a-z]{2,4})$"
            // "^[a-zA-Z0-9.!#$%&’*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$"
            // @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"
            // @"^[a-zA-Z0-9_\-\.]+@[a-zA-Z0-9_\-\.]+\.[a-zA-Z]{2,}$" is too simple.
            return Regex.IsMatch(email, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
        }

        public static bool IsValidEmail(string email)
        {
            // like IsEmailAddress but more accurate.

            if (!IsEmailAddress(email)) // simple reject ?
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;   // throw on fail.
            }
        }

        public bool IsValid(string email)
        {
            return IsValidEmail(email);
        }

        public static MailAddress GetMailAddress(string addr1, string sDisplayNameDefault = null)
        {
            // Create MailAddress with name parsed correctly.

            // Allow email formats:
            //  Nellis, Joanne <JNellis@co.dutchess.ny.us>
            //  Nellis, Joanne [JNellis@co.dutchess.ny.us]
            //  JNellis@co.dutchess.ny.us
            //  JNellis@co.dutchess.ny.us, Nellis, Joanne
            //  JNellis@co.dutchess.ny.us, Joanne Nellis 

            string addr2 = addr1.Trim();
            addr2 = addr2.Replace("mailto:", ""); // remove 'mailto' if present.

            if (addr2.IndexOf("[") > 0 && addr2.IndexOf("]") > 0)
            {
                // Allow it to be formatted non-standard like "Nellis, Joanne [JNellis@co.dutchess.ny.us]". convert to legal format using <>.
                addr2 = addr2.Replace("[", "<");
                addr2 = addr2.Replace("]", ">");
                return new MailAddress(addr2);
            }
            if (addr2.IndexOf("<") > 0 && addr2.IndexOf(">") > 0)
            {
                return new MailAddress(addr2);
            }
            else if (addr2.IndexOf(',') > 0)
            {
                // Allow separate as "email,displayname"
                string[] aMailParts = addr2.Split(',');
                return new MailAddress(aMailParts[0], aMailParts[1]);
            }
            else if (!string.IsNullOrWhiteSpace(sDisplayNameDefault))
            {
                // Use default display name.
                return new MailAddress(addr2, sDisplayNameDefault);
            }
            else
            {
                return new MailAddress(addr2);
            }
        }
    }

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
            if (String.IsNullOrWhiteSpace(Subject) && String.IsNullOrWhiteSpace(Body))
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
        //  return HttpUtility.HtmlEncode(s).Replace("\r\n", "<br />");
        //}
    }
}
