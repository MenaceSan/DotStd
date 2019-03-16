using System;
using System.ComponentModel;

namespace DotStd
{
    [Serializable()]
    public enum LanguageID
    {
        // from the base.dbo.Language table
        // similar to the Windows concept of culture.
        // Languages that we care about.

        [Description("eng")]    // Shortcode. or 'en'
        English = 1,        // American english. .NET LanguageID =  1033
        [Description("sp")]
        Spanish = 2,
        [Description("swe")]    // Shortcode
        Swedish = 3,
        [Description("fr")]
        French = 4,

        // French_Canadian
        // German
        // English_Canadian
        // English_Australian
    }

    public static class Language
    {
        public const string kCultureDef = "en-US"; // default english culture

        public static LanguageID GetLanguageID(string sShortCode)
        {
            // GetLanguageIDByShortCode(ByVal sLanguageShortCode As String) As DotLib.LanguageID
            if (sShortCode == null)
                return LanguageID.English;
            switch (sShortCode.ToLower())
            {
                case "spanish":
                case "sp":
                case "2":
                    return LanguageID.Spanish;
                case "3":
                case "swedish":
                case "swe":
                    return LanguageID.Swedish;
                case "french":
                case "fr":
                case "4":
                    return LanguageID.French;
                //case "english":
                //case "eng":
                //case "en":
                //case "1":
                default:
                    return LanguageID.English;
            }
        }
    }
}
