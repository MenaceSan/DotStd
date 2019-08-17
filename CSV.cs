using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DotStd
{
    public static class CSV
    {
        //! Helper to deal with encode and decode of a single line of CSV file.
        //! ex. Email,Fname,Quoted
        //!  rdeslonde@mydomain.com,Richard,"""This is what I think, suckas!"""
        //! The .NET library contains Microsoft.VisualBasic.FileIO.TextFieldParser which gives some decent though opaque and complex parsing.
        //! What i actually want is just the simple and well defined encode/decode rules for CSV files.
        //! http://stackoverflow.com/questions/10451842/how-to-escape-comma-and-double-quote-at-same-time-for-csv-file.
        //! NOTE: This should be compatible with Excel spreadsheets as well.
        //! rfc4180 = https://tools.ietf.org/html/rfc4180
        //! NOTE: No way to put comments here. Maybe use convention of # or ; ?

        public const string kHeaderBad = "CSV header is not in correct format";

        public static bool IsQuoteNeeded(string value, char q = '\"')
        {
            //! Are quotes needed for a single value?
            //! interior spaces are ok. leading or trailing spaces are not.
            if (string.IsNullOrEmpty(value))
                return false;
            if (char.IsWhiteSpace(value[0]))    // leading whitespace.
                return true;
            if (char.IsWhiteSpace(value[value.Length - 1])) // trailing whitespace
                return true;

            // RFC 4180. 6. Fields containing line breaks (CRLF), double quotes, and commas should be enclosed in double-quotes.

            foreach (char ch in value)
            {
                if (ch == ',' || ch == q)  // special chars.
                    return true;
                if (ch == '\n' || ch == '\r') // cant contain new lines either. 
                    return true;
            }
            return false;
        }

        public static string EncodeQ(string value, string q1 = "\"", string q2 = "\"\"")
        {
            // Encode a single value with quotes.
            // double quote = ASCII 34
            // NOTE: Newlines are left but the reader must be aware to keep reading lines.

            value = value.Replace(q1, q2);    // interior quotes become double quotes. NOT slash quotes.

            return string.Concat(q1, value, q1);   // Quote it.
        }

        public static string Encode1(string value, bool bAlwaysQuote = false)
        {
            // Quote intentional leading or ending whitespace for a single value.
            // This value needs no encoding if it has no commas or quotes.
            // double quote = ASCII 34
            if (!bAlwaysQuote)
            {
                if (!IsQuoteNeeded(value))
                    return value;
            }
            return EncodeQ(value);  // Quote it.
        }

        public static string Encode(IEnumerable<object> values, bool bAlwaysQuote)
        {
            // Encode a single line of a CSV .
            if (values == null)
                return null;
            var sb = new StringBuilder();
            foreach (object value in values)
            {
                if (sb.Length > 0)
                    sb.Append(",");
                if (value == null)
                {
                    continue; //  sb.Append("null");
                }
                sb.Append(Encode1(value.ToString(), bAlwaysQuote));
            }
            return sb.ToString();
        }

        public static string Encode(params object[] values)
        {
            // Add quotes only if necessary.
            return Encode(values, false);
        }
        public static string EncodeQ(params object[] values)
        {
            // Always quoted
            return Encode(values, true);
        }

        public static List<string> Decode(string sLine)
        {
            // Decode a single line of a CSV .
            // Like VB TextFieldParser
            // Ignore/strip non quoted whitespace at begin/end of values.
            if (sLine == null)
                return null;
            var a = new List<string>();
            var sb = new StringBuilder();
            bool bInQuotes = false;
            for (int i = 0; i < sLine.Length; i++)
            {
                char ch = sLine[i];
                if (bInQuotes)
                {
                    if (ch == '\"')
                    {
                        if (i >= sLine.Length - 1 || sLine[i + 1] != '\"')
                        {
                            bInQuotes = false;  // end quotes.
                            continue;
                        }
                        i++;    // skip 1 for double quotes. (interior quotes)
                    }
                }
                else
                {
                    if (ch == '\"')
                    {
                        bInQuotes = true;
                        continue;
                    }
                    if (ch == ',')
                    {
                        a.Add(sb.ToString());
                        sb.Clear();
                        continue;
                    }
                    if (char.IsWhiteSpace(ch))    // skip unquoted whitespace.
                    {
                        continue;
                    }
                }
                sb.Append(ch);
            }

            a.Add(sb.ToString());    // last item 

            return a;   // can use Linq ToArray() to get string[]
        }

        public static List<string> Decode2(string sLine, char delim)
        {
            // NOTE: This might not deal with interior line breaks correctly ???

            if (delim == ',')
                return Decode(sLine);

            return sLine.Split(delim).ToList();
        }

        public static string EncodeList<T>(IEnumerable<T> list, bool showProperties = true)
        {
            // Build a list into a CSV line/string. T = string.
            // ShowProperties = header line

            System.Type fromType = typeof(T);
            PropertyInfo[] propInfos = fromType.GetProperties();
            // ValidState.EnsureTrue(propInfos.Length > 0, "propInfos");  // THIS DOESNT COMPILE ?? WHY ?

            var sb = new StringBuilder();

            // Get the properties/field names for type T for the headers
            if (showProperties)
            {
                if (list != null)
                {
                    for (int i = 0; i < propInfos.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(",");
                        }
                        sb.Append(propInfos[i].Name);
                    }
                }
                sb.AppendLine();
            }

            // Loop through the collection, then the properties and add the values
            if (list != null)
            {
                foreach (T item in list)
                {
                    for (int j = 0; j < propInfos.Length; j++)
                    {
                        if (j > 0)
                        {
                            sb.Append(",");
                        }
                        PropertyInfo prop = fromType.GetProperty(propInfos[j].Name);
                        if (!prop.CanRead)
                            continue;
                        object o = prop.GetValue(item, null);
                        if (o != null)
                        {
                            sb.Append(CSV.Encode1(o.ToString()));
                        }
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

    }
}
