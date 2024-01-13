using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Month of the year. Oddly .NET System doesn't define months.
    /// like Microsoft.VisualBasic.MonthName
    /// Use cultural description from DateTime.ToString("MMM", CultureInfo.InvariantCulture)
    /// </summary>
    public enum MonthId : byte
    {
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    };

    /// <summary>
    /// Utility for date and time.
    /// System.DayOfWeek (same as Javascript) = Sunday is 0, Monday is 1,
    /// NOTE: DayOfWeek is different for MSSQL and MySQL db time functions.
    /// </summary>
    public static class DateUtil
    {
        public static readonly DateTime kExtremeMax = new(2179, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);  // reasonably inclusive max date that can be held by most db's. TODO MORE INFO ?
        public static readonly DateTime kExtremeMin = new(1800, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);  // reasonably inclusive min date that can be held by most db's. BUT NOT MS SQL smalldate
        // public static readonly DateTime kExtremeMin2 = new(1800, 1, 1); // MySQL doesn't like the UTC stuff ?? null !!! "Unable to cast object of type 'System.String' to type 'System.DateTime'."

        public static readonly DateTime kUnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);     // JavaScript epoch.

        public const int kHoursInWeek = 168;        // (7*24)
        public const int kMinutesInDay = 3600;      // 24*60 // Modulus for day time.

        public const string kISO = "yyyy-MM-dd hh:mm:ss";    // date and time. ISO 8601
        public const string kISODate = "yyyy-MM-dd";  // default short data format ALA JavaScript, ISO Date. NOT ToShortDateString()/ToString("d") which are cultural.
        public const string kShortDate2 = "MM/dd/yyyy"; // Weird format to be avoided. US local?

        /// <summary>
        /// Is this a probably useless date? 
        /// NOTE: DateTime is not nullable so use this acts like null.
        /// e.g. Year <= 1
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool IsExtremeDate(DateTime dt)
        {
            return dt <= kExtremeMin || dt >= kExtremeMax;
        }
        /// <summary>
        /// Is this a probably useless date? or null.
        /// </summary>
        public static bool IsExtremeDate([NotNullWhen(false)] DateTime? dt)
        {
            if (dt == null)
                return true;
            return IsExtremeDate(dt.Value);
        }

        /// <summary>
        /// JavaScript time stamp is milliseconds past kUnixEpoch
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>milliseconds</returns>
        public static double ToJavaTime(DateTime dt)
        {
            if (IsExtremeDate(dt))
                return 0;
            return (dt - kUnixEpoch).TotalMilliseconds;
        }

        /// <summary>
        /// JavaScript time stamp is milliseconds past epoch. NOT seconds like Unix time.
        /// </summary>
        /// <param name="javaTimeStamp"></param>
        /// <returns>UTC time.</returns>
        public static DateTime FromJavaTime(double javaTimeStamp)
        {
            return kUnixEpoch.AddMilliseconds(javaTimeStamp);
        }

        /// <summary>
        /// get a string for a rough amount of time ago or ahead.
        /// get a translatable approximate span.
        /// var ts = (yourDate - TimeNow.Utc);
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static string TimeSpanStr2(TimeSpan ts, out string? arg)
        {
            const int kSECOND = 1;
            const int kMINUTE = 60 * kSECOND;
            const int kHOUR = 60 * kMINUTE;
            const int kDAY = 24 * kHOUR;
            const int kMONTH = 30 * kDAY;   // approximate.

            long deltaSec = (long)ts.TotalSeconds;
            bool inPast = deltaSec < 0;
            string ago = "";
            if (inPast)
            {
                deltaSec = -deltaSec;
                ts = new TimeSpan(-ts.Ticks);
                ago = " ago";
            }

            if (deltaSec < 1)
            {
                arg = null;
                return "now";
            }

            if (deltaSec < kMINUTE)
            {
                if (deltaSec == 1)
                {
                    arg = null;
                    return "one second" + ago;
                }

                arg = deltaSec.ToString();
                return "{0} seconds" + ago;
            }

            if (deltaSec < 2 * kMINUTE)
            {
                arg = null;
                return "a minute" + ago;
            }

            if (deltaSec < 45 * kMINUTE)
            {
                arg = (deltaSec / kMINUTE).ToString();
                return "{0} minutes" + ago;
            }

            if (deltaSec < 90 * kMINUTE)
            {
                arg = null;
                return "an hour" + ago;
            }

            if (deltaSec < 24 * kHOUR)
            {
                arg = (deltaSec / kHOUR).ToString();
                return "{0} hours" + ago;
            }

            if (deltaSec < 48 * kHOUR)
            {
                arg = null;
                return inPast ? "yesterday" : "tomorrow";
            }

            if (deltaSec < 30 * kDAY)
            {
                arg = (deltaSec / kDAY).ToString();
                return "{0} days" + ago;
            }

            if (deltaSec < 12 * kMONTH)    // approximate.
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                if (months <= 1)
                {
                    arg = null;
                    return "one month" + ago;
                }

                arg = months.ToString();
                return "{0} months" + ago;
            }

            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365)); // approximate.
            if (years > 200)
            {
                arg = null;
                return "never";
            }

            if (years <= 1)
            {
                arg = null;
                return "one year" + ago;
            }

            arg = years.ToString();
            return "{0} years" + ago;
        }

        public static string TimeSpanStr(TimeSpan ts)
        {
            string txt = TimeSpanStr2(ts, out string? arg);
            if (arg == null)
                return txt;
            return string.Format(txt, arg);
        }

        public static async Task<string> TimeSpanStrAsync(TimeSpan ts, ITranslatorProvider1 trans)
        {
            // a translated time span
            string txt = await trans.TranslateAsync(TimeSpanStr2(ts, out string? arg));
            if (arg == null)
                return txt;
            return string.Format(txt, arg);
        }

        /// <summary>
        /// Describe How much time in mSec. 
        /// e.g. var watch = System.Diagnostics.Stopwatch.StartNew(); STUFF(); watch.Stop(); TimeUtil.TimeMsecStr(watch.ElapsedMilliseconds);
        /// </summary>
        /// <param name="mSec"></param>
        /// <returns></returns>
        public static string TimeMsecStr(long mSec)
        {
            if (mSec < 1000)
            {
                return mSec.ToString() + " ms";
            }
            return (((decimal)mSec) / 1000m).ToString() + " s"; 
        }

        /// <summary>
        /// modulus/wrap to force into valid range. 0 to 6 day of week.
        /// </summary>
        /// <param name="dow"></param>
        /// <returns></returns>
        public static DayOfWeek ModDOW(DayOfWeek dow)
        {
            int d = (int)dow;
            if (d > 6)
            {
                d %= 7;
            }
            else if (d < 0)
            {
                d %= 7;
                d += 7;
            }
            return (System.DayOfWeek)d;
        }

        /// <summary>
        /// Get DateTime for previous dow (day of week) inclusive of current day.
        /// Related to similar to cultureInfo.DateTimeFormat.FirstDayOfWeek
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="dow"></param>
        /// <returns></returns>
        public static DateTime GetDateOfWeekDayPrev(DateTime dt, DayOfWeek dow)
        {
            int diff = dt.DayOfWeek - dow;
            if (diff < 0)   // DayOfWeek.Sunday = 0
            {
                diff += 7;
            }
            // ASSERT( diff >= 0 );
            return dt.Date.AddDays(-diff);  // go back. always 0 or negative.
        }

        /// <summary>
        /// Get previous dow (day of week) NOT including dt.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="dow"></param>
        /// <returns></returns>
        public static DateTime GetDateOfWeekDayPrevX(DateTime dt, DayOfWeek dow)
        {
            return GetDateOfWeekDayPrev(dt.AddDays(-1), dow);
        }

        /// <summary>
        /// Get DateTime for next dow (day of week) inclusive of current day.
        /// like GetDateOfWeekDayPrev but get next week.
        /// Related to similar to cultureInfo.DateTimeFormat.FirstDayOfWeek
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="dow"></param>
        /// <returns></returns>
        public static DateTime GetDateOfWeekDayNext(DateTime dt, DayOfWeek dow)
        {
            int diff = dow - dt.DayOfWeek;
            if (diff < 0)   // DayOfWeek.Sunday = 0
            {
                diff += 7;
            }
            // ASSERT( diff >= 0 );
            return dt.Date.AddDays(diff);  // go forward.
        }

        /// <summary>
        /// Get DoW from English string as Enum.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static DayOfWeek GetDOW(string s)    // NOT USED
        {
            if (s != null)
            {
                switch (s.ToLower())
                {
                    case "sunday":
                    case "sun":
                    case "su":
                        return DayOfWeek.Sunday;
                    case "monday":
                    case "mon":
                    case "m":
                        return DayOfWeek.Monday;
                    case "tuesday":
                    case "tues":
                    case "tue":
                    case "t":
                        return DayOfWeek.Tuesday;
                    case "wednesday":
                    case "wed":
                    case "w":
                        return DayOfWeek.Wednesday;
                    case "thursday":
                    case "thurs":
                    case "thu":
                    case "th":
                        return DayOfWeek.Thursday;
                    case "friday":
                    case "fri":
                    case "f":
                        return DayOfWeek.Friday;
                    case "saturday":
                    case "sat":
                    case "sa":
                        return DayOfWeek.Saturday;
                }
            }
            return DayOfWeek.Monday;        // no idea. -1 ?
        }

        /// <summary>
        /// Convert a string in format "10:45" to minutes in the day.
        /// Assume military time if no AM, PM
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int GetTimeMinutes(string s)
        {
            string[] parts = s.Split(':');
            if (parts.Length == 0)
                return -1;

            int minutes = 0;
            int hours = Converter.ToInt(parts[0]);
            if (parts.Length > 1)
            {
                minutes = Converter.ToIntSloppy(parts[1]);
            }

            if (s.EndsWith("AM") || s.EndsWith("am"))
            {
                if (hours == 12)
                    hours = 0; // midnight.
            }
            else if (s.EndsWith("PM") || s.EndsWith("pm"))
            {
                if (hours < 12)
                    hours += 12;    // after noon.
            }
            else if (parts.Length == 1)
            {
                minutes = hours;
                hours = 0;
            }

            return hours * 60 + minutes;
        }

        /// <summary>
        /// Convert (0 based) minutes in the day to a military (or AM/PM) time string.
        /// if (minutes > DateUtil.kMinutesInDay) then just wrap to next day.
        /// </summary>
        /// <param name="minutesInDay">(0 based) minutes in the day</param>
        /// <param name="ampm">Culture?</param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static string GetTimeStr(int minutesInDay, bool ampm = false, bool space = true)
        {
            if (minutesInDay < 0)
                return string.Empty;    // this should never happen!
            int hours = (minutesInDay / 60) % 24;
            minutesInDay %= 60;
            if (ampm)
            {
                ampm = hours < 12;
                hours %= 12;
                if (hours == 0)
                    hours = 12;
                return string.Format("{0:D2}:{1:D2}{2}{3}", hours, minutesInDay, space ? " " : "", ampm ? "AM" : "PM");
            }
            else
            {
                // ignore hours > 24? or hours %= 24;
                return string.Format("{0:D2}:{1:D2}", hours, minutesInDay);
            }
        }

        public static string GetTimeStr(TimeSpan ts, bool ampm = false, bool space = true)
        {
            return GetTimeStr((int)ts.TotalMinutes, ampm, space);
        }

        /// <summary>
        /// format DateTime as a string.
        /// ASSUME timeZone is already accounted for ???
        /// </summary>
        /// <param name="dt">DateTime</param>
        /// <param name="format">"M/d/yyyy" or default = 'yyyy-MM-dd' (for ISO 8601)</param>
        /// <param name="culture">Use format from culture for fancy string formatting.</param>
        /// <param name="def">use this if its a bad DateTime.</param>
        /// <returns></returns>
        public static string GetDtStr(DateTime dt, string? format=null, IFormatProvider? culture = null, string? def = null)
        {
            if (def == null)
                def = string.Empty;
            if (dt.IsExtremeDate())    // Const.dateExtremeMin
                return def;     // null or "" ??
            if (format == null)
                format = DateUtil.kISO;       // "yyyy-MM-dd" = ISO
            if (culture != null)
                return dt.ToString(format, culture);
            return dt.ToString(format);
        }
    }
}
