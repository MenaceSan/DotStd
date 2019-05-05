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

        public static ulong GetHashCode(string s)
        {
            // create a hash code for the source string. 
            // Try to make this as fast as possible on longer strings.
            // use 64 bits for lower hash collisions. 
            return HashUtil.GetKnuthHash(s);
        }

        public abstract bool SetFromLanguage(LanguageId fromLang);
        public abstract string TranslateTo(string fromLangWords, LanguageId toLang);

        public string TranslateHtmlTo(string fromLangHtml, LanguageId toLang)
        {
            return null;
        }
    }

    public class TranslatorGoogle : TranslatorBase
    {
        // Wrap the Google translate service.
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
