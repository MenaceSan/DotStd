using System;
using System.Threading.Tasks;

namespace DotStd
{
    public enum MonthId : byte
    {
        // Month of the year.
        // Oddly .NET System doesn't define months.
        // like Microsoft.VisualBasic.MonthName
        // Use cultural description from DateTime.ToString("MMM", CultureInfo.InvariantCulture)

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

    public static class DateUtil
    {
        // Utility for date and time.
        // System.DayOfWeek (same as Javascript) = Sunday is 0, Monday is 1,
        // NOTE: DayOfWeek is different for MSSQL and MySQL db time functions.

        public static readonly DateTime kExtremeMin = new DateTime(1800, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);  // reasonably inclusive min date that can be held by most db's. BUT NOT MS SQL smalldate
        // public static readonly DateTime kExtremeMin2 = new DateTime(1800, 1, 1); // MySQL doesnt like the UTC stuff ?? null !!! "Unable to cast object of type 'System.String' to type 'System.DateTime'."

        public static readonly DateTime kUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);     // JavaScript epoch.
        public static readonly DateTime kExtremeMax = new DateTime(2179, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);  // reasonably inclusive max date that can be held by most db's.  

        public const int kHoursInWeek = 168;        // (7*24)
        public const int kMinutesInDay = 3600;      // 24*60 // Modulus for day time.

        public const string kShortDate = "yyyy-dd-MM";  // default short data format. Similar to ToShortDateString() or ToString("d") (cultural)

        public static bool IsExtremeDate(DateTime dt)
        {
            // Is this probably a useless date? NOTE: DateTime is not nullable so use this as null.
            // e.g. Year <= 1
            return dt <= kExtremeMin || dt >= kExtremeMax;
        }
        public static bool IsExtremeDate(DateTime? dt)
        {
            if (dt == null)
                return true;
            return IsExtremeDate(dt.Value);
        }

        public static double ToJavaTime(DateTime dt)
        {
            // JavaScript time stamp is milliseconds past kUnixEpoch
            if (IsExtremeDate(dt))
                return 0;
            return (dt - kUnixEpoch).TotalMilliseconds;
        }
        public static DateTime FromJavaTime(double javaTimeStamp)
        {
            // JavaScript time stamp is milliseconds past epoch. NOT seconds like Unix time.
            // Return UTC time.
            return kUnixEpoch.AddMilliseconds(javaTimeStamp);
        }

        public static string TimeSpanStr(TimeSpan ts)
        {
            // get a string for a rough amount of time ago or ahead.
            // var ts = (yourDate - DateTime.UtcNow);

            const int kSECOND = 1;
            const int kMINUTE = 60 * kSECOND;
            const int kHOUR = 60 * kMINUTE;
            const int kDAY = 24 * kHOUR;
            const int kMONTH = 30 * kDAY;   // approximate.

            long deltaSec = (long) ts.TotalSeconds;
            bool inPast = deltaSec < 0;
            string ago = "";
            if (inPast)
            {
                deltaSec = -deltaSec;
                ts = new TimeSpan(-ts.Ticks);
                ago = " ago";
            }

            if (deltaSec < 1)
                return "now";

            if (deltaSec < kMINUTE)
            {
                if (deltaSec == 1)
                    return "one second" + ago;
                return deltaSec + " seconds" + ago;
            }

            if (deltaSec < 2 * kMINUTE)
                return "a minute" + ago;

            if (deltaSec < 45 * kMINUTE)
                return (deltaSec/ kMINUTE) + " minutes" + ago;

            if (deltaSec < 90 * kMINUTE)
                return "an hour" + ago;

            if (deltaSec < 24 * kHOUR)
                return (deltaSec / kHOUR) + " hours" + ago;

            if (deltaSec < 48 * kHOUR)
            {
                return inPast ? "yesterday" : "tomorrow";
            }

            if (deltaSec < 30 * kDAY)
                return (deltaSec / kDAY) + " days" + ago;

            if (deltaSec < 12 * kMONTH)    // approximate.
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                if (months <= 1)
                    return "one month" + ago;
                return months + " months" + ago;
            }

            int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365)); // approximate.
            if (years > 200)
            {
                return "never";
            }

            if (years <= 1)
                return "one year" + ago;

            return years + " years" + ago;
        }

        public static async Task<string> TimeSpanStrAsync(TimeSpan ts, ITranslatorProvider1 trans)
        {
            return await trans.TranslateAsync(TimeSpanStr(ts));
        }

        public static string TimeMsecStr(long mSec)
        {
            // How much time in mSec.
            // the code that you want to measure comes here
            // var watch = System.Diagnostics.Stopwatch.StartNew(); STUFF(); watch.Stop(); TimeUtil.TimeMsecStr(watch.ElapsedMilliseconds);

            if (mSec < 1000)
            {
                return mSec.ToString() + " ms";
            }
            return (((decimal)mSec) / 1000m).ToString() + " s"; ;
        }

        public static DayOfWeek ModDOW(DayOfWeek dow)
        {
            // modulus/wrap to force into valid range. 0 to 6 day of week.

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

        public static DateTime GetDateOfWeekDayPrev(DateTime dt, DayOfWeek dow)
        {
            // Get DateTime for previous dow (day of week) inclusive of current day.
            // Related to similar to cultureInfo.DateTimeFormat.FirstDayOfWeek

            int diff = dt.DayOfWeek - dow;
            if (diff < 0)   // DayOfWeek.Sunday = 0
            {
                diff += 7;
            }
            // ASSERT( diff >= 0 );
            return dt.Date.AddDays(-diff);  // go back. always 0 or negative.
        }

        public static DateTime GetDateOfWeekDayPrevX(DateTime dt, DayOfWeek dow)
        {
            // Get previous dow (day of week) NOT including dt.
            return GetDateOfWeekDayPrev(dt.AddDays(-1), dow);
        }

        public static DateTime GetDateOfWeekDayNext(DateTime dt, DayOfWeek dow)
        {
            // Get DateTime for next dow (day of week) inclusive of current day.
            // like GetDateOfWeekDayPrev but get next week.
            // Related to similar to cultureInfo.DateTimeFormat.FirstDayOfWeek

            int diff = dow - dt.DayOfWeek;
            if (diff < 0)   // DayOfWeek.Sunday = 0
            {
                diff += 7;
            }
            // ASSERT( diff >= 0 );
            return dt.Date.AddDays(diff);  // go forward.
        }

        public static DayOfWeek GetDOW(string s)
        {
            // Get DoW from string as Enum.
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

        public static int GetTimeMinutes(string s)
        {
            // Convert a string in format "10:45" to minutes in the day.
            // Assume military time if no AM, PM

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

        public static string GetTimeStr(int minutes, bool ampm = false, bool space = true)
        {
            // Convert (0 based) minutes in the day to a military (or AMPM) time string.
            // if (minutes > DateUtil.kMinutesInDay) then just wrap.

            if (minutes < 0)
                return null;
            int hours = (minutes / 60) % 24;
            minutes %= 60;
            if (ampm)
            {
                ampm = hours < 12;
                hours %= 12;
                if (hours == 0)
                    hours = 12;
                return string.Format("{0:D2}:{1:D2}{2}{3}", hours, minutes, space ? " " : "", ampm ? "AM" : "PM");
            }
            else
            {
                // ignore hours > 24? or hours %= 24;
                return string.Format("{0:D2}:{1:D2}", hours, minutes);
            }
        }

        public static string GetTimeStr(TimeSpan ts, bool ampm = false, bool space = true)
        {
            return GetTimeStr((int)ts.TotalMinutes, ampm, space);
        }
    }
}
