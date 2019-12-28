using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DotStd
{
    public interface ITranslatorProvider1
    {
        // Translate some text. Assume it ignores and preserves punctuation, spaces and place holders like {0}
        // ASSUME this does not throw an exception. We can safely call this in non-async code.
        // related to selected CultureInfo

        Task<string> TranslateAsync(string fromText);
    }

    public interface ITranslatorProvider 
    {
        // Translate from some source language to some target language.
        // May use underlying cache. 
        // Ignored/filtered stuff : <XML> may be filtered out, {0} MUST NOT be filtered/altered. 
        // Similar to ASP Core IStringLocalizer

        LanguageId FromLangId { get; }

        // Select FromLangId
        bool SetFromLanguage(LanguageId fromLang);

        // What languages are available to translate to? LanguageId as string, Name of language in fromLang.
        Task<List<TupleKeyValue>> GetToLanguages();

        // Translate some text. Assume it ignores and preserves punctuation, spaces and place holders like {0}
        Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native);

        // Translate a batch of texts.
        Task<List<string>> TranslateBatchAsync(List<string> fromTexts, LanguageId toLang = LanguageId.native);
    }

    public abstract class TranslatorBase : ITranslatorProvider
    {
        // Base implementation of a translation engine. provider
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

        public LanguageId FromLangId => _fromLang;

        public virtual bool SetFromLanguage(LanguageId fromLang)
        {
            _fromLang = fromLang;
            return true;
        }

        public abstract Task<List<TupleKeyValue>> GetToLanguages();

        public abstract Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native);

        public virtual async Task<List<string>> TranslateBatchAsync(List<string> fromTexts, LanguageId toLang = LanguageId.native)
        {
            // translate a batch of texts.
            // Just treat the batch as a bunch of singles if i cant optimize this.
            var lst = new List<string>();
            foreach (string txt in fromTexts)
            {
                lst.Add(await TranslateTextAsync(txt, toLang));
            }
            return lst;
        }
    }

    public class TranslatorTest : TranslatorBase
    {
        // A dummy/test translator . takes English (or anything) and converts to a fake language with accented vowels.
        // LanguageId.test
        // Assume any caching happens at a higher level.
        // TODO : ensure we don't double translate.

        static char[] _pairs = new char[] {     // Swap letters.

            'A', 'Ä', 'a', 'ä', // C4, E4
            'B', 'ß', 'b', 'Ƅ',
            'C', 'Ç', 'c', 'ç',     // C7,
            'D', 'Ð', 'd', 'đ',     // D0
            'E', 'Ë', 'e', 'ë', // CB, EB
            'F', 'Ƒ', 'f', 'ƒ',       // 83
            'G', 'Ĝ', 'g', 'ğ',
            'H', 'Ħ', 'h', 'ħ',
            'I', 'Ï', 'i', 'ï', // CF, EF
            'J', 'Ĵ', 'j', 'ĵ',
            'K', 'Ķ', 'k', 'ķ',
            'L', 'Ĺ', 'l', 'ĺ',
            'm', 'ʍ',
            'N', 'Ñ', 'n', 'ñ',     // D1,
            'O', 'Ö', 'o', 'ö', // 
            'P', 'Ƥ', 'p', 'ƥ',
            'q', 'ʠ',
            'R', 'Ʀ',
            'S', 'Š', 's', 'š',     // 8A, 9A
            'T', 'Ť', 't', 'ŧ',
            'U', 'Ü', 'u', 'ü', // 
            'V', 'Ɣ',
            'W', 'Ŵ', 'w', 'ŵ',
            'X', 'Ӽ', 'x', 'ӽ',
            'Y', 'Ÿ', 'y', 'ÿ', // 9F, FF
            'Z', 'Ž', 'z', 'ž',     // 8E, 
        };

        public const char kStart = 'α'; // all test words start with this. Assume this is not normal.
        public const char kEnd = '¡'; // all test words end with this. Assume this is not normal. IsTestLang

        public static bool IsTestLang(string s)
        {
            // Has this string already been translated? This should not happen . indicate failure.
            return s.Contains(kStart.ToString());
        }

        public override Task<List<TupleKeyValue>> GetToLanguages()
        {
            // What languages do i translate to?
            return Task.FromResult(new List<TupleKeyValue> { new TupleKeyValue(LanguageId.test) });
        }

        private char TranslateLetter(char ch)
        {
            for (int i = 0; i < _pairs.Length; i++)
            {
                if (_pairs[i] == ch)
                    return _pairs[i + 1];
            }
            return ch;
        }

        public string TranslateText(string fromText, LanguageId toLang = LanguageId.native)
        {
            // Sync translate.
            if (toLang == _fromLang)    // nothing to do.
                return fromText;

            // Throw if the text is already translated. NO double translation.
            ValidState.ThrowIf(IsTestLang(fromText));

            int wordStart = -1;

            var sb = new StringBuilder();
            for (int i = 0; i < fromText.Length; i++)
            {
                char ch = fromText[i];

                bool isLetter = char.IsLetter(ch);
                if (isLetter)
                {
                    ch = TranslateLetter(ch);
                }

                if (wordStart >= 0) // was in a word.
                {
                    if (!isLetter)  // end of word.
                    {
                        sb.Append(kEnd);
                        wordStart = -1;
                    }
                }
                else // was not in a word.
                {
                    if (isLetter) // start of new word.
                    {
                        if (i < fromText.Length - 1 && char.IsLetter(fromText[i + 1]))  // only for multi char words.
                        {
                            sb.Append(kStart);
                            wordStart = i;
                        }
                    }
                }

                sb.Append(ch);
            }

            string result = sb.ToString();
            ValidState.ThrowIf(!IsTestLang(result));
            return result;    // result fromText
        }

        public override Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native)
        {
            // not async.
            return Task.FromResult(TranslateText(fromText, toLang));
        }
    }

    public class TranslatorGoogleRest : TranslatorBase
    {
        // Wrap the Google translate service.
        // Assume any caching happens at a higher level.
        // Rest = Bypass Google  "Google.Cloud.Translation.V2" NuGet library.
        // Translator Will ignore embedded HTML tags?

        // https://codelabs.developers.google.com/codelabs/cloud-translation-csharp/index.html?index=..%2F..index#5
        // https://stackoverflow.com/questions/2246017/using-google-translate-in-c-sharp
        // https://cloud.google.com/translate/
        // https://cloud.google.com/translate/docs/translating-text

        // You will only be allowed to translate about 100 words per hour using the free API. If you abuse this, Google API will return a 429 (Too many requests) error.

        public string ApiKey; // send this to Google for commercial usage.

        public override Task<List<TupleKeyValue>> GetToLanguages()
        {
            // What languages does this provider support ?
            // TODO support this properly ?? ask Google for the list.
            var langs = new List<TupleKeyValue>
            {
                new TupleKeyValue(LanguageId.ru),
                new TupleKeyValue(LanguageId.de),
                new TupleKeyValue(LanguageId.es),
                new TupleKeyValue(LanguageId.fr),
                new TupleKeyValue(LanguageId.ja),
                new TupleKeyValue(LanguageId.pt),
                new TupleKeyValue(LanguageId.it),
                new TupleKeyValue(LanguageId.fa),
            };
            return Task.FromResult(langs);
        }

        public async Task<string> TranslateSingleAsync(string fromText, LanguageId toLang = LanguageId.native)
        {
            // use the older single sentence format. additional sentences are ignored.

            if (toLang == _fromLang)    // nothing to do.
                return fromText;

            const string baseUrl = "https://translate.googleapis.com/translate_a/single";

            string fromTextEnc = WebUtility.UrlEncode(fromText);
            string url = $"{baseUrl}?client=gtx&sl={_fromLang}&tl={toLang}&dt=t&q={fromTextEnc}";

            using (var client = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(ApiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpUtil.kAuthBearer, ApiKey);
                    url += "&key=" + ApiKey;
                }

                byte[] bytesResp = await client.GetByteArrayAsync(url);
                string jsonResp = Encoding.UTF8.GetString(bytesResp, 0, bytesResp.Length);  // read System.Text.Encoding.UTF8

                // e.g. jsonResp = [[["Hallo","Hello",null,null,1]],null,"en"]
                const int kOffset = 4;

                string toText = jsonResp.Substring(kOffset, jsonResp.IndexOf("\"", kOffset, StringComparison.Ordinal) - kOffset);
                return toText;
            }
        }

        const string kUrlGoogTrans = "https://translation.googleapis.com/language/translate/v2";

        public async Task<string> TranslateJsonAsync(string fromEnc, LanguageId toLang = LanguageId.native)
        {
            // use the newer JSON format.
            // https://cloud.google.com/translate/docs/basic/translating-text

            if (toLang == _fromLang)    // nothing to do.
                return fromEnc;

            string url = kUrlGoogTrans;
            using (var client = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(ApiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpUtil.kAuthBearer, ApiKey);
                    url += "?key=" + ApiKey;
                }

                string json = JsonConvert.SerializeObject(new { q = fromEnc, source = LanguageId.en.ToString(), target = toLang.ToString() });        // format = "text"
                var content = new StringContent(json, Encoding.UTF8, DocumentType.JSON.ToDescription());

                HttpResponseMessage resp = await client.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();

                string respStr = await resp.Content.ReadAsStringAsync();
                dynamic respObj = JsonConvert.DeserializeObject(respStr);

                // detectedSourceLanguage
                string toText = respObj.data.translations[0].translatedText;
                return toText;
            }
        }

        public override async Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native)
        {
            // ASSUME this does not throw an exception. We can safely call this in non-async code.
            try
            {
                if (string.IsNullOrWhiteSpace(ApiKey))
                    return await TranslateSingleAsync(fromText, toLang);
                else
                    return await TranslateJsonAsync(fromText, toLang);
            }
            catch
            {
                return null;
            }
        }

        public override async Task<List<string>> TranslateBatchAsync(List<string> fromTexts, LanguageId toLang = LanguageId.native)
        {
            // translate a batch of texts.

            if (toLang == _fromLang)    // nothing to do.
                return null;

            var lstResp = new List<string>();
            string url = kUrlGoogTrans;

            using (var client = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(ApiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpUtil.kAuthBearer, ApiKey);
                    url += "?key=" + ApiKey;
                }

                object req = new { q = fromTexts, target = toLang.ToString() };
                string json = JsonConvert.SerializeObject(req);
                var content = new StringContent(json, Encoding.UTF8, DocumentType.JSON.ToDescription());

                HttpResponseMessage resp = await client.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();

                string respStr = await resp.Content.ReadAsStringAsync();
                dynamic respObj = JsonConvert.DeserializeObject(respStr);

                // detectedSourceLanguage
                foreach (dynamic tran in respObj.data.translations)
                {
                    lstResp.Add(tran.translatedText);
                }
            }

            return lstResp;
        }
    }
}
