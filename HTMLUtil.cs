using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotStd
{
    public static class HTMLUtil
    {
        // Helper functions for formatting HTML.
        // compliment Encode, Decode

        public const string kNBSP = "&nbsp;";   // HTML space

        public const string kCommentOpen = "<!--";
        public const string kCommentClose = "-->";

        public static string GetOptStr(string value, string desc, bool selected = false)
        {
            // for <select>
            // construct HTML "<option value='1'>Main</option>");
            // like TupleKeyValue
            return string.Concat("<option ", selected ? "selected " : string.Empty, "value='", value, "'>", desc, "</option>");
        }

        public static string GetOptStr(int value, string desc, bool selected = false)
        {
            // construct HTML 
            // like TupleIdValue
            return GetOptStr(value.ToString(), desc, selected);
        }
 
        public static string GetOptStr(Enum n, bool selected = false)
        {
            // construct HTML 
            // Get a value from enum to populate a HTML select list option.
            return GetOptStr(n.ToInt(), n.ToDescription(), selected);
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
