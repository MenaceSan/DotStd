using System;
using System.Text.RegularExpressions;

namespace DotStd
{
    /// <summary>
    /// CreditCard Type Id. For use with BluePay, etc.
    /// http://www.informit.com/articles/article.aspx?p=1223879&seqNum=12
    /// </summary>
    public enum CreditCardTypeId
    {
        Unk = 0,
        Visa = 1,       // VISA - start with 4 and are 13 or 16 digits
        MasterCard = 2, // MC - 16 digits. the first digit is always 5, and the second digit is 1 through 5
        Discover = 3,   // DISC - 16 digits and start with digits 6011
        Amex = 4,       // AMEX - 15 digits and start with 34 or 37
        DinersClub = 5,     // Diners Club - 14 digits and begin with 300 through 305, 36, or 38
        // UCB ?
        // MaxValue
    }

    /// <summary>
    /// Helper class for CC
    /// derived from https://www.codeproject.com/Articles/20271/Ultimate-NET-Credit-Card-Utility-Class?msg=4819150#xx4819150xx
    /// </summary>
    public static class CreditCard
    {
        // BEWARE CreditCardTypeId hard coded as string. e.g. "Visa"
        private const string _cardRegex = "^(?:(?<Visa>4\\d{3})|(?<MasterCard>5[1-5]\\d{2})|(?<Discover>6011)|(?<DinersClub>(?:3[68]\\d{2})|(?:30[0-5]\\d))|(?<Amex>3[47]\\d{2}))([ -]?)(?(DinersClub)(?:\\d{6}\\1\\d{4})|(?(Amex)(?:\\d{6}\\1\\d{5})|(?:\\d{4}\\1\\d{4}\\1\\d{4})))$";
        private static readonly Lazy<Regex> _cardRegex1 = new Lazy<Regex>( () => new Regex(_cardRegex));

        /// <summary>
        /// strip all valid spacers out. All the rest MUST be digits !
        /// </summary>
        /// <param name="cardNum"></param>
        /// <returns></returns>
        private static string GetClean(string cardNum)
        {
            if (string.IsNullOrWhiteSpace(cardNum))
                return "";
            // Clean the card number- remove dashes and spaces
            return cardNum.Replace("-", "").Replace(" ", "");
        }

        /// <summary>
        /// Compare the supplied card number with the regex pattern and get reference regex named groups
        /// NOTE: this does not mean the card is valid format/length, just that it resembles a type.
        /// </summary>
        /// <param name="cardNum"></param>
        /// <returns></returns>
        public static CreditCardTypeId GetCardTypeFromNumber2(string cardNum)
        {
            // Assume clean cardNum.
            GroupCollection gc = _cardRegex1.Value.Match(cardNum).Groups;

            for (CreditCardTypeId id = CreditCardTypeId.Visa; id <= CreditCardTypeId.DinersClub; id++)
            {
                // Compare each card type to the named groups to determine which card type the number matches
                if (gc[id.ToString()].Success)
                {
                    return id;
                }
            }

            // Card type is not supported by our system, return null
            return CreditCardTypeId.Unk;
        }

        public static CreditCardTypeId GetCardTypeFromNumber(string cardNum)
        {
            return GetCardTypeFromNumber2(GetClean(cardNum));
        }

        /// <summary>
        /// Does this card number look legit?
        /// Luhn Algorithm Adapted from code available on Wikipedia at
        /// http://en.wikipedia.org/wiki/Luhn_algorithm
        /// </summary>
        /// <param name="cardNum"></param>
        /// <returns></returns>
        public static bool IsValidLuhn2(string cardNum)
        {
            // Assume clean cardNum.
            int i = cardNum.Length - 1;
            if (i < 0)
                return false;

            int sum = 0;
            bool alt = false;
            for (; i >= 0; i--)
            {
                char ch = cardNum[i];
                if (!StringUtil.IsDigit1(ch)) // must be all digits.
                    return false;

                int curDigit = (ch - '0');
                if (alt)
                {
                    curDigit *= 2;
                    if (curDigit > 9)
                    {
                        curDigit -= 9;
                    }
                }
                sum += curDigit;
                alt = !alt;
            }

            // If Mod 10 equals 0, the number is good and this will return true
            return sum % 10 == 0;
        }

        public static bool IsValidLuhn(string cardNum)
        {
            return IsValidLuhn2(GetClean(cardNum));
        }

        public static bool IsValidType(CreditCardTypeId id)
        {
            if (id <= CreditCardTypeId.Unk)
                return false;
            if (id > CreditCardTypeId.DinersClub)
                return false;
            return true;
        }

        public static bool IsValidLength(int len, CreditCardTypeId cardType)
        {
            return true;
        }

        public static bool IsValidNumber2(string cardNum, CreditCardTypeId cardType)
        {
            // Assume clean cardNum.
            if (!IsValidType(cardType))
                return false;
            if (!IsValidLength(cardNum.Length, cardType))
                return false;

            // Make sure the supplied number matches the supplied card type
            if (!_cardRegex1.Value.Match(cardNum).Groups[cardType.ToString()].Success)
            {
                return false; // The card number does not match the card type
            }

            // all cards use Luhn's alg
            return IsValidLuhn2(cardNum);
        }

        public static bool IsValidNumber(string cardNum, CreditCardTypeId cardType)
        {
            return IsValidNumber2(GetClean(cardNum), cardType);
        }

        public static bool IsValidNumber(string cardNum)
        {
            // Determine the card type based on the number
            cardNum = GetClean(cardNum);

            CreditCardTypeId cardType = GetCardTypeFromNumber2(cardNum);
            if (!IsValidType(cardType))
                return false;
            if (!IsValidLength(cardNum.Length, cardType))
                return false;

            // all cards use Luhn's alg
            return IsValidLuhn2(cardNum);
        }

        /// <summary>
        /// Return a bogus CC number that passes Luhn and format tests for a given CreditCardTypeId
        /// Src: https://www.paypal.com/en_US/vhelp/paypalmanager_help/credit_card_numbers.htm
        /// http://www.geekswithblogs.net/sdorman
        /// </summary>
        /// <param name="cardType"></param>
        /// <returns></returns>
        public static string GetCardTestNumber(CreditCardTypeId cardType)
        {
            // According to PayPal, the valid test numbers that should be used
            // for testing card transactions are:
            // Credit Card Type              Credit Card Number
            // American Express              378282246310005
            // American Express              371449635398431
            // American Express Corporate    378734493671000
            // Diners Club                   30569309025904
            // Diners Club                   38520000023237
            // Discover                      6011111111111117
            // Discover                      6011000990139424
            // MasterCard                    5555555555554444
            // MasterCard                    5105105105105100
            // Visa                          4111111111111111
            // Visa                          4012888888881881

            switch (cardType)
            {
                case CreditCardTypeId.Unk:
                    return ValidState.kInvalidName;
                case CreditCardTypeId.Visa:
                    return "4111 1111 1111 1111";
                case CreditCardTypeId.MasterCard:
                    return "5105 1051 0510 5100";
                case CreditCardTypeId.Discover:
                    return "6011 1111 1111 1117";
                case CreditCardTypeId.Amex:
                    return "3782 822463 10005";
                case CreditCardTypeId.DinersClub:
                    return "30569309025904";
                default:
                    return "!";
            }
        }
    }
}
