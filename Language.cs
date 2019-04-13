using System;
using System.ComponentModel;

namespace DotStd
{
    [Serializable()]
    public enum LanguageId
    {
        // Id for Languages that we care about.
        // from Language db table ?
        // similar to the Windows concept of culture.
        // AKA LanguageID
        // https://developers.google.com/admin-sdk/directory/v1/languages

        [Description("eng")]    // Shortcode. or 'en'
        English = 1,        // American English. .NET LanguageId =  1033 = 0x409
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
        public const string kDefault = "en";   // source = from English
        public const string kCultureDef = "en-US"; // default english culture

        public static LanguageId GetLanguageId(string lang)
        {
            // get LanguageId from string.

            if (lang == null)
                return LanguageId.English;
            switch (lang.ToLower())
            {
                case "spanish":
                case "sp":
                case "2":
                    return LanguageId.Spanish;
                case "3":
                case "swedish":
                case "swe":
                    return LanguageId.Swedish;
                case "french":
                case "fr":
                case "4":
                    return LanguageId.French;
                //case "english":
                //case "eng":
                //case "en":
                //case "1":
                default:
                    return LanguageId.English;
            }
        }
    }
}
