using System;
using System.ComponentModel;

namespace DotStd
{
    [Serializable()]
    public enum LanguageId
    {
        // Id for Languages-Cultures that we care about. <html lang="en"> <html lang="en-US">
        // from Language db table ? CultureInfo Code.
        // Description = Native Name and font (English Name)
        // https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes = 2 letter codes.
        // similar to the Windows concept of CultureInfo.
        // https://developers.google.com/admin-sdk/directory/v1/languages
        // https://en.wikipedia.org/wiki/Language_localisation
        // https://en.wikipedia.org/wiki/Languages_used_on_the_Internet

        proper = 0,     // non-translatable proper name.

        [Description("Default")]    // default langauge.
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

        [Description("Português (Portuguese)")]     // por https://en.wikipedia.org/wiki/Portuguese_language
        pt = 9,

        [Description("Italiano (Italian)")]     //   https://en.wikipedia.org/wiki/Italian_language
        it = 10,

        [Description("فارسی (Persian/Farsi)")]     // farsi https://en.wikipedia.org/wiki/Persian_language
        fa = 11,

        [Description("Polski (Polish)")]     // pol  https://en.wikipedia.org/wiki/Polish_language
        pl = 12,

        [Description("汉语/中文 (Chinese)")]     // zho https://en.wikipedia.org/wiki/Chinese_language ???
        zh = 13,

        [Description("Nederlands (Dutch)")]     // nld https://en.wikipedia.org/wiki/Dutch_language
        nl = 14,

        [Description("Türkçe (Turkish)")]     // tur https://en.wikipedia.org/wiki/Turkish_language
        tr = 15,

        [Description("Čeština (Czech)")]     //  ces https://en.wikipedia.org/wiki/Czech_language
        cs = 16,

        [Description("한국어/韓國語 (Korean)")]     //  kor https://en.wikipedia.org/wiki/Korean_language
        ko = 17,

        // fr_CA = "French Canadian", 
        // en_CA = "English_Canadian"
        // en_AU = "English_Australian"

        [Description("Test Language (Test Accents)")]     // for testing auto translation.
        test = 100,     // https://en.wikipedia.org/wiki/Constructed_language
    }

    public class Language
    {
        // CultureInfo

        public const string kDefault = "en";   // source = from English ISO_639
        public const string kCultureDef = "en-US"; // default English culture

        public LanguageId Id;  // int = Popularity rank. name from ISO 639-1 AKA TwoLetterISOLanguageName

        public string Name;     // AKA EnglishName

        public string NativeName;       // Native speakers term for it.

        public string URL;     // Wikipedia page.

        public string TwoLetterISOLanguageName => Id.ToString();

        public string GetDescription()
        {
            if (String.IsNullOrWhiteSpace(NativeName))
                return Name;
            return string.Concat(NativeName, " (", Name, ")");
        }

        public static LanguageId GetId(string lang, int level = 3)
        {
            // get LanguageId from string. forgiving.
            // level = how close does the match need to be ?

            if (string.IsNullOrWhiteSpace(lang))
                return LanguageId.native;

            lang = lang.ToLower();

            Array enumValues = Enum.GetValues(typeof(LanguageId));
            foreach (LanguageId value in enumValues)
            {
                if (value.ToString() == lang)   // code.
                    return value;
                if (level <= 1)
                    continue;
                if (lang == ((int)value).ToString())    // number
                    return value;
                if (level <= 2)
                    continue;
                string desc = value.ToDescription().ToLower();  // part of full text.
                if (desc.Contains(lang))
                    return value;
            }

            return LanguageId.native;
        }

        public static LanguageId GetAcceptLang(string acceptLang)
        {
            // Get the best value from the "Accept-Language" format string.
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

        public static System.Globalization.CultureInfo GetCulture(LanguageId id)
        {
            // Get equiv .NET CultureInfo

            return null;
        }
    }
}
