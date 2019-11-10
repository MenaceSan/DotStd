using System.Text;

namespace DotStd
{

    public class PhoneNumber
    {
        // Manage string phone numbers . encode/decode formatting.
        // Similar to Google phone number parser. PhoneNumbers. libphonenumber-csharp

        // https://www.twilio.com/blog/validating-phone-numbers-effectively-with-c-and-the-net-frameworks
        // https://www.twilio.com/docs/glossary/what-e164
        // https://en.wikipedia.org/wiki/National_conventions_for_writing_telephone_numbers

        public const int kCodeUSA = 1;  // CallingCode
        public const ulong kMax = 10000000000ul;
        public const ulong kMin = 999999ul;

        public int CountryCode { get; set; } = kCodeUSA;   // We get the country part of the Phone number. 1 = USA. NOT the same as CountryId. AKA CallingCode
        public ulong NationalNumber { get; set; }   // Get number without country code. (NOT in PhoneNumberFormat.E164 format)
        public string Extension { get; set; }       // optional extension.

        public bool IsValidPhone
        {
            get
            {
                // can i use this as a valid phone number?
                if (CountryCode <= 0)
                    return false;
                if (NationalNumber == 0 || NationalNumber <= kMin ||  NationalNumber >= kMax)
                    return false;
                return true;
            }
        }

        public PhoneNumber()
        {
        }
        public PhoneNumber(ulong nationalNumber, int countryCode = kCodeUSA, string extension = null)
        {
            CountryCode = countryCode;
            NationalNumber = nationalNumber;
            Extension = extension;
        }

        readonly string _seps = " ()-."; // only valid separators.

        public bool Parse(string phone, bool isOptional = false, int countryCodeDef = kCodeUSA)
        {
            // Must have 10 or more digits.
            // isOptional = empty string is ok.
            // RETURN: true = ok.
            // EX: formats that are valid.
            // ""

            CountryCode = countryCodeDef;
            NationalNumber = 0;
            Extension = null;

            if (string.IsNullOrWhiteSpace(phone))
            {
                return isOptional;
            }

            phone = phone.Trim();

            int i = 0;
            bool isCC = false;
            if (phone[0] == '+')    // starts with a country code.
            {
                isCC = true;
                i = 1;
            }

            for (; i < phone.Length; i++)
            {
                char ch = phone[i];
                if (char.IsDigit(ch))
                {
                    NationalNumber *= 10;
                    NationalNumber += (ulong)(ch - '0');
                    if (NationalNumber >= kMax)
                        return false;
                    continue;
                }

                if (ch == '+')  // never valid to have another country code.
                    return false;

                if (ch == 'x' || ch == ',')     // extension ?
                {
                    if (phone.Length <= i + 1)
                        return false;
                    Extension = phone.Substring(i + 1);
                    break;
                }

                if (_seps.IndexOf(ch) < 0)  // valid separator chars only.
                    return false;

                if (isCC)   // done with country code.
                {
                    CountryCode = (int)NationalNumber;
                    NationalNumber = 0;
                    if (CountryCode == 0)
                        return false;
                    isCC = false;
                }
            }

            if (isCC)   // must not end with the country code !
                return false;

            return IsValidPhone;
        }

        public string GetUS()
        {
            // format 10 digit string for US style display. US format = "123-456-7890"
            // No country code prefix.

            ulong v0 = (this.NationalNumber / 10000000ul) % 1000;
            ulong v1 = (this.NationalNumber / 10000ul) % 1000;
            ulong v2 = (this.NationalNumber) % 10000;

            return string.Format("{0:D3}-{1:D3}-{2:D4}", v0, v1, v2);
        }

        public string GetE164(bool fancy = true)
        {
            // get default string format.
            // Get number in E164 string format. ex. "+1 617-346-8556"

            string s;
            if (this.CountryCode == kCodeUSA && fancy)
            {
                s = "+1 " + GetUS();
            }
            else
            {
                s = string.Format("+{0} {1:D10}", this.CountryCode, this.NationalNumber);
            }

            if (!string.IsNullOrEmpty(Extension))
            {
                s += "x" + Extension;
            }
            return s;
        }

        public override string ToString()
        {
            return GetE164(true);
        }
    }
}
