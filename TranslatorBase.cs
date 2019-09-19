using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
        Task<string> TranslateTextAsync(string fromLangWords, LanguageId toLang);
    }

    public abstract class TranslatorBase : ITranslatorTo
    {
        // Base implementation of a translation engine.
        // Similar to ASP IStringLocalizer<AboutController> _localizer;
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-2.2
        // Use the IHtmlLocalizer<T> implementation for resources that contain HTML.
        // NOTE: we might want to check the "Accept-Language" header on HTTP messages ?

        protected LanguageId _fromLang = LanguageId.en;           // English is default source language.

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

        public abstract Task<string> TranslateTextAsync(string fromLangWords, LanguageId toLang);
    }

    public class TranslatorTest : TranslatorBase
    {
        // A dummy/test translator . takes English (or anything) and converts to a fake langauge with accented vowels.
        // LanguageId.test
        // Assume any caching happens at a higher level.
        // TODO : ensure we dont double translate.

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

        public const char kStart = 'α'; // all test words start with this.
        public const char kEnd = 'Ω'; // all test words end with this. Assume this is not normal.

        public static bool IsTestLang(string s)
        {
            // Has this string already been translated? This should not happen . indicate failure.
            return s.Contains(kEnd.ToString());
        }

        public string TranslateText(string fromLangWords, LanguageId toLang)
        {
            if (toLang == _fromLang)
                return fromLangWords;

            // Throw if the text is already translated. NO double translation.
            ValidState.ThrowIf(IsTestLang(fromLangWords));

            //string result;

            //ValidState.ThrowIf(!IsTestLang(result));
            return fromLangWords;
        }

        public override Task<string> TranslateTextAsync(string fromLangWords, LanguageId toLang)
        {
            return Task.FromResult(TranslateText(fromLangWords, toLang));
        }
    }

    public class TranslatorGoogle : TranslatorBase
    {
        // Wrap the Google translate service.
        // Assume any caching happens at a higher level.

        public override bool SetFromLanguage(LanguageId fromLang)
        {
            base.SetFromLanguage(fromLang);
            throw new NotImplementedException();
        }

        public override Task<string> TranslateTextAsync(string fromLangWords, LanguageId toLang)
        {
            // TODO IMPL
            throw new NotImplementedException();
        }
    }
}
