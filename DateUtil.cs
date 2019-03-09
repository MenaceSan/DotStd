using System;

namespace DotStd
{
    public static class DateUtil
    {
        // Util for date and time.
        // System.DayOfWeek (same as Javascript) = Sunday is 0, Monday is 1,
        // NOTE: DayOfWeek is different for MSSQL db time functions? MySQL ?

        public enum Months : byte
        {
            // Oddly .NET System doesn't have this.
            // like Microsoft.VisualBasic.MonthName
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

        public static readonly DateTime kExtremeMin = new DateTime(1800, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);  // reasonably inclusive min date that can be held by most db's. BUT NOT MS SQL smalldate
        public static readonly DateTime kUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);     // JavaScript
        public static readonly DateTime kExtremeMax = new DateTime(2179, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);  // reasonably inclusive max date that can be held by most db's.
        public const int kHoursInWeek = 168;

        public static bool IsExtremeDate(DateTime dt)
        {
            // Is this probably a useless date? NOTE: DateTime is not nullable so use this as null.
            // e.g. Year <= 1
            return dt <= kExtremeMin || dt >= kExtremeMax;
        }

        public static double ToJavaTime(DateTime dt)
        {
            // JavaScript timestamp is milliseconds past epoch
            if (IsExtremeDate(dt))
                return 0;
            return (dt - kUnixEpoch).TotalMilliseconds;
        }
        public static DateTime FromJavaTime(double javaTimeStamp)
        {
            // JavaScript timestamp is milliseconds past epoch
            return kUnixEpoch.AddMilliseconds(javaTimeStamp);
        }

        public static string TimeSpanStr(TimeSpan ts)
        {
            // Rough amount of time ago or ahead.
            // var ts = (yourDate - DateTime.UtcNow);

            const int kSECOND = 1;
            const int kMINUTE = 60 * kSECOND;
            const int kHOUR = 60 * kMINUTE;
            const int kDAY = 24 * kHOUR;
            const int kMONTH = 30 * kDAY;

            double delta = ts.TotalSeconds;
            bool inPast = delta < 0;
            string ago = "";
            if (inPast)
            {
                delta = -delta;
                ts = new TimeSpan(-ts.Ticks);
                ago = " ago";
            }

            if (delta < 1)
                return "now";

            if (delta < kMINUTE)
                return ts.Seconds == 1 ? "one second" + ago : ts.Seconds + " seconds" + ago;

            if (delta < 2 * kMINUTE)
                return "a minute" + ago;

            if (delta < 45 * kMINUTE)
                return ts.Minutes + " minutes" + ago;

            if (delta < 90 * kMINUTE)
                return "an hour" + ago;

            if (delta < 24 * kHOUR)
                return ts.Hours + " hours" + ago;

            if (delta < 48 * kHOUR)
                return ts.TotalSeconds > 0 ? "a day" : "yesterday";

            if (delta < 30 * kDAY)
                return ts.Days + " days" + ago;

            if (delta < 12 * kMONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month" + ago : months + " months" + ago;
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year" + ago : years + " years" + ago;
            }
        }

        public static string TimeAgoStr(DateTime t)
        {
            if (IsExtremeDate(t))
            {
                return "never";
            }
            return TimeSpanStr(DateTime.Now - t);
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

        public static System.DayOfWeek ModDOW(DayOfWeek dow)
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
                switch (s.ToUpper())
                {
                    case "MONDAY":
                    case "MON":
                    case "M":
                        return DayOfWeek.Monday;
                    case "TUESDAY":
                    case "TUES":
                    case "TUE":
                    case "T":
                        return DayOfWeek.Tuesday;
                    case "WEDNESDAY":
                    case "WED":
                    case "W":
                        return DayOfWeek.Wednesday;
                    case "THURSDAY":
                    case "THURS":
                    case "THU":
                    case "TH":
                        return DayOfWeek.Thursday;
                    case "FRIDAY":
                    case "FRI":
                    case "F":
                        return DayOfWeek.Friday;
                    case "SATURDAY":
                    case "SAT":
                    case "SA":
                        return DayOfWeek.Saturday;
                    case "SUNDAY":
                    case "SUN":
                    case "SU":
                        return DayOfWeek.Sunday;
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
            // Convert minutes in the day to a military (or AMPM) time string.
            if (minutes < 0)
                return null;
            int hours = minutes / 60;
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
                return string.Format("{0:D2}:{1:D2}", hours, minutes);           
            }
        }
    }
}
