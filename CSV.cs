using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// Helper to deal with encode and decode of a single line of CSV file.
    /// ex. Email,Fname,Quoted rdeslonde@mydomain.com,Richard,"""This is what I think, suckas!"""
    /// The .NET library contains Microsoft.VisualBasic.FileIO.TextFieldParser which gives some decent though opaque and complex parsing.
    /// What i actually want is just the simple and well defined encode/decode rules for CSV files.
    /// http://stackoverflow.com/questions/10451842/how-to-escape-comma-and-double-quote-at-same-time-for-csv-file.
    /// NOTE: This should be compatible with Excel spreadsheets as well.
    /// rfc4180 = https://tools.ietf.org/html/rfc4180
    /// NOTE: No way to put comments here. Maybe use convention of # or ; ?
    /// </summary>
    public class Csv
    {
        public const string kHeaderBad = "CSV header is not in correct format";

        /// <summary>
        /// List of my columns/fields from DbColumnDef/AppField/app_field. 
        /// The header names all the props for the CVS import/export.
        /// </summary>
        public List<string>? ColumnNames;

        /// <summary>
        /// Are quotes needed for a single value in CSV? 
        /// interior spaces are ok. leading or trailing spaces are not.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static bool IsQuoteNeeded(string? value, char q = '\"')
        {
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
            return false;   // no quotes required
        }

        public static string AddQ(string value, string q1 = "\"")
        {
            return string.Concat(q1, value, q1);   // Quote it.
        }

        /// <summary>
        /// Encode a single value with quotes. double quote = ASCII 34
        /// NOTE: Newlines are left but the reader must be aware to keep reading lines.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        public static string EncodeQ(string value, string q1 = "\"", string q2 = "\"\"")
        {
            // interior quotes become double quotes. NOT slash quotes.
            return AddQ(value.Replace(q1, q2), q1);   // Quote it.
        }

        /// <summary>
        /// Quote intentional leading or ending whitespace for a single value.
        /// This value needs no encoding if it has no commas or quotes.
        /// double quote = ASCII 34
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bAlwaysQuote"></param>
        /// <returns></returns>
        public static string Encode1(string? value, bool bAlwaysQuote = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            if (!bAlwaysQuote)
            {
                if (!IsQuoteNeeded(value))
                    return value;
            }
            return EncodeQ(value);  // Quote it.
        }

        public static string? Encode(IEnumerable<object>? values, bool bAlwaysQuote)
        {
            // Encode a single line of a CSV .
            if (values == null)
                return null;
            var sb = new StringBuilder();
            foreach (object? value in values)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                if (value == null)  // null as ,,
                {
                    continue; //  sb.Append("null");
                }
                sb.Append(Encode1(value.ToString(), bAlwaysQuote));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Encode and Add quotes only if necessary.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string? Encode(params object[]? values)
        {
            return Encode(values, false);
        }

        /// <summary>
        /// Encode and Always quoted
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string? EncodeQ(params object[]? values)
        {
            return Encode(values, true);
        }

        /// <summary>
        /// Decode a single line of a CSV .
        /// Like VB TextFieldParser
        /// Ignore/strip non quoted whitespace at begin/end of values.
        /// </summary>
        /// <param name="sLine"></param>
        /// <returns></returns>
        public static List<string> Decode(string sLine)
        {
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

        public List<string>? DecodeRow(string? sLine)
        {
            if (string.IsNullOrWhiteSpace(sLine))
                return null;
            var vals = Csv.Decode(sLine);   // chop comments at end of lines?
            if (ColumnNames == null)   // get header from first line
            {
                ColumnNames = vals;
                return null;
            }
            return vals;
        }

        /// <summary>
        /// Decode a list using something other than comma delimiter?
        /// </summary>
        /// <param name="sLine"></param>
        /// <param name="delim">comma or other</param>
        /// <returns></returns>
        public static List<string> Decode2(string sLine, char delim)
        {
            // NOTE: This might not deal with interior line breaks correctly ???

            if (delim == ',')
                return Decode(sLine);

            return sLine.Split(delim).ToList();
        }

        /// <summary>
        /// Build a list into a CSV line/string. T = string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="showProperties">has header line</param>
        /// <returns></returns>
        public static string EncodeList<T>(IEnumerable<T> list, bool showProperties = true)
        {
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
                            sb.Append(',');
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
                            sb.Append(',');
                        }
                        PropertyInfo? prop = fromType.GetProperty(propInfos[j].Name);
                        if (prop == null || !prop.CanRead)
                            continue;
                        object? o = prop.GetValue(item, null);
                        if (o != null)
                        {
                            sb.Append(Encode1(o.ToString()));
                        }
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
