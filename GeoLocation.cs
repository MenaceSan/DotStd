using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DotStd
{
    /// <summary>
    /// ISO codes for Countries that we care about. CountryCode. NOT the same as calling/dialing codes.
    /// from the table geo_country
    /// https://www.ncbi.nlm.nih.gov/books/NBK7249/
    /// https://www.worldatlas.com/aatlas/ctycodes.htm A2 (ISO), A3 (UN), NUM (UN), DIALING CODE
    /// </summary>
    [Serializable()]
    public enum CountryId
    {
        ANY = 0,    // Don't care. give me all.

        [Description("International")]
        III = 1,

        [Description("United States")]
        USA = 840,    // US, us
        [Description("Sweden")]
        SWE = 752,    // SE, se 
        [Description("Canada")]
        CAN = 124,    // CA, ca
        [Description("Australia")]
        AUS = 36,    // AU, au

        // More ...
        Custom = 1000,  // indicate this is a hybrid table. it can grow.
    }

    /// <summary>
    /// Custom code for States/Provinces in a country we care about (USA first)
    /// from the table geo_state
    /// https://www.50states.com/abbreviations.htm
    /// </summary>
    [Serializable()]
    public enum GeoStateId
    {
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
        NV,
        NH,
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

        // More ... in other countries. Manually or Db added.
        Custom = 10000, // indicate this can grow.
    }

    /// <summary>
    /// Latitude and longitude.
    /// Same format as JavaScript navigator.geolocation.getCurrentPosition() coords
    /// Can be serialized directly from the JSON poco.
    /// </summary>
    [Serializable]
    public class GeoLocation
    {
        public double Latitude { get; set; }     // AKA latitude. degrees (360).
        public double Longitude { get; set; }    // AKA longitude. degrees (360).
        public float? Altitude { get; set; }     // optional AKA altitude. meters.

        public const int kLonMax = 180;

        // https://stackoverflow.com/questions/1220377/latitude-longitude-storage-and-compression-in-c
        // The circumference of the Earth is approx. 40.000 km or 24900 miles. You need one-meter accuracy(3ft) to be able to out-resolve gps precision by an order of magnitude. Therefore you need precision to store 40.000.000 different values. That's at minimum 26 bits of information.
        // NOT float storage => a 32-bit IEEE float has 23 explicit bits of fraction (and an assumed 1) for 24 effective bits of significand. That is only capable of distinguishing 16 million unique values, of the 40 million required. 
        public const int kIntMult = 10000000;    // Convert back and forth to 32 bit int. (~.1m res, i.e. more than needed)
        public const double kIntDiv = 0.0000001;    // Convert back and forth to 32 bit int. (~.1m res, i.e. more than needed)

        public const double kEarthRadiusMeters = 6371000.0;    // Approximate. or 6378137 ?
        public const int kEarthDistMax = 50000000;    // Max reasonable distance on Earth. Any distance greater is not on earth. (40,075 km circumference)

        public const double kDeg2Rad = Math.PI / 180.0;    // multiply for degrees to Radians 

        // https://gis.stackexchange.com/questions/142326/calculating-longitude-length-in-miles
        public const double kMet2Deg = 111000.0;      // Meters to degrees. approximate for reverse Haversine.
        public const double kDeg2Met = 0.000001;      // ~1m in degrees. reverse of kMet2Deg. approximate for reverse Haversine.

        public static bool IsValidLat(double x)
        {
            return x >= -90 && x <= 90;
        }
        public static bool IsValidLon(double x)
        {
            return x >= -180 && x <= kLonMax;
        }
        public bool IsValid
        {
            get
            {
                // if (Latitude == 0 && Longitude == 0) return false; // TZ UTC ?
                return IsValidLat(Latitude) && IsValidLon(Longitude);
            }
        }
        public bool IsExtreme
        {
            // !IsValid or May be Valid but not normal value. Might be bad value.
            // NOTE: 0,0 can be considered invalid. It is in the Gulf of Guinea in the Atlantic Ocean, about 380 miles (611 kilometers) south of Ghana 
            get
            {
                if (Latitude <= -90 || Latitude >= 90) // Poles are extreme.
                    return true;
                if (!IsValidLon(Longitude))
                    return true;
                return Latitude == 0 && Longitude == 0;  // Extreme point in the Atlantic.
            }
        }

        public string GetLatLonStr()
        {
            // Composite string.
            // e.g. "15.0N+30.0E"
            // https://maps.google.com/maps?q=24.197611,120.780512
            // https://maps.google.com/maps?q=24.197611,120.780512&z=18

            return String.Concat(Latitude.ToString(), ",", Longitude.ToString());
        }

        public override string ToString()
        {
            return GetLatLonStr();
        }

        public bool IsEqual2(GeoLocation? x)
        {
            if (x == null)
                return false;
            return Latitude == x.Latitude && Longitude == x.Longitude;
        }

        /// <summary>
        /// Get OpenStreet Map URL
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public static string ToGeoUrlOsm(double lat, double lon)
        {
            return "http://www.openstreetmap.org/?mlat=" + lat + "&mlon=" + lon;
        }
        /// <summary>
        /// Get Google Map URL
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public static string ToGeoUrlGoo(double lat, double lon)
        {
            return "https://www.google.com/maps/search/?api=1&query=" + lat + "," + lon;
        }

        /// <summary>
        /// Get Map URL
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="deviceTypeId">classify device we are displaying on</param>
        /// <returns></returns>
        public static string ToGeoUrl(double lat, double lon, DeviceTypeId deviceTypeId)
        {
            if (deviceTypeId == DeviceTypeId.Unknown || deviceTypeId == DeviceTypeId.Windows10)
            {
                return ToGeoUrlGoo(lat, lon); // Unknown/Windows
            }
            if (deviceTypeId == DeviceTypeId.iOS)
            {
                return "maps://maps.google.com/maps?daddr=" + lat + "," + lon + "&amp;ll=";  // iOS
            }
            return "geo:" + lat + "," + lon;
        }

        public static int ToInt(double value)
        {
            // Convert degrees (360) to int.
            return (int)(value * kIntMult);
        }
        public static int? ToInt(double? value)
        {
            // Convert degrees (360) to int.
            if (value == null)
                return null;
            return ToInt(value.Value);
        }
        public static double ToDouble(int value)
        {
            // Convert int to degrees (360).
            return value * kIntDiv;
        }
        public static double? ToDouble(int? value)
        {
            // Convert int to degrees (360).
            if (value == null)
                return null;
            return ToDouble(value.Value);
        }

        public int GetLatInt()
        {
            return ToInt(this.Latitude);
        }
        public int GetLonInt()
        {
            return ToInt(this.Longitude);
        }

        public static bool IsNear(double value1, double value2, double range)
        {
            return Math.Abs(value1 - value2) < range;
        }

        public double GetDistance(double latitude, double longitude)
        {
            // Calculate the Haversine distance between this and that.
            // Haversine. // http://en.wikipedia.org/wiki/Haversine_formula

            var su = Math.Sin((this.Latitude - latitude) * 0.5 * kDeg2Rad);
            var sv = Math.Sin((this.Longitude - longitude) * 0.5 * kDeg2Rad);

            return 2.0 * kEarthRadiusMeters * Math.Asin(Math.Sqrt((su * su) + (Math.Cos(latitude * kDeg2Rad) * Math.Cos(this.Latitude * kDeg2Rad) * sv * sv)));
        }

        public GeoLocation GetMove(double bearing, double distance)
        {
            // Implementation of the reverse of Haversine formula.  https://gist.github.com/shayanjm/451a3242685225aa934b
            // Takes one set of latitude/longitude as a start point, a bearing, and a distance, and returns the resultant lat/long pair.
            // bearing in radians.
            // distance in meters.

            // TODO

            return new GeoLocation { };
        }

        public static double ParseValue(string v, int point)
        {
            // Parse a single dimension value string to a double.

            if (point <= 0) // no decimal place.
            {
                int i = ((v.Length & 1) == 1) ? 3 : 2;  // DDDMM vs DDDMM
                double d = Converter.ToInt(v.Substring(0, i));
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
                return double.Parse(v, fi) * 3600;
            }
            else if (point == 4)
            {
                return double.Parse(v.Substring(0, 2), fi) * 3600 + double.Parse(v.Substring(2), fi) * 60;
            }
            else  // point==8
            {
                return double.Parse(v.Substring(0, 2), fi) * 3600 + double.Parse(v.Substring(2, 2), fi) * 60 + double.Parse(v.Substring(4), fi);
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
                Altitude = float.Parse(parts[3], NumberFormatInfo.InvariantInfo);
            }

            return true;
        }

        public GeoLocation()
        {
        }
        public GeoLocation(double lat, double lon, float? alt = null)
        {
            Latitude = lat; Longitude = lon; Altitude = alt;
        }
    }

    [Serializable]
    public class GeoLocation5 : GeoLocation
    {
        // All the optional extra JSON stuff.
        // Same format as JavaScript navigator.geolocation.getCurrentPosition().coords
        // Can be serialized directly from the JSON poco. 

        public float accuracy;
        public float? altitudeAccuracy;

        public float? heading;
        public float? speed;
    }
}
