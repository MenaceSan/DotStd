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
        International = 1,

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

    [Serializable]
    public class GeoLocation
    {
        // Latitude and longitude. Same format as JavaScript navigator.geolocation.getCurrentPosition() coords
        // maybe altitude ?

        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public static bool IsValidLat(float x)
        {
            return x >= -90 && x <= 90;
        }
        public static bool IsValidLon(float x)
        {
            return x >= -180 && x <= 180;
        }
        public bool IsValid
        {
            get
            {
                return IsValidLat(Latitude) && IsValidLon(Longitude);
            }
        }

        public override string ToString()
        {
            // Composite string.
            // e.g. "15.0N+30.0E"
            // https://maps.google.com/maps?q=24.197611,120.780512
            // https://maps.google.com/maps?q=24.197611,120.780512&z=18
 
            return String.Concat(Latitude.ToString(),",",Longitude.ToString());
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
            Latitude = ParseValue(parts[1], point);
            Longitude = ParseValue(parts[2], point);

            // Add proper sign to lat/lon
            if (isoStr[0] == '-')
                Latitude = -Latitude;
            if (isoStr[parts[1].Length + 1] == '-')
                Longitude = -Longitude;

            // Parse altitude, just to check if it is valid
            if (parts.Length == 4)
            {
                // Alt = float.Parse(parts[3], NumberFormatInfo.InvariantInfo);
            }

            return true;
        } 
    }
}
