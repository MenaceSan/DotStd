// using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Translate from some application NATIVE language text for a specific user/consumer who wants LangId.
    /// Assume it ignores and preserves punctuation, spaces and place holders like {0} (proper nouns are not translated)
    /// ASSUME this does not throw an exception. We can safely call this in non-async code.
    /// related to selected CultureInfo.  // Sort of related to TimeZone ? Not really.
    /// </summary>
    public interface ITranslatorProvider1
    {
        LanguageId LangId { get; }      // Destination language. Related to CultureInfo
        Task<string> TranslateAsync(string fromText);
    }

    /// <summary>
    /// Translate from some source language to some target language.
    /// May use underlying cache.
    /// Ignored/filtered stuff : <XML> may be filtered out, {0} MUST NOT be filtered/altered. 
    /// Similar to ASP Core IStringLocalizer
    /// </summary>
    public interface ITranslatorProvider
    {
        LanguageId FromLangId { get; }  // Source Language for text.

        /// Select FromLangId
        bool SetFromLanguage(LanguageId fromLang);

        /// <summary>
        /// What languages are available to translate to (given FromLangId)? LanguageId as string, Name of language in fromLang.
        /// </summary>
        /// <returns></returns>
        List<TupleKeyValue> GetToLanguages();

        /// <summary>
        /// Translate some text. Assume it ignores and preserves punctuation, spaces and place holders like {0}
        /// </summary>
        /// <param name="fromText"></param>
        /// <param name="toLang"></param>
        /// <returns></returns>
        Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native);

        /// <summary>
        /// Translate a batch of texts.
        /// </summary>
        /// <param name="fromTexts"></param>
        /// <param name="toLang"></param>
        /// <returns></returns>
        Task<List<string>> TranslateBatchAsync(List<string> fromTexts, LanguageId toLang = LanguageId.native);
    }

    /// <summary>
    /// Base implementation of a translation engine. provider
    /// Similar to ASP IStringLocalizer<AboutController> _localizer;
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-2.2
    /// Use the IHtmlLocalizer<T> implementation for resources that contain HTML.
    /// NOTE: check the "Accept-Language" header on HTTP messages ?
    /// </summary>
    public abstract class TranslatorBase : ITranslatorProvider
    {
        public const int kHashCode0 = 0;    // placeholder for hash not yet calculated.

        protected LanguageId _fromLang = LanguageId.en;           // English is default source language.

        /// <summary>
        /// create a hash code for the source string. 
        /// Try to make this as fast as possible on longer strings.
        /// use 64 bits for lower hash collisions. 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static ulong GetHashCode(string s)
        {
            return HashUtil.GetKnuthHash(s);
        }

        public LanguageId FromLangId => _fromLang;

        public virtual bool SetFromLanguage(LanguageId fromLang)
        {
            _fromLang = fromLang;
            return true;
        }

        public abstract List<TupleKeyValue> GetToLanguages();

        public abstract Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native);

        public virtual async Task<List<string>> TranslateBatchAsync(List<string> fromTexts, LanguageId toLang = LanguageId.native)
        {
            // translate a batch of texts at once.
            // default = Just treat the batch as a bunch of singles if i cant optimize this.

            var lst = new List<string>();
            foreach (string txt in fromTexts)
            {
                lst.Add(await TranslateTextAsync(txt, toLang)); // emit
            }
            return lst;
        }
    }

    public class TranslatorDummy : TranslatorBase, ITranslatorProvider1
    {
        // No/null/dummy translation.

        LanguageId ITranslatorProvider1.LangId => LanguageId.test; // ITranslatorProvider1 Destination language.

        public override List<TupleKeyValue> GetToLanguages()
        {
            throw new NotImplementedException();
        }

        public Task<string> TranslateAsync(string fromText)
        {
            // implement ITranslatorProvider1
            return Task.FromResult(fromText);
        }

        public override Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native)
        {
            // implement/override ITranslatorProvider
            return Task.FromResult(fromText);
        }
    }

    public class TranslatorTest : TranslatorBase
    {
        // A dummy/test translator . takes English (or anything) and converts to a fake language with accented vowels.
        // LanguageId.test
        // Assume any caching happens at a higher level.
        // TODO : ensure we don't double translate.

        static readonly char[] _pairs = new char[] {     // Swap letters.

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

        public const char kStart = '¡'; // all test words start with this. Assume this is not normal.

        public static bool IsTestLang(string s)
        {
            // Has this string already been translated? This should not happen . indicate failure.
            return s.Contains(kStart.ToString());
        }

        public override List<TupleKeyValue> GetToLanguages()
        {
            // What languages do i translate to?
            return new List<TupleKeyValue> { new TupleKeyValue(LanguageId.test) };
        }

        private static char TranslateLetter(char ch)
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
                        // sb.Append(kEnd);
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

    /// <summary>
    /// Alternative Wrapper for the Google translate service. 
    /// NOTE: You will only be allowed to translate about 100 words per hour using the free API. If you abuse this, Google API will return a 429 (Too many requests) error.
    /// NOTE: Too expensive to use for real!
    /// Assume any caching happens at a higher level.
    /// Rest = Bypass Google API  "Google.Cloud.Translation.V2" NuGet library.
    /// Translator Will ignore embedded HTML tags and {0}?
    /// </summary>
    public class TranslatorGoogleRest : TranslatorBase  // ExternalService
    {
        // https://codelabs.developers.google.com/codelabs/cloud-translation-csharp/index.html?index=..%2F..index#5
        // https://stackoverflow.com/questions/2246017/using-google-translate-in-c-sharp
        // https://cloud.google.com/translate/
        // https://cloud.google.com/translate/docs/translating-text

        public string? ApiKey; // send this to Google for commercial usage.

        static readonly List<TupleKeyValue> _Langs = new()
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

        public override List<TupleKeyValue> GetToLanguages()
        {
            // What languages does this provider support ?
            // TODO support this properly ?? ask Google for the list.
            return _Langs;
        }

        public async Task<string> TranslateSingleAsync(string fromText, LanguageId toLang = LanguageId.native)
        {
            // use the older single sentence format. additional sentences are ignored.

            if (toLang == _fromLang)    // nothing to do.
                return fromText;

            const string baseUrl = "https://translate.googleapis.com/translate_a/single";

            string fromTextEnc = WebUtility.UrlEncode(fromText);
            string url = $"{baseUrl}?client=gtx&sl={_fromLang}&tl={toLang}&dt=t&q={fromTextEnc}";

            using var client = new HttpClient();
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

        const string kUrlGoogTrans = "https://translation.googleapis.com/language/translate/v2";

        /// <summary>
        /// use the newer JSON format translation.
        /// https://cloud.google.com/translate/docs/basic/translating-text
        /// </summary>
        /// <param name="fromEnc"></param>
        /// <param name="toLang"></param>
        /// <returns></returns>
        public async Task<string> TranslateJsonAsync(string fromEnc, LanguageId toLang = LanguageId.native)
        {
            if (toLang == _fromLang)    // nothing to do.
                return fromEnc;

            string url = kUrlGoogTrans;
            using var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpUtil.kAuthBearer, ApiKey);
                url += "?key=" + ApiKey;
            }

            var req = new { q = fromEnc, source = LanguageId.en.ToString(), target = toLang.ToString() }; // format = "text"
            string json = JsonSerializer.Serialize(req);
            // string json = JsonConvert.SerializeObject(req);        // NewtonSoft

            var content = new StringContent(json, Encoding.UTF8, MimeId.json.ToDescription());

            HttpResponseMessage resp = await client.PostAsync(url, content);
            resp.EnsureSuccessStatusCode();

            string respStr = await resp.Content.ReadAsStringAsync();
            // dynamic? respObj = JsonConvert.DeserializeObject(respStr); // NewtonSoft
            JsonElement respObj = JsonSerializer.Deserialize<JsonElement>(respStr);
            if (respObj.ValueKind == JsonValueKind.Null || respObj.ValueKind == JsonValueKind.Undefined)
            {
                return ValidState.kInvalidName;
            }

            // detectedSourceLanguage
            var trans = respObj.GetProperty("data").GetProperty("translations").EnumerateArray();
            string toText = trans.Current.GetProperty("translatedText").ToString();    // this will throw if malformed?!
            return toText;
        }

        /// <summary>
        /// translate a single line of text.
        /// ASSUME this does not throw an exception. We can safely call this in non-async code.
        /// </summary>
        /// <param name="fromText"></param>
        /// <param name="toLang"></param>
        /// <returns></returns>
        public override async Task<string> TranslateTextAsync(string fromText, LanguageId toLang = LanguageId.native)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ApiKey))
                    return await TranslateSingleAsync(fromText, toLang);
                else
                    return await TranslateJsonAsync(fromText, toLang);
            }
            catch
            {
                return fromText;    // just leave untranslated
            }
        }

        /// <summary>
        /// translate a batch of texts. No Cache.
        /// </summary>
        /// <param name="fromTexts"></param>
        /// <param name="toLang"></param>
        /// <returns></returns>
        public override async Task<List<string>> TranslateBatchAsync(List<string> fromTexts, LanguageId toLang = LanguageId.native)
        {
            if (toLang == _fromLang)    // nothing to do.
                return fromTexts;

            var lstResp = new List<string>();

            using (var client = new HttpClient())
            {
                string url = kUrlGoogTrans;
                if (!string.IsNullOrWhiteSpace(ApiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(HttpUtil.kAuthBearer, ApiKey);
                    url += "?key=" + ApiKey;
                }

                object req = new { q = fromTexts, target = toLang.ToString() };
                // string json = JsonConvert.SerializeObject(req);   // NewtonSoft
                string json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, MimeId.json.ToDescription());

                HttpResponseMessage resp = await client.PostAsync(url, content);
                resp.EnsureSuccessStatusCode();

                string respStr = await resp.Content.ReadAsStringAsync();
                // dynamic? respObj = JsonConvert.DeserializeObject(respStr);  // NewtonSoft
                JsonElement respObj = JsonSerializer.Deserialize<JsonElement>(respStr);

                // detectedSourceLanguage
                if (respObj.ValueKind != JsonValueKind.Null && respObj.ValueKind != JsonValueKind.Undefined)
                {
                    var trans = respObj.GetProperty("data").GetProperty("translations").EnumerateArray();
                    foreach (JsonElement tran in trans)
                    {
                        lstResp.Add(tran.GetProperty("translatedText").ToString());
                    }
                }
            }

            return lstResp;
        }
    }
}
