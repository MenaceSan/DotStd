using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// Helper functions for formatting HTML.
    /// compliment Encode, Decode
    /// In some ways HTML can be treated like XML except for some exceptions. (some non closed tags, literals, some encoding)
    /// Use WebUtility.HtmlEncode() to encode a string to proper HTML. 
    /// NOTE: FormUrlEncodedContent is for URL args. NOT the same.
    /// NOTE: Use select2 for option lists with icons.
    /// </summary>
    public static class HTMLUtil
    {
        public const string kNBSP = "&nbsp;";   // HTML non breaking space
        public const string kGT = "&gt;";   //  
        public const string kLT = "&lt;";   // 

        public const string kCommentOpen = "<!--";
        public const string kCommentClose = "-->";

        // List of known/common entities. https://www.w3schools.com/html/html_entities.asp
        public static readonly Dictionary<string, char> kEntities = new Dictionary<string, char>
        {
            {kNBSP, ' '},
            {"&copy;", '©'},
            {"&reg;", '®'},
            {"&eacute;", 'é'},
            {"&trade;", '™'},
            {"&amp;", '&'},
            {kGT, '>'},
            {kLT, '<'},
            {"&quot;", '"'},
            {"&apos;", '\''},
        };

        public static char GetEntityChar(string entity)
        {
            // TODO handle "&#160;" ??

            char value;
            if (kEntities.TryGetValue(entity, out value))
            {
                return value;
            }
            return '\0';
        }

        public static string? GetEntityName(string src, int startIndex)
        {
            // assume any entity name starts with & and ends with ;
            if (src[startIndex] != '&')
                return null;
            int j = src.IndexOf(';', startIndex + 1);
            if (j <= startIndex + 1)
                return null;
            int length = (j - startIndex) + 1;
            if (length > 10)
                return null;
            return src.Substring(startIndex, length);
        }

        public static string DecodeEntities2(string src)
        {
            // replace Non standard entities with chars. HTML is not the same as XML.
            // Replacing "&nbsp;" with "&#160;" since "&nbsp;" is not XML standard
            // NOTE: XmlReader is vulnerable to attacks from entity creation. XSS. OK in .Net 4.0+ but use DtdProcessing.Prohibit !!

            for (int i = 0; i < src.Length; i++)
            {
                string? entityName = GetEntityName(src, i);
                if (entityName == null)
                    continue;
                char ch = GetEntityChar(entityName);
                if (ch == '\0')
                    continue;
                src = string.Concat(src.Substring(0, i), ch, src.Substring(i + entityName.Length));
            }

            return src;
        }

        public const string kSelected = "selected ";

        public static string GetOpt(string value, string? desc, bool selected = false, string? extra = null)
        {
            // construct HTML option for <select>
            // construct HTML "<option value='1'>Main</option>");
            // like TupleKeyValue

            if (selected)
            {
                extra = string.Concat(kSelected, extra);
            }

            return string.Concat("<option ", extra, "value='", value, "'>", desc ?? value, "</option>");
        }

        public static string GetOpt(int value, string? desc, bool selected = false)
        {
            // construct HTML option for <select> 
            return GetOpt(value.ToString(), desc, selected);
        }

        public static string GetOpt(TupleIdValue n, bool selected = false)
        {
            // construct HTML option for <select>
            return GetOpt(n.Id, n.Value, selected);
        }

        public static string GetOpt(Enum n, bool selected = false)
        {
            // construct HTML option for <select>
            // Get a value from enum to populate a HTML select list option.
            return GetOpt(n.ToInt(), n.ToDescription(), selected);
        }

        public static bool IsCommentLine(string s)
        {
            s = s.Trim();
            return s.StartsWith(kCommentOpen) && s.EndsWith(kCommentClose);
        }

        public static string GetList(IEnumerable<string> a)
        {
            if (!a.Any())
                return "";
            var sb = new StringBuilder();
            sb.Append("<ul>");
            foreach (var s in a)
            {
                sb.Append("<li>");
                sb.Append(s);
                sb.Append("</li>");
            }
            sb.Append("</ul>");
            return sb.ToString();
        }
    }
}
