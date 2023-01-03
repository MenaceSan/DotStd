using System.ComponentModel;

namespace DotStd
{
    /// <summary>
    /// Define localized currency type. 
    /// What currency we want to be paid in?
    /// ISO 4217 - https://en.wikipedia.org/wiki/ISO_4217
    /// </summary>
    public enum CurrencyId
    {
        [Description("U.S Dollar")]
        USD = 840,    // $, US$, U.S. Dollar https://en.wikipedia.org/wiki/United_States_dollar
        [Description("European Euro")]
        EUR = 978,        // €  https://en.wikipedia.org/wiki/Euro
        [Description("Japanese Yen")]
        JPY = 392,        // 円 or ¥ https://en.wikipedia.org/wiki/Japanese_yen
        [Description("UK Pound Sterling")]
        GBP = 826,        // https://en.wikipedia.org/wiki/Pound_sterling
        [Description("Australian dollar")]
        AUD = 36,        // Australian dollar. https://en.wikipedia.org/wiki/Australian_dollar
        [Description("Canadian Dollar")]
        CAD = 124,        // $ Canadian Dollar, Loonie, https://en.wikipedia.org/wiki/Canadian_dollar
        [Description("Swiss Franc")]
        CHF = 756,        // https://en.wikipedia.org/wiki/Swiss_franc
        [Description("Chinese yuan (Renminbi, RMB)")]
        CNY = 156,        // Chinese yuan (Renminbi, RMB)  // https://en.wikipedia.org/wiki/Renminbi
        [Description("Swedish krona")]
        SEK = 752,          // https://en.wikipedia.org/wiki/Swedish_krona
        [Description("New Zealand dollar")]
        NZD = 554,        // $, NZ$, Kiwi, New Zealand Dollar https://en.wikipedia.org/wiki/New_Zealand_dollar
        [Description("Mexican peso")]
        MXN = 484,      // https://en.wikipedia.org/wiki/Mexican_peso
        [Description("Singapore dollar")]
        SGD = 702,        // https://en.wikipedia.org/wiki/Singapore_dollar

        [Description("Hong Kong dollar")]
        HKD = 344,            // HKD (HK$)  https://en.wikipedia.org/wiki/Hong_Kong_dollar
        [Description("Norwegian krone")]
        NOK = 578,            // KRW (₩) https://en.wikipedia.org/wiki/Norwegian_krone
        [Description("South Korean won")]
        KRW = 410,            // https://en.wikipedia.org/wiki/South_Korean_won
        [Description("Turkish lira")]
        TRY = 949,            // https://en.wikipedia.org/wiki/Turkish_lira
        [Description("Russian ruble")]
        RUB = 643,            // https://en.wikipedia.org/wiki/Russian_ruble
        [Description("Indian Rupee")]
        INR = 356,        // ₹ Indian Rupee  // https://en.wikipedia.org/wiki/Indian_rupee
        [Description("Brazilian real")]
        BRL = 986,                // https://en.wikipedia.org/wiki/Brazilian_real
        [Description("South African Rand")]
        ZAR = 710,        // R, Rand, South African Rand. https://en.wikipedia.org/wiki/South_African_rand

        [Description("Bitcoin (or XBT)")]
        BTC = 1000,        // https://en.wikipedia.org/wiki/Bitcoin
        // 
    }

    /// <summary>
    /// Helper for different currency types. CurrencyId.
    /// amount of currency should always be decimal type.
    /// </summary>
    public class CurrencyInfo
    {
        public readonly CurrencyId CurrencyId;
        public readonly string Sign;        // Symbol. e.g. "$"
        public readonly string Sign2;       // more specific sign. (for mixed usage. e.g. "US$")
        public readonly bool SignPostfix;    // prefix or postfix ?
        public readonly string? URL;        // wikipedia .

        public string Code => CurrencyId.ToString();
        public string Description => CurrencyId.ToDescription();

        // Exponent = 2
        public static readonly CurrencyInfo kUSD = new CurrencyInfo(CurrencyId.USD, "$", "US$", false, "https://en.wikipedia.org/wiki/United_States_dollar");
        public static readonly CurrencyInfo kEUR = new CurrencyInfo(CurrencyId.EUR, "€", null, false, "https://en.wikipedia.org/wiki/Euro");
        public static readonly CurrencyInfo kJPY = new CurrencyInfo(CurrencyId.JPY, "円", null, false, "https://en.wikipedia.org/wiki/Japanese_yen");
        public static readonly CurrencyInfo kGBP = new CurrencyInfo(CurrencyId.GBP, "£", null, false, "https://en.wikipedia.org/wiki/Pound_sterling");

        public static readonly CurrencyInfo kAUD = new CurrencyInfo(CurrencyId.AUD, "$", "AU$", false, "https://en.wikipedia.org/wiki/Australian_dollar");
        public static readonly CurrencyInfo kCAD = new CurrencyInfo(CurrencyId.CAD, "$", "CA$", false, "https://en.wikipedia.org/wiki/Canadian_dollar");
        public static readonly CurrencyInfo kCHF = new CurrencyInfo(CurrencyId.CHF, "Fr.", null, false, "https://en.wikipedia.org/wiki/Swiss_franc");
        public static readonly CurrencyInfo kCNY = new CurrencyInfo(CurrencyId.CNY, "元", null, false, "https://en.wikipedia.org/wiki/Renminbi");

        public static readonly CurrencyInfo kSEK = new CurrencyInfo(CurrencyId.SEK, "kr", null, false, "https://en.wikipedia.org/wiki/Swedish_krona");
        public static readonly CurrencyInfo kNZD = new CurrencyInfo(CurrencyId.NZD, "$", "NZ$", false, "https://en.wikipedia.org/wiki/New_Zealand_dollar");
        public static readonly CurrencyInfo kMXN = new CurrencyInfo(CurrencyId.MXN, "$", "MX$", false, "https://en.wikipedia.org/wiki/Mexican_peso");
        public static readonly CurrencyInfo kSGD = new CurrencyInfo(CurrencyId.SGD, "S$", null, false, "https://en.wikipedia.org/wiki/Singapore_dollar");

        // ...

        public static readonly CurrencyInfo kBTC = new CurrencyInfo(CurrencyId.BTC, "₿", null, false, "https://en.wikipedia.org/wiki/Bitcoin");

        public static readonly CurrencyInfo kUNK = new CurrencyInfo(ValidState.kInvalidId, "?", null, false);

        private CurrencyInfo(CurrencyId id, string sign, string? sign2 = null, bool signPost = false, string? url = null)
        {
            CurrencyId = id;
            Sign = sign;
            Sign2 = sign2 ?? sign;
            SignPostfix = signPost;
            URL = url;
        }

        public static CurrencyInfo Get(CurrencyId currencyId)
        {
            switch (currencyId)
            {
                case CurrencyId.USD:
                    return kUSD;
                case CurrencyId.EUR:
                    return kEUR;
                case CurrencyId.JPY:
                    return kJPY;
                case CurrencyId.GBP:
                    return kGBP;

                case CurrencyId.AUD:
                    return kAUD;
                case CurrencyId.CAD:
                    return kCAD;
                case CurrencyId.CHF:
                    return kCHF;
                case CurrencyId.CNY:
                    return kCNY;

                case CurrencyId.SEK:
                    return kSEK;
                case CurrencyId.NZD:
                    return kNZD;
                case CurrencyId.MXN:
                    return kMXN;
                case CurrencyId.SGD:
                    return kSGD;

                // ...

                case CurrencyId.BTC:
                    return kBTC;
            }

            return kUNK;
        }

        private string GetCurrencySL(string s)
        {
            // Get currency with prefix/postfix label. e.g. "$1.12"
            // JavaScript can use 'prefix' ?

            if (SignPostfix)
            {
                return s + Sign;
            }
            return Sign + s;
        }

        public string GetCurrency(decimal d)
        {
            // Get currency with no label. e.g. 1.12
            // Proper/normal number of decimal places.
            return d.ToString("F2");
        }

        public string GetCurrencyR(decimal d)
        {
            // Get rate (extra precision) currency  with no label
            // extra precision decimal places for rates. e.g. 1.123
            return d.ToString("F3");
        }

        public string GetCurrencyL(decimal d)
        {
            // Get currency with label
            return GetCurrencySL(GetCurrency(d));
        }
        public string GetCurrencyRL(decimal d)
        {
            // Get rate (extra precision) currency with label
            // extra precision for rates. e.g. $1.123
            return GetCurrencySL(GetCurrencyR(d));
        }
    }
}
