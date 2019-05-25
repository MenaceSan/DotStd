using System.Text;

namespace DotStd
{
    public enum PhoneTypeId
    {
        // what sort of phone number is this ?

        Unknown = 0,  // unknown.
        Mobile = 1,       // Avail any time. Personal mobile.
        Home = 2,       // Avail home hours. maybe shared.
        Work = 3,       // Avail work hours. maybe shared.
    }

    public static class PhoneUtil
    {
        // Manage string phone numbers . encode/decode formatting.

        public static bool IsValidPhone(string phone, bool isOptional=false)
        {
            // can i use this as a valid phone number?
            // Must have 10 or more digits.
            phone = RemoveFormatting(phone);
            if (string.IsNullOrEmpty(phone))
                return isOptional;
            return phone.Length >= 10;  // MUST include area code.
        }

        public static string RemoveFormatting(string phone)
        {
            // Strip all formatting for storage. just numbers.
            return StringUtil.GetNumericOnly(phone);
        }

        public static string GetFormatted(string phone)
        {
            // format 10 digit string for display. NOT storage.

            phone = RemoveFormatting(phone);

            var sb = new StringBuilder();
            if ( phone != null && phone.Length > 9)
            {
                for (int i = 0; i < phone.Length; i++)
                {
                    if (i == 3) sb.Append("-");
                    else if (i == 6) sb.Append("-");

                    sb.Append(phone.Substring(i, 1));
                }
            }

            return sb.ToString();
        }
    }
}
