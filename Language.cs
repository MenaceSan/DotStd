using System;
using System.ComponentModel;

namespace DotStd
{
    [Serializable()]
    public enum LanguageId
    {
        // Id for Languages/Cultures that we care about. <html lang="en"> <html lang="en-US">
        // from Language db table ? CultureInfo Code
        // https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes
        // similar to the Windows concept of culture.
        // https://developers.google.com/admin-sdk/directory/v1/languages
        // https://en.wikipedia.org/wiki/Language_localisation
        // https://en.wikipedia.org/wiki/Languages_used_on_the_Internet

        [Description("English")]    // English
        en = 1,        // Default American English. .NET LanguageId =  1033 = 0x409 = 'en-US'

        [Description("Russian")]    //  
        ru = 2,

        [Description("German")]     //  
        de = 3,

        [Description("Spanish")]    // Spanish, Espana
        es = 4,

        [Description("French")]     // French
        fr = 5,

        [Description("Japanese")]     //  
        ja = 6,

        [Description("Portuguese")]     // por
        pt = 7,

        [Description("Italian")]     //  
        it = 8,

        [Description("Persian")]     // farsi
        fa = 9,

        [Description("Polish")]     // pol
        pl = 10,

        [Description("Chinese")]     // zho
        zh = 11,

        [Description("Dutch")]     // nld
        nl = 12,

        [Description("Turkish")]     // tur
        tr = 13,

        [Description("Czech")]     //  ces
        cs = 14,

        [Description("Korean")]     //  kor
        ko = 15,

        // fr_CA = "French Canadian", 
        // en_CA = "English_Canadian"
        // en_AU = "English_Australian"

    }

    public static class Language
    {
        public const string kDefault = "en";   // source = from English ISO_639
        public const string kCultureDef = "en-US"; // default English culture

        public static LanguageId GetLanguageId(string lang)
        {
            // get LanguageId from string. forgiving.

            if (lang == null)
                return LanguageId.en;
            switch (lang.ToLower())
            {
                case "spanish":
                case "es":
                case "2":
                    return LanguageId.es;
  
                case "french":
                case "fr":
                case "4":
                    return LanguageId.fr;

                case "english":
                case "eng":
                case "en":
                case "1":
                default:
                    return LanguageId.en;
            }
        }
    }
}
