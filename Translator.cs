using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    interface ITranslatorTo
    {
        // translate from some set language to some other language.
        // May use underlying cache. 
        // fromLangWords may be scraped from MVC razor pages etc. 
        // Ignored or filtered stuff : XML may be filtered out, {0} may be filtered. 
        // Similar to ASP Core IStringLocalizer

        bool SetFromLanguage(LanguageId fromLang);
        string TranslateTo(string fromLangWords, LanguageId toLang);
        string TranslateHtmlTo(string fromLangHtml, LanguageId toLang);
    }

    public abstract class TranslatorBase : ITranslatorTo
    {
        // Base implementation of a translation engine.
        // Similar to ASP IStringLocalizer<AboutController> _localizer;
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-2.2
        // Use the IHtmlLocalizer<T> implementation for resources that contain HTML.
        // NOTE: we might want to check the "Accept-Language" header on HTTP messages ?

        protected LanguageId _fromLang = LanguageId.en;           // English is default.

        public static ulong GetHashCode(string s)
        {
            // create a hash code for the source string. 
            // Try to make this as fast as possible on longer strings.
            // use 64 bits for lower hash collisions. 
            return HashUtil.GetKnuthHash(s);
        }

        public virtual bool SetFromLanguage(LanguageId fromLang)
        {
            _fromLang = fromLang;
            return true;
        }

        public abstract string TranslateTo(string fromLangWords, LanguageId toLang);

        public string TranslateHtmlTo(string fromLangHtml, LanguageId toLang)
        {
            // Break up XML/HTML and just translate the text contents.
            // Ignore tags and elements. ignore {handlebars} ??

            if (toLang == _fromLang)
                return fromLangHtml;
            return fromLangHtml;
        }
    }
    
    public class TranslatorTest : TranslatorBase
    {
        // A dummy/test translator . takes English (or anything) and converts to a fake langauge with accented vowels.
        // LanguageId.test
        // Assume any caching happens at a higher level.

        static char[] _pairs = new char[] {     // Swap letters.
            'A', 'Ä', 'a', 'ä', // C4, E4
            'E', 'Ë', 'e', 'ë', // CB, EB
            'I', 'Ï', 'i', 'ï', // CF, EF
            'O', 'Ö', 'o', 'ö', // 
            'U', 'Ü', 'u', 'ü', // 
            'Y', 'Ÿ', 'y', 'ÿ', // 9F, FF

            'C', 'Ç', 'c', 'ç',     // C7,
            'N', 'Ñ', 'n', 'ñ',     // D1,
            'S', 'Š', 's', 'š',     // 8A, 9A
            'Z', 'Ž', 'z', 'ž',     // 8E, 

            'D', 'Ð',       // D0
            'f', 'ƒ',       // 83
        };

        public const char kStart = 'α'; // all test words start with .
        public const char kEnd = 'Ω'; // all test words end with .

        public static bool IsTestLang(string s)
        {
            return s.Contains(kEnd.ToString());
        }

        public override string TranslateTo(string fromLangWords, LanguageId toLang)
        {
            if (toLang == _fromLang)
                return fromLangWords;

            // Throw if the text is already translated. NO double translation.
            ValidState.ThrowIf(IsTestLang(fromLangWords));

            //string result;

            //ValidState.ThrowIf(!IsTestLang(result));
            return fromLangWords;
        }

    }
    
    public class TranslatorGoogle : TranslatorBase
    {
        // Wrap the Google translate service.
        // Assume any caching happens at a higher level.

        public override bool SetFromLanguage(LanguageId fromLang)
        {
            throw new NotImplementedException();
        }

        public override string TranslateTo(string fromLangWords, LanguageId toLang)
        {
            throw new NotImplementedException();
        }
    }
}
