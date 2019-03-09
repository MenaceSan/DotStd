using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DotStd
{
    [Serializable()]
    public enum CountryId
    {
        // Countries that we care about. CountryCode
        // from the table get_country
        // https://www.ncbi.nlm.nih.gov/books/NBK7249/
        // https://www.worldatlas.com/aatlas/ctycodes.htm A2 (ISO), A3 (UN), NUM (UN), DIALING CODE

        ANY = 0, // Don't care. give me all.
        [Description("United States")]
        USA = 840,    // US, us
        [Description("Sweden")]
        SWE = 752,    // SE, se 
        [Description("Canada")]
        CAN = 124,    // CA, ca
        [Description("Australia")]
        AUS = 36,    // AU, au

        // More ...
    }

    [Serializable()]
    public enum StateId
    {
        // States/Provinces in a country we care about (USA first)
        // from the table geo_state
        // https://www.50states.com/abbreviations.htm

        UNK = 0,        // Invalid default value.

        [Description("Alabama")]
        AL = 1,
        [Description("Alaska")]
        AK,
        [Description("Arizona")]
        AZ,
        [Description("Arkansas")]
        AR,
        [Description("California")]
        CA,
        [Description("Colorado")]
        CO,
        [Description("Connecticut")]
        CT,
        [Description("Delaware")]
        DE,
        [Description("Florida")]
        FL,
        [Description("Georgia")]
        GA,
        HI,
        ID,
        IL,
        [Description("Indiana")]
        IN = 14,
        [Description("Iowa")]
        IA = 15,
        KS,
        [Description("Kentucky")]
        KY = 17,
        LA,
        ME,
        MD,
        [Description("Massachusetts")]
        MA = 21,
        MI,
        [Description("Minnesota")]
        MN = 23,
        MS,
        MO,
        MT,
        NE,
        NV, NH,
        NJ,
        NM,
        NY,
        [Description("North Carolina")]
        NC = 33,
        ND,
        OH,
        OK,
        [Description("Oregon")]
        OR = 37,
        PA,
        [Description("Rhode Island")]
        RI,
        SC,
        SD,
        TN,
        TX,
        UT,
        VT,
        [Description("Virginia")]
        VA = 46,
        WA,
        WV,
        [Description("Wisconsin")]
        WI = 49,
        [Description("Wyoming")]
        WY = 50,

        [Description("American Samoa")]
        AS = 51,
        [Description("District of Columbia")]
        DC,
        [Description("Federated States of Micronesia")]
        FM,
        [Description("Guam")]
        GU,
        [Description("Marshall Islands")]
        MH,
        [Description("Northern Mariana Islands")]
        MP,
        [Description("Palau")]
        PW,
        [Description("Puerto Rico")]
        PR,
        [Description("Virgin Islands")]
        VI,

        // State/Province in other countries
        [Description("Alberta")]
        AB = 60,
        [Description("British Columbia")]
        BC,
        [Description("Manitoba")]
        MB,
        [Description("New Brunswick")]
        NB,
        [Description("Newfoundland and Labrador")]
        NL,
        [Description("Nova Scotia")]
        NS,
        [Description("Northwest Territories")]
        NT,
        [Description("Nunavut")]
        NU,
        [Description("Ontario")]
        ON,
        [Description("Prince Edward Island")]
        PE,
        [Description("Quebec")]
        QC,

#if false
        [Description("Saskatchewan")]
        SK,
        [Description("Yukon")]
        YT,
#endif

        // More ... in other countries. Manually added.
    }

    public enum TimeZoneId
    {
        // Difference in minutes from GMT if between (-12*60 and 12*60).
        // ASSUME all zones round down to 15 minute chunks.
        // ASSUME All values between (-12*60 and 12*60) observe US daylight savings time rules.
        // Create new time zones outside this range if the client does not use DST.
        // Name = Delta from east coast time to some target/client time.
        // Same values as JavaScript getTimezoneOffset()

        [Description("Hawaii (UTC-10:00)")]
        HST = -(10 * 60),        // -600
        [Description("Alaska (UTC-9:00)")]
        AKST = -(9 * 60),         // -540
        [Description("Pacific Time (UTC-8:00)")]
        PST = -(8 * 60),        // -480 = Pacific Time. Observes normal DST rules., 
        [Description("Mountain Time (UTC-7:00)")]
        MST = -(7 * 60),       // -420 = Mountain.  Observes normal DST rules. 
        [Description("Central Time (UTC-6:00)")]
        CST = -(6 * 60),        // -360 = Central time.  Observes normal DST rules. 
        [Description("Eastern Time (UTC-5:00)")]
        EST = -(5 * 60),        // -300 = Eastern Time.  Observes normal DST rules. 
        [Description("Atlantic Time (UTC-4:00)")]
        AST = -(4 * 60),       // -240 = +1 hour from EST

        UTC = 1,        // No DST.
        GMT = 2,        // uses DST.
        ChunkSize = 15,     // ASSUME all zones are in this chunk size.

        [Description("Australian Western Standard Time (UTC+8:00)")]
        AWST = (8 * 60),        // 480
        [Description("Australian Central Western Standard Time (UTC+8:45)")]
        ACWST = ((8 * 60) + 45),  //  525
        [Description("Australian Central Standard Time (UTC+9:30)")]
        ACST = ((9 * 60) + 30), //  570
        [Description("Australian Eastern Standard Time (UTC+10:00)")]
        AEST = (10 * 60),       // 600
        [Description("Lord Howe Standard Time (UTC+10:30)")]        // https://www.timeanddate.com/time/zones/lhst
        LHST = ((10 * 60) + 30),    // 630

        // More ...
    }

    public class GeoLocation
    {
        public const int k_ZipCode_MaxLen = 32;   // there are no countries that use Zip > 32 (i'm pretty sure)

        public float Lat;   // Latitude.
        public float Lon;   // Longitude

        public static int GetTZOffset(TimeZoneId id)
        {
            // -599 -> -600, 601 -> 600
            // 'Order by' should have lowest as best match.
            int i = (int)id;
            return i - Math.Abs(i % 15); // Chop 15 minute chunk.
        }

        public static float ParseValue(string v, int point)
        {
            // Parse a single dimension value string to a float.

            if (point <= 0) // no decimal place.
            {
                int i = ((v.Length & 1) == 1) ? 3 : 2;  // DDDMM vs DDDMM
                float d = Converter.ToInt(v.Substring(0, i));
                if (v.Length > i)
                {
                    d += Converter.ToInt(v.Substring(i, 2)) / 60;
                    i += 2;
                }
                if (v.Length > i)
                {
                    d += Converter.ToInt(v.Substring(i, 2)) / 3600;
                }
                return d;
            }

            var fi = NumberFormatInfo.InvariantInfo;
            if (point == 2)
            {
                return float.Parse(v, fi) * 3600;
            }
            else if (point == 4)
            {
                return float.Parse(v.Substring(0, 2), fi) * 3600 + float.Parse(v.Substring(2), fi) * 60;
            }
            else  // point==8
            {
                return float.Parse(v.Substring(0, 2), fi) * 3600 + float.Parse(v.Substring(2, 2), fi) * 60 + float.Parse(v.Substring(4), fi);
            }
        }

        public bool ParseIso(string isoStr)
        {
            // get code as string in format "+515248−1763929"
            // https://en.wikipedia.org/wiki/ISO_6709
            // https://github.com/jaime-olivares/coordinate/blob/master/Coordinate.cs

            // Parse coordinate in the following ISO 6709 formats:
            // Latitude and Longitude in Degrees:
            // �DD.DDDD�DDD.DDDD/         (eg +12.345-098.765/)
            // Latitude and Longitude in Degrees and Minutes:
            // �DDMM.MMMM�DDDMM.MMMM/     (eg +1234.56-09854.321/)
            // Latitude and Longitude in Degrees, Minutes and Seconds:
            // �DDMMSS.SSSS�DDDMMSS.SSSS/ (eg +123456.7-0985432.1/)
            // Latitude, Longitude (in Degrees) and Altitude:
            // �DD.DDDD�DDD.DDDD�AAA.AAA/         (eg +12.345-098.765+15.9/)
            // Latitude, Longitude (in Degrees and Minutes) and Altitude:
            // �DDMM.MMMM�DDDMM.MMMM�AAA.AAA/     (eg +1234.56-09854.321+15.9/)
            // Latitude, Longitude (in Degrees, Minutes and Seconds) and Altitude:
            // �DDMMSS.SSSS�DDDMMSS.SSSS�AAA.AAA/ (eg +123456.7-0985432.1+15.9/)

            if (isoStr == null || isoStr.Length < 8 || isoStr.Length > 18)  // Check for min/max length
                return false;

            isoStr = isoStr.Trim().Replace(Converter.kMinus2, '-');    // weird character.

            if (isoStr.EndsWith("/"))  // Check for trailing slash
            {
                isoStr = isoStr.Remove(isoStr.Length - 1); // Remove trailing slash
            }

            string[] parts = isoStr.Split(new char[] { '+', '-' }, StringSplitOptions.None);
            if (parts.Length < 3 || parts.Length > 4)  // Check for parts count
                return false;

            // first part must be empty!
            if (!string.IsNullOrEmpty(parts[0]))
                return false;

            int point = parts[1].IndexOf('.');
            if (point >= 0)
            {
                if (point != 2 && point != 4 && point != 6) // Check for valid length for lat/lon
                    return false;
                if (point != parts[2].IndexOf('.') - 1) // Check for lat/lon decimal positions
                    return false;
            }
            else
            {
                // based on length of string.
                int len = parts[1].Length;
                if (len < 2 || len > 7)
                    return false;
                len = parts[2].Length;
                if (len < 2 || len > 7)
                    return false;
            }

            // Parse latitude and longitude values, according to format
            Lat = ParseValue(parts[1], point);
            Lon = ParseValue(parts[2], point);

            // Add proper sign to lat/lon
            if (isoStr[0] == '-')
                Lat = -Lat;
            if (isoStr[parts[1].Length + 1] == '-')
                Lon = -Lon;

            // Parse altitude, just to check if it is valid
            if (parts.Length == 4)
            {
                // Alt = float.Parse(parts[3], NumberFormatInfo.InvariantInfo);
            }

            return true;
        }

        public static string CityStateZip(string city, string state, string zip)
        {
            // Format "city, state zip" and account for missing info in USA normal style. 
            // uses Formatter.ToTitleCase 

            string ret = Formatter.JoinTitles(",", city, state);
            if (ValidState.IsValidUnique(zip))
            {
                if (ret.Length > 0)
                    ret += " ";
                ret += zip;
            }
            return ret;
        }

        public static CountryId GetCountryFromZip(string s, CountryId eCountryDef = CountryId.ANY)
        {
            // Is this a valid ZipCode for a country ? USA, Canada

            // http://stackoverflow.com/questions/578406/what-is-the-ultimate-postal-code-and-zip-regex

            // "US"=>"^\d{5}([\-]?\d{4})?$",
            // "UK"=>"^(GIR|[A-Z]\d[A-Z\d]??|[A-Z]{2}\d[A-Z\d]??)[ ]??(\d[A-Z]{2})$",
            // "DE"=>"\b((?:0[1-46-9]\d{3})|(?:[1-357-9]\d{4})|(?:[4][0-24-9]\d{3})|(?:[6][013-9]\d{3}))\b",
            // "CA"=>"^([ABCEGHJKLMNPRSTVXY]\d[ABCEGHJKLMNPRSTVWXYZ])\ {0,1}(\d[ABCEGHJKLMNPRSTVWXYZ]\d)$",
            // "FR"=>"^(F-)?((2[A|B])|[0-9]{2})[0-9]{3}$",
            // "IT"=>"^(V-|I-)?[0-9]{5}$",
            // "AU"=>"^(0[289][0-9]{2})|([1345689][0-9]{3})|(2[0-8][0-9]{2})|(290[0-9])|(291[0-4])|(7[0-4][0-9]{2})|(7[8-9][0-9]{2})$",
            // "NL"=>"^[1-9][0-9]{3}\s?([a-zA-Z]{2})?$",
            // "ES"=>"^([1-9]{2}|[0-9][1-9]|[1-9][0-9])[0-9]{3}$",
            // "DK"=>"^([D-d][K-k])?( |-)?[1-9]{1}[0-9]{3}$",
            // "SE"=>"^(s-|S-){0,1}[0-9]{3}\s?[0-9]{2}$",
            // "BE"=>"^[1-9]{1}[0-9]{3}$"

            if (Regex.IsMatch(s, @"^\d{5}(-\d{4})?$"))
                return CountryId.USA;

            if (Regex.IsMatch(s, @"^(s-|S-){0,1}[0-9]{3}\s?[0-9]{2}$")) // pass="12345|||932 68|||S-621 46", fail="5367|||425611|||31 545"
                return CountryId.SWE;

            // Canada
            // Lower case letters are not strictly allowed. but we allow them.
            if (Regex.IsMatch(s, @"^[ABCEGHJKLMNPRSTVXYabceghjklmnprstvxy]{1}\d{1}[A-Za-z]{1} *\d{1}[A-Za-z]{1}\d{1}$"))
                return CountryId.CAN;

            // Australian postal code verification. 
            // Australia has 4-digit numeric postal codes with the following state based specific ranges. ACT: 0200-0299 and 2600-2639. NSW: 1000-1999, 2000-2599 and 2640-2914. NT: 0900-0999 and 0800-0899. QLD: 9000-9999 and 4000-4999. SA: 5000-5999. TAS: 7800-7999 and 7000-7499. VIC: 8000-8999 and 3000-3999. WA: 6800-6999 and 6000-6799
            if (Regex.IsMatch(s, @"^(0[289][0-9]{2})|([1345689][0-9]{3})|(2[0-8][0-9]{2})|(290[0-9])|(291[0-4])|(7[0-4][0-9]{2})|(7[8-9][0-9]{2})$"))
                return CountryId.AUS;

            // Look for bad chars or bad length that no valid zip would use?
            return CountryId.ANY;   // no idea what country this might be.
        }

        public static StateId GetStateFromZipUS(int zip)
        {
            // Convert US Zip codes to StateId. 

            if ((zip >= 600 && zip <= 799) || (zip >= 900 && zip <= 999)) // Puerto Rico (00600-00799 and 900--00999 ranges)
                return StateId.PR;
            else if (zip >= 800 && zip <= 899) // US Virgin Islands (00800-00899 range)            
                return StateId.VI;
            else if (zip >= 1000 && zip <= 2799) // Massachusetts (01000-02799 range)
                return StateId.MA;
            else if (zip >= 2800 && zip <= 2999) // Rhode Island (02800-02999 range)
                return StateId.RI;
            else if (zip >= 3000 && zip <= 3899) // New Hampshire (03000-03899 range)
                return StateId.NH;
            else if (zip >= 3900 && zip <= 4999) // Maine (03900-04999 range)
                return StateId.ME;
            else if (zip >= 5000 && zip <= 5999) // Vermont (05000-05999 range)
                return StateId.VT;
            else if ((zip >= 6000 && zip <= 6999) && zip != 6390) // Connecticut (06000-06999 range excluding 6390)
                return StateId.CT;
            else if (zip >= 70000 && zip <= 8999) // New Jersey (07000-08999 range)
                return StateId.NJ;
            else if ((zip >= 10000 && zip <= 14999) || zip == 6390 || zip == 501 || zip == 544) // New York (10000-14999 range and 6390, 501, 544)
                return StateId.NY;
            else if (zip >= 15000 && zip <= 19699) // Pennsylvania (15000-19699 range)
                return StateId.PA;
            else if (zip >= 19700 && zip <= 19999) // Delaware (19700-19999 range)
                return StateId.DE;
            else if ((zip >= 20000 && zip <= 20099) || (zip >= 20200 && zip <= 20599) || (zip >= 56900 && zip <= 56999)) // District of Columbia (20000-20099, 20200-20599, and 56900-56999 ranges)
                return StateId.DC;
            else if (zip >= 20600 && zip <= 21999) // Maryland (20600-21999 range)            
                return StateId.MD;
            else if ((zip >= 20100 && zip <= 20199) || (zip >= 22000 && zip <= 24699)) // Virginia (20100-20199 and 22000-24699 ranges, also some taken from 20000-20099 DC range)
                return StateId.VA;
            else if (zip >= 24700 && zip <= 26999) // West Virginia (24700-26999 range)
                return StateId.WV;
            else if (zip >= 27000 && zip <= 28999) // North Carolina (27000-28999 range)
                return StateId.NC;
            else if (zip >= 29000 && zip <= 29999) // South Carolina (29000-29999 range)            
                return StateId.SC;
            else if ((zip >= 30000 && zip <= 31999) || (zip >= 39800 && zip <= 39999)) // Georgia (30000-31999, 39901[Atlanta] range)
                return StateId.GA;
            else if (zip >= 32000 && zip <= 34999) // Florida (32000-34999 range)
                return StateId.FL;
            else if (zip >= 35000 && zip <= 36999) // Alabama (35000-36999 range)
                return StateId.AL;
            else if (zip >= 37000 && zip <= 38599) // Tennessee (37000-38599 range)
                return StateId.TN;
            else if (zip >= 38600 && zip <= 39799) // Mississippi (38600-39999 range)
                return StateId.MS;
            else if (zip >= 40000 && zip <= 42799) // Kentucky (40000-42799 range)
                return StateId.KY;
            else if (zip >= 43000 && zip <= 45999) // Ohio (43000-45999 range)
                return StateId.OH;
            else if (zip >= 46000 && zip <= 47999) // Indiana (46000-47999 range)
                return StateId.IN;
            else if (zip >= 48000 && zip <= 49999) // Michigan (48000-49999 range)
                return StateId.MI;
            else if (zip >= 50000 && zip <= 52999) // Iowa (50000-52999 range)
                return StateId.IA;
            else if (zip >= 53000 && zip <= 54999) // Wisconsin (53000-54999 range)
                return StateId.WI;
            else if (zip >= 55000 && zip <= 56799) // Minnesota (55000-56799 range)
                return StateId.MN;
            else if (zip >= 57000 && zip <= 57999) // South Dakota (57000-57999 range)
                return StateId.SD;
            else if (zip >= 58000 && zip <= 58999) // North Dakota (58000-58999 range)
                return StateId.ND;
            else if (zip >= 59000 && zip <= 59999) // Montana (59000-59999 range)
                return StateId.MT;
            else if (zip >= 60000 && zip <= 62999) // Illinois (60000-62999 range)
                return StateId.IL;
            else if (zip >= 63000 && zip <= 65999) // Missouri (63000-65999 range)
                return StateId.MO;
            else if (zip >= 66000 && zip <= 67999) // Kansas (66000-67999 range)
                return StateId.KS;
            else if (zip >= 68000 && zip <= 69999) // Nebraska (68000-69999 range)
                return StateId.NE;
            else if (zip >= 70000 && zip <= 71599) // Louisiana (70000-71599 range)
                return StateId.LA;
            else if (zip >= 71600 && zip <= 72999) // Arkansas (71600-72999 range)
                return StateId.AR;
            else if (zip >= 73000 && zip <= 74999) // Oklahoma (73000-74999 range)
                return StateId.OK;
            else if ((zip >= 75000 && zip <= 79999) || (zip >= 88500 && zip <= 88599)) // Texas (75000-79999 and 88500-88599 ranges)
                return StateId.TX;
            else if (zip >= 80000 && zip <= 81999) // Colorado (80000-81999 range)
                return StateId.CO;
            else if (zip >= 82000 && zip <= 83199) // Wyoming (82000-83199 range)
                return StateId.WY;
            else if (zip >= 83200 && zip <= 83999) // Idaho (83200-83999 range)
                return StateId.ID;
            else if (zip >= 84000 && zip <= 84999) // Utah (84000-84999 range)
                return StateId.UT;
            else if (zip >= 85000 && zip <= 86999) // Arizona (85000-86999 range)
                return StateId.AZ;
            else if (zip >= 87000 && zip <= 88499) // New Mexico (87000-88499 range)
                return StateId.NM;
            else if (zip >= 88900 && zip <= 89999) // Nevada (88900-89999 range)
                return StateId.NV;
            else if (zip >= 90000 && zip <= 96199) // California (90000-96199 range)
                return StateId.CA;
            else if (zip >= 96700 && zip <= 96899) // Hawaii (96700-96899 range)  
                return StateId.HI;
            else if (zip >= 97000 && zip <= 97999) // Oregon (97000-97999 range)
                return StateId.OR;
            else if (zip >= 98000 && zip <= 99499) // Washington (98000-99499 range)
                return StateId.WA;
            else if (zip >= 99500 && zip <= 99999) // Alaska (99500-99999 range)
                return StateId.AK;

            return StateId.UNK;
        }

        public static StateId GetStateFromZip(string zip)
        {
            string[] temp = zip.Split('-');

            if (GetCountryFromZip(temp[0]) == CountryId.USA)
                return GetStateFromZipUS(Converter.ToInt(temp[0]));

            return StateId.UNK;
        }

        public static StateId GetStateId(string stateStr)
        {
            // Get StateId from string.

            StateId id = EnumUtil.ParseEnum2<StateId>(stateStr);
            if (id != 0)
                return id;

            return StateId.UNK;
        }
    }
}
