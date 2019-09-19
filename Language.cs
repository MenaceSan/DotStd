using System;
using System.ComponentModel;

namespace DotStd
{
    [Serializable()]
    public enum LanguageId
    {
        // Id for Languages/Cultures that we care about. <html lang="en"> <html lang="en-US">
        // from Language db table ? CultureInfo Code.
        // Description = Native Name and font (English Name)
        // https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes
        // similar to the Windows concept of culture.
        // https://developers.google.com/admin-sdk/directory/v1/languages
        // https://en.wikipedia.org/wiki/Language_localisation
        // https://en.wikipedia.org/wiki/Languages_used_on_the_Internet

        proper = 0,     // non-translatable proper name.

        native = 1,     // The native language of the app, whatever that might be.

        [Description("English")]    // English
        en = 3,        // Default American English. .NET LanguageId =  1033 = 0x409 = 'en-US'

        [Description("Русский язык (Russian)")]    //  https://en.wikipedia.org/wiki/Russian_language
        ru = 4,

        [Description("Deutsch (German)")]     //  https://en.wikipedia.org/wiki/German_language
        de = 5,

        [Description("Español (Spanish)")]    // Spanish, Espana. https://en.wikipedia.org/wiki/Spanish_language
        es = 6,

        [Description("Le Français (French)")]     // French https://en.wikipedia.org/wiki/French_language
        fr = 7,

        [Description("日本語 (Japanese)")]     //   https://en.wikipedia.org/wiki/Japanese_language
        ja = 8,

        [Description("Portuguese")]     // por
        pt = 9,

        [Description("Italian")]     //  
        it = 10,

        [Description("Persian")]     // farsi
        fa = 11,

        [Description("Polish")]     // pol
        pl = 12,

        [Description("Chinese")]     // zho
        zh = 13,

        [Description("Dutch")]     // nld
        nl = 14,

        [Description("Turkish")]     // tur
        tr = 15,

        [Description("Czech")]     //  ces
        cs = 16,

        [Description("Korean")]     //  kor
        ko = 17,

        // fr_CA = "French Canadian", 
        // en_CA = "English_Canadian"
        // en_AU = "English_Australian"

        [Description("Test Language (Test Accents)")]     // for testing auto translation.
        test = 100,     // https://en.wikipedia.org/wiki/Constructed_language
    }

    public static class Language
    {
        public const string kDefault = "en";   // source = from English ISO_639
        public const string kCultureDef = "en-US"; // default English culture

        public static LanguageId GetId(string lang, int level)
        {
            // get LanguageId from string. forgiving.

            if (string.IsNullOrWhiteSpace(lang))
                return LanguageId.native;

            lang = lang.ToLower();

            Array enumValues = Enum.GetValues(typeof(LanguageId));
            foreach (LanguageId value in enumValues)
            {
                if (value.ToString() == lang)
                    return value;
                if (level <= 1)
                    continue;
                if (lang == ((int)value).ToString())
                    return value;
                if (level <= 2)
                    continue;
                string desc = value.ToDescription().ToLower();
                if ( desc.Contains(lang))
                    return value;
            }

            return LanguageId.native;
        }

        public static LanguageId GetAcceptLang(string acceptLang)
        {
            // Get the best value from the "Accept-Langauge" format string.
            // HTTP Accept-Language tag e.g. "en-US,en;q=0.9"

            if (string.IsNullOrWhiteSpace(acceptLang))
                return LanguageId.native;

            LanguageId langId;
            string[] langs = acceptLang.Split(',');
            foreach (var lang1 in langs)
            {
                string[] langA = acceptLang.Split(';');
                if (langA != null && langA.Length > 0)
                {
                    langId = GetId(langA[0], 1);
                    if (langId != LanguageId.native)
                        return langId;
                }

                langA = acceptLang.Split('-');
                if (langA != null && langA.Length > 0)
                {
                    langId = GetId(langA[0], 1);
                    if (langId != LanguageId.native)
                        return langId;
                }
            }

            return LanguageId.native;
        }

    }
}
