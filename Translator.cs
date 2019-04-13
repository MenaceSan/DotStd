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

        bool SetFromLanguage(string fromLang);
        string TranslateTo(string fromLangWords, string toLang);
    }

    public abstract class TranslatorBase : ITranslatorTo
    {
        // LanguageId

        public static ulong GetHashCode(string s)
        {
            // create a hash code for the string. 
            // Try to make this as fast as possible on longer strings.
            // use 64 bits for lower hash collisions. 
            return HashUtil.GetKnuthHash(s);
        }

        public abstract bool SetFromLanguage(string fromLang);
        public abstract string TranslateTo(string fromLangWords, string toLang);
    }

    public class TranslatorGoogle : TranslatorBase
    {
        // Wrap the google translate service.
        public override bool SetFromLanguage(string fromLang)
        {
            throw new NotImplementedException();
        }

        public override string TranslateTo(string fromLangWords, string toLang)
        {
            throw new NotImplementedException();
        }
    }
}
