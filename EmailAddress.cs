using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace DotStd
{
    /// <summary>
    /// wrapper/helper to support System.Net.Mail.MailAddress
    /// parse the email to get First and Last Names, display names.
    /// </summary>
    public class EmailAddress : IValidatorT<string>
    {
        public const int kMaxLen = 128;   // there are no valid email addresses > kMaxLen

        /// <summary>
        /// Is string in a deneral format that seems like an email address ? renamed from IsValidEmail()
        /// http://emailregex.com/
        /// ASSUME will not throw if passed to System.Net.Mail.MailAddress(email)
        /// Sample valid emails: "jim+somecoolshop22@example.com"
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsEmailAddress([NotNullWhen(true)] string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > kMaxLen)
                return false;

            // "^[_a-z0-9-]+(.[a-z0-9-]+)@[a-z0-9-]+(.[a-z0-9-]+)*(.[a-z]{2,4})$"
            // "^[a-zA-Z0-9.!#$%&’*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$"
            // @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"
            // @"^[a-zA-Z0-9_\-\.]+@[a-zA-Z0-9_\-\.]+\.[a-zA-Z]{2,}$" is too simple.
            return Regex.IsMatch(email, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
        }

        public static string? GetHostName(string? email)
        {
            // Get the hostname part of the (assumed valid) email.
            if (string.IsNullOrWhiteSpace(email))
                return null;
            int i = email.LastIndexOf('@');     // IndexOf ?
            if (i < 0)
                return null;
            return email.Substring(i + 1);
        }

        public static bool IsValidEmail([NotNullWhen(true)] string? email)
        {
            // like IsEmailAddress but more accurate.

            if (!IsEmailAddress(email)) // simple reject ?
                return false;

            try
            {
                // Does this check if the domain hostname resolves in the DNS?
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                // catch throw on fail.
                return false;
            }
        }

        public virtual bool IsValid([NotNullWhen(true)] string? email)
        {
            // virtual IValidatorT<string>
            return IsValidEmail(email);
        }

        public static MailAddress GetMailAddress(string addr1, string? sDisplayNameDefault = null)
        {
            // Create MailAddress object from string with name parsed correctly.

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
}
