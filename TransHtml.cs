using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    public static class TransHtml
    {
        // Prepare HTML for translation.
        // Remove untranslatable parts of <HTML> to be re-injected after translation.

        public const string kAttrX = "tranx";      // DO NOT translate some internal part. eXclude

        public static string[] GetTranX(ref string text)
        {
            // remove stuff to be excluded from translation.
            // return null = no substitution required. else the list of stuff to be string.Format() back into the string.

            return null;
        }
    }
}
