using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DotStd
{
    public enum CurrencyId
    {
        // Define localized currency type. 
        // What currency we want to be paid in?
        // ISO 4217 - https://en.wikipedia.org/wiki/ISO_4217
        // https://en.wikipedia.org/wiki/Euro

        [Description("$, US$, U.S Dollar")]
        USD = 840,    // $, US$, U.S. Dollar https://en.wikipedia.org/wiki/United_States_dollar
        [Description("€ European Euro")]
        EUR = 978,        // €  https://en.wikipedia.org/wiki/Euro
        [Description("円 Japanese Yen")]
        JPY = 392,        // 円 or ¥ https://en.wikipedia.org/wiki/Japanese_yen
        [Description("£ UK Pound Sterling")]
        GBP = 826,        // https://en.wikipedia.org/wiki/Pound_sterling
        [Description("$ Australian dollar")]
        AUD = 36,        // Australian dollar. https://en.wikipedia.org/wiki/Australian_dollar
        [Description("$ Canadian Dollar")]
        CAD = 124,        // $ Canadian Dollar https://en.wikipedia.org/wiki/Canadian_dollar
        [Description("Fr. Swiss Franc")]
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
        [Description("₹ Indian Rupee")]
        INR = 356,        // ₹ Indian Rupee  // https://en.wikipedia.org/wiki/Indian_rupee
        [Description("Brazilian real")]
        BRL = 986,                // https://en.wikipedia.org/wiki/Brazilian_real
        [Description("R South African Rand")]
        ZAR = 710,        // R South African Rand. https://en.wikipedia.org/wiki/South_African_rand

        [Description("₿ Bitcoin (or XBT)")]
        BTC = 1000,        // https://en.wikipedia.org/wiki/Bitcoin

    }

    public class CurrencyUtil
    {
        // Helper for different currency types.
        // CurrencyId
        // amount of currency should always be decimal.

        public readonly CurrencyId CurrencyId;

        // Exponent = 2

        public CurrencyUtil(CurrencyId currencyId)
        {
            CurrencyId = currencyId;
        }

        public string GetCurrencySL(string s)
        {
            // Get currency with prefix/postfix label. e.g. $1.12
            // JavaScript can use 'prefix' ?
            switch (this.CurrencyId)
            {
                case CurrencyId.USD:
                case CurrencyId.CAD:
                    return "$" + s;
                case CurrencyId.EUR:
                    return "€" + s;
                case CurrencyId.JPY:
                    return "円" + s;
                case CurrencyId.GBP:
                    return "£" + s;
                case CurrencyId.CHF:
                    return "Fr." + s;

                case CurrencyId.BTC:
                    return "₿" + s;
                default:
                    return "?" + s;
            }
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
            // Get currency rate with label
            return GetCurrencySL(GetCurrencyR(d));
        }
        public string GetCurrencyRL(decimal d)
        {
            // Get rate (extra precision) currency with label
            // extra precision for rates. e.g. $1.123
            return GetCurrencySL(GetCurrencyR(d));
        }
    }
}
