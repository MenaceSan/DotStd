using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotStd
{
    public static class StringUtil
    {
        // String and char util functions.

        public static string IIf(bool b, string s, string sDef = "")
        {
            // https://en.wikipedia.org/wiki/IIf
            if (b)
                return s;
            return sDef;
        }

        public static bool IsDigit(char ch)
        {
            // is this char a number? 
            // Regex regexDigit = new Regex("[^0-9]");
#if false   // false true 
            return ch >= '0' && ch <= '9';
#else
            return char.IsDigit(ch);   // NOT char.IsNumber
#endif
        }
        public static bool IsUpper(char ch)
        {
            // new Regex("[^A-Z]");
#if true   // false true 
            return (ch >= 'A' && ch <= 'Z');
#else
            return char.IsUpper(ch);
#endif
        }
        public static bool IsLower(char ch)
        {
            // new Regex("[^a-z]");
#if true   // false true 
            return (ch >= 'a' && ch <= 'z');
#else
            return char.IsLower(ch);
#endif
        }

        public static bool IsAlpha(char ch)
        {
            // new Regex("[^a-zA-Z]");
#if true   // false true 
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
#else
            return char.IsLetter(ch);
#endif
        }

        public static bool IsAlphaNumeric(char ch)
        {
            // Regex("[^a-zA-Z0-9]")
#if true   // false true 
            return IsAlpha(ch) || IsDigit(ch);
#else
            return char.IsLetterOrDigit(ch);
#endif
        }

        public static bool HasUpperCase(string str)
        {
            return !string.IsNullOrEmpty(str) && str.Any(c => char.IsUpper(c));
        }
        public static bool HasLowerCase(string str)
        {
            return !string.IsNullOrEmpty(str) && str.Any(c => char.IsLower(c));
        }

        public static bool HasNumber(string str)
        {
            return !string.IsNullOrEmpty(str) && str.Any(c => char.IsNumber(c));
        }

        public static bool IsNumeric(string str)
        {
            // Does this string contain a simple integer number? no - or . 
            if (string.IsNullOrWhiteSpace(str))
                return false;
            return str.All(c => IsDigit(c));
        }

        static Regex _regexNum2 = null;
        public static bool IsNumeric2(string str)
        {
            // Far more forgiving IsNumeric(). allow leading spaces. points. signs.
            if (string.IsNullOrWhiteSpace(str))
                return false;
            if (_regexNum2 == null)
            {
                _regexNum2 = new Regex(@"^\s*\-?\d+(\.\d+)?\s*$");
            }
            return !_regexNum2.IsMatch(str);
        }

        public static bool IsAlphaNumeric(string str)
        {
            // _regexAlNum = new Regex("[^a-zA-Z0-9]");
            if (string.IsNullOrWhiteSpace(str))
                return false;
            return str.All(c => IsAlphaNumeric(c));
        }

        public static string GetNumericOnly(string sValue, bool bStopOnNonNumeric = false)
        {
            // filter out all non numeric chars. For telephone numbers?
            // AKA ToNumStr
            // return System.Text.RegularExpressions.Regex.Replace(sValue,"[^\d]", ""); or Regex.Replace(sValue, "[^0-9]", "")
            if (sValue == null)
                return null;
            if (string.IsNullOrWhiteSpace(sValue))
                return "";
            int nLen = sValue.Length;
            var sb = new StringBuilder();
            for (int i = 0; i < nLen; i++)
            {
                char ch = sValue[i];
                if (IsDigit(ch))
                {
                    sb.Append(ch);
                }
                else if (bStopOnNonNumeric)
                    break;
            }
            return sb.ToString();
        }

        public static string GetAlphaNumericOnly(string sValue)
        {
            // filter out all non alpha numeric chars.
            // For comparing DLNum etc.
            // System.Text.RegularExpressions.Regex.Replace(sDL, "[^A-Za-z0-9]", "")
            // AKA ToAlNum

            if (string.IsNullOrWhiteSpace(sValue))
                return "";
            return System.Text.RegularExpressions.Regex.Replace(sValue, "[^A-Za-z0-9]", "");
        }

        public static string TrimN(string s)
        {
            // trim a string for whitespace but ignore null.
            if (s == null)
                return s;
            return s.Trim();
        }

        public static string SubSafe(string s, int i, int lenTake = short.MaxValue)
        {
            // Substring that Will NOT throw.
            if (s == null)
                return null;
            if (i < 0)
            {
                lenTake += i;
                i = 0;
            }
            int lenMax = s.Length;
            if (i >= lenMax || lenTake <= 0)    // take nothing
                return "";
            lenMax -= i;
            if (lenTake > lenMax)
                lenTake = lenMax;
            return s.Substring(i, lenTake);
        }

        public static string Truncate(string s, int size)
        {
            // Left len chars.
            // Take X chars and lose the rest. No padding.
            // AKA Left() in Strings (VB)
            ValidState.ThrowIfNegative(size, nameof(size));
            if (s == null)
                return null;
            else if (s.Length > size)
                return s.Substring(0, size);
            return s;   // no truncate or padding.
        }

        public static string Ellipsis(this string s, int lenMax = 0x400)
        {
            // Truncate string with ellipsis.

            ValidState.ThrowIfNegative(lenMax, nameof(lenMax));
            if (s == null)
                return s;
            if (s.Length > lenMax)
                return s.Substring(0, lenMax) + "...";
            return s;
        }

        public static string TruncateRight(string s, int size)
        {
            // right len chars.
            // Take X chars and lose the rest. No padding.
            // AKA Right() in Strings (VB)
            ValidState.ThrowIfNegative(size, nameof(size));
            if (s == null)
                return null;
            if (s.Length > size)
                return s.Substring(s.Length - size);
            return s; // no truncate or padding.
        }

        public static string FieldLeft(string s, int size, char paddingChar = ' ')
        {
            // Left aligned field that is cropped or padded with spaces to exact size.
            if (s == null)
                return new string(paddingChar, size);
            else if (s.Length > size)
                return s.Substring(0, size);
            else
                return s.PadRight(size, paddingChar);
        }

        public static string FieldRight(string s, int size, char paddingChar = ' ')
        {
            // Right aligned field that is cropped or padded with spaces to exact size.
            if (s == null)
                return new string(paddingChar, size);
            else if (s.Length > size)
                return s.Substring(s.Length - size, size);
            else
                return s.PadLeft(size, paddingChar);    // PadLeft doesn't truncate 
        }

        public static string LeadZero(string s, int size)
        {
            // Right aligned. Add leading zeros
            return FieldRight(s, size, '0');
        }

        public static string ToLower1(string s)
        {
            // Opposite of ToTitleCase()
            // make sure the first letter is lower case char. JavaScript names like this.
            if (s == null || s.Length <= 0)
                return s;
            if (!IsUpper(s[0]))
                return s;
            return char.ToLower(s[0]) + s.Substring(1);
        }
        
        public static string Initials(string nameString)
        {
            var initials = new Regex(@"(\b[a-zA-Z])[a-zA-Z]* ?");
            return initials.Replace(nameString, "$1");
        }
    }
}
