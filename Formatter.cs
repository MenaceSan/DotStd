using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace DotStd
{
    public static class Formatter
    {
        // String formatting for display.
        // Compliments StringUtil
        // TODO: TimeSpan string ??

        public const string kCrLf = "\r\n";     // Windows style. like VB vbCrLf or Environment.NewLine

        // handlebars for email templates.
        public const string kBlockStart = "{{"; // Start of template block. For boiler plate fields in PDF or emails HTML.
        public const string kBlockEnd = "}}";   // End of template field.

        public static string ToOrdinal(int number)
        {
            // AKA GetNth()
            // Convert a number to a ordinal number string. e.g. 1 to "1st", 22 = "22nd"

            string s = number.ToString();

            switch (number % 100)
            {
                case 11:
                case 12:
                case 13:
                    return s + "th";
            }

            switch (number % 10)
            {
                case 1:
                    return s + "st";
                case 2:
                    return s + "nd";
                case 3:
                    return s + "rd";
                default:
                    return s + "th";
            }
        }

        public static string ToTitleCase(string str)
        {
            // capitalize word. CultureInfo cultureInfo
            if (string.IsNullOrWhiteSpace(str))
                return "";
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo ti = cultureInfo.TextInfo;
            return ti.ToTitleCase(str).Trim();
        }

        public static string Join(string separator, params string[] array)
        {
            // separator = ","
            return string.Join(separator, array.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
        public static string JoinTitles(string separator, params string[] array)
        {
            // separator = ","
            return string.Join(separator, array.Where(s => !string.IsNullOrWhiteSpace(s)).Select(x => ToTitleCase(x)));
        }

        public static string FullName(params string[] array)
        {
            // Puts together first,middle,last names separated by spaces. Always capitalize.
            // ignores null, whitespace. 
            return JoinTitles(" ", array);
        }
 
        public static string GetYN(bool yn)
        {
            if (yn) return "Y";
            return "N";
        }

        public static string GetNormalized(string sValue)
        {
            // convert all newlines to proper Environment.NewLine
            // May be similar to String.Normalize(). not sure. TODO find out.
            // http://www.codinghorror.com/blog/2010/01/the-great-newline-schism.html

            var sOut = new System.Text.StringBuilder();
            int nLen = sValue.Length;
            for (int i = 0; i < nLen; i++)
            {
                char ch = sValue[i];
                if (ch == '\r')
                    continue;
                if (ch == '\n')
                {
                    sOut.Append(Environment.NewLine);
                    continue;
                }
                if (ch == ' ' && (nLen - i) > 4 && sValue[i + 1] == ' ' && sValue[i + 2] == ' ' && sValue[i + 3] != ' ')
                {
                    // Stack dumps use 3 spaces to represent line breaks. For some reason.
                    sOut.Append(Environment.NewLine);
                    i += 2;
                    continue;
                }
                sOut.Append(ch);
            }
            return sOut.ToString();
        }

        public static string GetFirstLine(string s, int iLenMax = 128)
        {
            // get the first line of a multi line string.
            int iLen = s.Length;
            if (iLen < iLenMax)
                iLenMax = iLen;
            iLen = s.IndexOf(Environment.NewLine);
            if (iLen > 0 && iLen < iLenMax)
                iLenMax = iLen;
            iLen = s.IndexOf("\n");
            if (iLen > 0 && iLen < iLenMax)
                iLenMax = iLen;
            iLen = s.IndexOf("\r");
            if (iLen > 0 && iLen < iLenMax)
                iLenMax = iLen;
            return s.Substring(0, iLenMax);
        }

        public static string ReplaceToken0(string body, string token, string value)
        {
            // replace an exact token in a boiler plate template email.
            // Name the template file XX.html such that it highlights correctly in VS editor.
            // NOT? ${identifier} or <#XXX#> or  @(XXX)@{XX} (razor)

            if (value == null)
                value = "";
            return body.Replace(token, value);
        }

        public static string ReplaceTokenX(string body, string token, string value)
        {
            // Use handlebar style.
            // Prefer this token style {{x}} for templates. Go/? syntax
            return ReplaceToken0(body, kBlockStart + token + kBlockEnd, value);
        }

        public static string ReplaceTokenX(string body, IPropertyGetter props, string errorStr = null)
        {
            // Replace a set of possible tokens from IPropertyGetter

            var sb = new StringBuilder();
            int i0 = 0;
            int i = 0;
            while (true)
            {
                int b1 = body.IndexOf(kBlockStart, i);
                if (b1 < 0)
                    break;
                i = b1;
                int n1 = b1 + kBlockStart.Length;
                int n2 = body.IndexOf(kBlockEnd, n1);
                if (n2 < 0)
                    continue;
                int b2 = n2 + kBlockEnd.Length;
                i = b2;

                string name = body.Substring(n1, n2 - n1);
                object val = props.GetPropertyValue(name);
                if (val == null)
                {
                    if (errorStr==null) // just leave errors.
                        continue;
                    val = errorStr;   // replace error with something.
                }

                // replace text
                sb.Append(body.Substring(i0, b1 - i0));
                sb.Append((string)val);
                i0 = b2;
            }

            if (i0>0)
            {
                sb.Append(body.Substring(i0));
                return sb.ToString();
            }
            return body;
        }
    }
}
