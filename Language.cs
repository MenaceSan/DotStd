using System;
using System.ComponentModel;

namespace DotStd
{
    /// <summary>
    /// Id for Languages-Cultures that we most care about. '<html lang="en"> <html lang="en-US">'
    /// from Language db table ? CultureInfo Code.
    /// Description = Native Name and font (English Name)
    /// similar to the Windows concept of CultureInfo.
    /// https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes = 2 letter codes.
    /// </summary>
    [Serializable()]
    public enum LanguageId
    {
        // https://developers.google.com/admin-sdk/directory/v1/languages
        // https://en.wikipedia.org/wiki/Language_localisation
        // https://en.wikipedia.org/wiki/Languages_used_on_the_Internet  // sort by popularity.

        proper = 0,     // non-translatable proper name.

        [Description("Default")]    // default language.
        native = 1,     // The native/neutral language of the app, whatever that might be. probably English.

        [Description("English")]    // English 
        en = 3,        // Default American English. .NET LanguageId =  1033 = 0x409 = 'en-US' // deployed with ASP

        [Description("Русский язык (Russian)")]    //  https://en.wikipedia.org/wiki/Russian_language
        ru = 4,     // deployed with ASP

        [Description("Deutsch (German)")]     //  https://en.wikipedia.org/wiki/German_language
        de = 5,     // deployed with ASP

        [Description("Español (Spanish)")]    // Spanish, Espana. https://en.wikipedia.org/wiki/Spanish_language
        es = 6,     // deployed with ASP

        [Description("Le Français (French)")]     // French https://en.wikipedia.org/wiki/French_language
        fr = 7,     // deployed with ASP

        [Description("日本語 (Japanese)")]     //   https://en.wikipedia.org/wiki/Japanese_language
        ja = 8,     // deployed with ASP

        [Description("Português (Portuguese)")]     // por https://en.wikipedia.org/wiki/Portuguese_language
        pt = 9,     // pt-BR deployed with ASP

        [Description("Italiano (Italian)")]     //   https://en.wikipedia.org/wiki/Italian_language
        it = 10,        // deployed with ASP

        [Description("فارسی (Persian/Farsi)")]     // farsi https://en.wikipedia.org/wiki/Persian_language
        fa = 11,        // NOT deployed with ASP

        [Description("Polski (Polish)")]     // pol  https://en.wikipedia.org/wiki/Polish_language
        pl = 12,        // deployed with ASP

        [Description("汉语/中文 (Chinese)")]     // zho https://en.wikipedia.org/wiki/Chinese_language ???
        zh = 13,            // zh-Hant and  zh-Hans deployed with ASP

        [Description("Nederlands (Dutch)")]     // nld https://en.wikipedia.org/wiki/Dutch_language
        nl = 14,        // NOT deployed with ASP

        [Description("Türkçe (Turkish)")]     // tur https://en.wikipedia.org/wiki/Turkish_language
        tr = 15,    // deployed with ASP

        [Description("Čeština (Czech)")]            // ces https://en.wikipedia.org/wiki/Czech_language
        cs = 16,        // deployed with ASP

        [Description("한국어/韓國語 (Korean)")]       // kor https://en.wikipedia.org/wiki/Korean_language
        ko = 17,        // deployed with ASP

        [Description("اَلْعَرَبِيَّةُ‎ (Arabic)")]          // https://en.wikipedia.org/wiki/Arabic
        ar = 18,
        [Description("ελληνικά (Greek)")]           // https://en.wikipedia.org/wiki/Greek_language
        el = 19,
        [Description("bahasa (Indonesian)")]        // https://en.wikipedia.org/wiki/Indonesian_language
        id = 20,
        [Description("हिन्दी (Hindi)")]                // https://en.wikipedia.org/wiki/Hindi
        hi = 21,

        // Swedish
        // Hungarian
        // Romanian

        // Thai
        // Slovak
        // Danish
        // Hebrew
        // Ukrainian
        // Urdu // Pakistan

        // fr_CA = "French Canadian", 
        // en_CA = "English_Canadian"
        // en_AU = "English_Australian"

        [Description("Test Language (Test Accents)")]     // for testing auto translation.
        test = 100,     // https://en.wikipedia.org/wiki/Constructed_language
    }

    /// <summary>
    /// Meta/Info for language. similar to CultureInfo
    /// </summary>
    [Serializable()]
    public class Language
    {
        public const string kDefault = "en";   // source = from English ISO_639
        public const string kCultureDef = "en-US"; // default English culture

        public LanguageId Id;  // int = Popularity rank. name from ISO 639-1 AKA TwoLetterISOLanguageName

        public string? Name;     // AKA EnglishName

        public string? NativeName;       // Native speakers term for it.

        public string? URL;     // Wikipedia page.

        public string TwoLetterISOLanguageName => Id.ToString();

        public string GetDescription()
        {
            if (String.IsNullOrWhiteSpace(Name))    // should not happen!
                return ValidState.kInvalidName;
            if (String.IsNullOrWhiteSpace(NativeName))
                return Name;
            return string.Concat(NativeName, " (", Name, ")");
        }

        /// <summary>
        /// get LanguageId from string. forgiving.
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="testLevel">how close does the match need to be ?</param>
        /// <returns></returns>
        public static LanguageId GetId(string? lang, int testLevel = 3)
        {
            if (string.IsNullOrWhiteSpace(lang))
                return LanguageId.native;

            lang = lang.ToLower();

            Array enumValues = Enum.GetValues(typeof(LanguageId));
            foreach (LanguageId value in enumValues)
            {
                string langVal = value.ToString();
                if (langVal == lang)   // code.
                    return value;
                if (testLevel <= 1)
                    continue;
                if (lang == ((int)value).ToString())    // number
                    return value;
                if (testLevel <= 2 || lang.Length <= 2) // dont test desc. strict tets.
                    continue;
                string desc = value.ToDescription().ToLower();  // part of full text.
                if (desc.Contains(lang))
                    return value;
            }

            return LanguageId.native;
        }

        /// <summary>
        /// Get the best value from the "Accept-Language" format string.
        /// HTTP Accept-Language tag e.g. "en-US,en;q=0.9"
        /// </summary>
        /// <param name="acceptLang"></param>
        /// <returns></returns>
        public static LanguageId GetAcceptLang(string? acceptLang)
        {
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

        /// <summary>
        /// Get close equiv .NET CultureInfo for the LanguageId.
        /// describes how to sort strings, show decimals and show date time for a language/culture. like RequestCulture.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static System.Globalization.CultureInfo GetCulture(LanguageId id)
        {
            if (id < LanguageId.en || id >= LanguageId.test)
                id = LanguageId.en;
            return new System.Globalization.CultureInfo(id.ToString());
        }
    }
}
