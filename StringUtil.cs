﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotStd
{
    /// <summary>
    /// String and char util functions.
    /// </summary>
    public static class StringUtil
    {
        public const string _NoErrorMsg = "";  // "" = Success = no error. null = did nothing.

        // ToByteArray = byte[] = System.Text.Encoding.Default.GetBytes(sIn);

        public static int CompareNoCase(this string s1, string s2)
        {
            // simple Wrapper 
            return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// return one string or the other. https://en.wikipedia.org/wiki/IIf
        /// </summary>
        /// <param name="expr"></param>
        /// <param name="ifTrue"></param>
        /// <param name="ifFalse"></param>
        /// <returns></returns>
        public static string IIf(bool expr, string ifTrue, string ifFalse = "")
        {
            if (expr)
                return ifTrue;
            return ifFalse;
        }

        /// <summary>
        /// is this char a basic number? like Regex regexDigit = new Regex("[^0-9]");
        /// NOT extended ASCII, 1/2 etc.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsDigit1(char ch)
        {
#if false   // false true 
            return ch >= '0' && ch <= '9';
#else
            return char.IsDigit(ch);   // NOT char.IsNumber
#endif
        }

        /// <summary>
        /// is this char basic upper case? like new Regex("[^A-Z]");
        /// NOT extended ASCII. Latin only.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsUpper1(char ch)
        {
#if true   // false true 
            return (ch >= 'A' && ch <= 'Z');
#else
            return char.IsUpper(ch);
#endif
        }

        /// <summary>
        /// is this char basic lower case? like new Regex("[^a-z]");
        /// NOT extended ASCII. Latin only.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsLower1(char ch)
        {
#if true   // false true 
            return (ch >= 'a' && ch <= 'z');
#else
            return char.IsLower(ch);
#endif
        }

        /// <summary>
        /// Is Alpha?
        /// NOT extended ASCII. Latin only.
        /// new Regex("[^a-zA-Z]");
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsAlpha1(char ch)
        {
#if true   // false true 
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
#else
            return char.IsLetter(ch);
#endif
        }

        public static bool IsAlphaNumeric1(char ch)
        {
            // NOT extended ASCII. Latin only.
            // Regex("[^a-zA-Z0-9]")
#if true   // false true 
            return IsAlpha1(ch) || IsDigit1(ch);
#else
            return char.IsLetterOrDigit(ch);
#endif
        }

        public const string kVowels = "aeiouAEIOU";

        public static bool IsVowel(char ch)
        {
            return kVowels.Contains(ch);
        }

        public static bool HasUpperCase([NotNullWhen(true)] string? str)
        {
            // IsUpper for extended ASCII 
            return !string.IsNullOrEmpty(str) && str.Any(c => char.IsUpper(c));
        }
        public static bool HasLowerCase([NotNullWhen(true)] string? str)
        {
            // IsLower for extended ASCII 
            return !string.IsNullOrEmpty(str) && str.Any(c => char.IsLower(c));
        }

        public static bool HasNumber([NotNullWhen(true)] string? str)
        {
            // allow extended IsNumber for 1/2 etc
            return !string.IsNullOrEmpty(str) && str.Any(c => char.IsNumber(c));
        }

        public static bool IsNumeric1([NotNullWhen(true)] string? str)
        {
            // Does this string contain a simple/strict integer number? no spaces, -+ or . 
            // NOT extended IsNumber 1/2
            if (string.IsNullOrWhiteSpace(str))
                return false;
            return str.All(c => IsDigit1(c));
        }

        static readonly Lazy<Regex> _regexNum2 = new(() => new Regex(@"^\s*\-?\d+(\.\d+)?\s*$"));
        public static bool IsNumeric2([NotNullWhen(true)] string? str)
        {
            // Far more forgiving IsNumeric(). allow leading spaces. points. signs.
            // NOT extended IsNumber 1/2. No decimal comma for European?
            if (string.IsNullOrWhiteSpace(str))
                return false;
            return _regexNum2.Value.IsMatch(str);
        }

        public static bool IsAlphaNumeric1([NotNullWhen(true)] string? str)
        {
            // _regexAlNum = new Regex("[^a-zA-Z0-9]");
            // NOT extended ASCII. Latin only.

            if (string.IsNullOrWhiteSpace(str))
                return false;
            return str.All(c => IsAlphaNumeric1(c));
        }

        [return: NotNullIfNotNull("sValue")]
        public static string? GetNumericOnly(string? sValue, bool bStopOnNonNumeric = false)
        {
            // filter out all non numeric chars. For telephone numbers?
            // NOT extended ASCII. Latin only.
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
                if (IsDigit1(ch))
                {
                    sb.Append(ch);
                }
                else if (bStopOnNonNumeric)
                    break;
            }
            return sb.ToString();
        }

        public static string GetAlphaNumericOnly(string? sValue)
        {
            // filter out all non alpha numeric chars.
            // NOT extended ASCII. Latin only.
            // For comparing DLNum etc.
            // System.Text.RegularExpressions.Regex.Replace(sDL, "[^A-Za-z0-9]", "")
            // AKA ToAlNum

            if (string.IsNullOrWhiteSpace(sValue))
                return "";
            return System.Text.RegularExpressions.Regex.Replace(sValue, "[^A-Za-z0-9]", "");
        }

        public static bool IsWildcardMatch([NotNullWhen(true)] string? wildcardPattern, string subject)
        {
            // Simple wildcard match
            // https://www.hiimray.co.uk/2020/04/18/implementing-simple-wildcard-string-matching-using-regular-expressions/474
            if (string.IsNullOrWhiteSpace(wildcardPattern))
            {
                return false;
            }

            string newWildcardPattern = wildcardPattern.Replace("*", "");
            int wildcardCount = wildcardPattern.Length - newWildcardPattern.Length;
            if (wildcardCount <= 0)
            {
                return subject.Equals(wildcardPattern, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (wildcardCount == 1)
            {
                if (wildcardPattern.StartsWith("*"))
                {
                    return subject.EndsWith(newWildcardPattern, StringComparison.CurrentCultureIgnoreCase);
                }
                else if (wildcardPattern.EndsWith("*"))
                {
                    return subject.StartsWith(newWildcardPattern, StringComparison.CurrentCultureIgnoreCase);
                }
            }

            string regexPattern = string.Concat("^", Regex.Escape(wildcardPattern).Replace("\\*", ".*"), "$");
            try
            {
                return Regex.IsMatch(subject, regexPattern);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// trim a string for whitespace but ignore null.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull("s")]
        public static string? TrimN(string? s)
        {
            if (s == null)
                return s;
            return s.Trim();
        }

        public static int CompareSub(string s, int i, string sub, int len)
        {
            for (int j = 0; j < len; j++)
            {
                char ch1 = s[i + j];
                char ch2 = sub[j];
                if (ch1 != ch2)
                {
                    return ch1 - ch2;
                }
            }
            return 0;
        }

        public static int CountSub(string s, string sub)
        {
            // count occurrences of sub in s.
            int lenSub = sub.Length;
            if (lenSub <= 0)
                return 0;
            int count = 0;
            int len = s.Length - lenSub;
            for (int i = 0; i <= len; i++)
            {
                if (CompareSub(s, i, sub, lenSub) == 0)
                    count++;
            }
            return count;
        }

        /// Get a substring but never throw.
        [return: NotNullIfNotNull("s")]
        public static string? SubSafe(string? s, int i, int lenTake = short.MaxValue)
        {
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

        [return: NotNullIfNotNull("s")]
        public static string? Truncate(string? s, int size)
        {
            // Left len chars.
            // Take X chars and lose the rest. No padding.
            // AKA Left() in Strings (VB)
            ValidState.ThrowIfNegative(size, nameof(size));
            if (s == null)
                return null;
            if (s.Length > size)
                return s.Substring(0, size);
            return s;   // no truncate or padding.
        }

        [return: NotNullIfNotNull("s")]
        public static string? Ellipsis(this string? s, int lenMax = 0x400)
        {
            // Truncate string with ellipsis.

            ValidState.ThrowIfNegative(lenMax, nameof(lenMax));
            if (s == null)
                return null;
            if (s.Length > lenMax)
                return s.Substring(0, lenMax) + "...";
            return s;
        }

        [return: NotNullIfNotNull("s")]
        public static string? TruncateRight(this string? s, int size)
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
    }
}
