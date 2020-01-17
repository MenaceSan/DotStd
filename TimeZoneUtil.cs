using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DotStd
{
    public enum TimeZoneId
    {
        // Difference in minutes from GMT if between (-12*60 and 12*60). Standard Time.
        // ASSUME all zones modulus to 15 minute chunks.
        // ?? Do All values between (-12*60 and 12*60) observe US daylight savings time rules.
        // ?? Create new time zones outside this range if the client does not use DST.
        // Similar to JavaScript getTimezoneOffset()

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
        GMT = 12,        // uses DST.
        // ChunkSize = 15,     // ASSUME all zones are in 15 minute chunk size.

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

    public class TimeZoneUtil
    {
        // Pull useful info from TimeZoneInfo
        // https://en.wikipedia.org/wiki/List_of_tz_database_time_zones table.

        public TimeZoneId Id;        // See TimeZoneUtil.GetOffset(), maybe the same as Offset.
        public int Offset { get; set; }          // offset in minutes from UTC. Not including DST.
        public bool UsesDst { get; set; }

        protected TimeZoneInfo _tzi;  // best match for .NET .
        public static TimeZoneInfo _tziUTC;     // Cache this if we need to.

        public bool IsEquiv(TimeZoneInfo tzi)
        {
            if (this.Offset != tzi.BaseUtcOffset.TotalMinutes)
                return false;
            if (this.UsesDst != tzi.SupportsDaylightSavingTime)
                return false;
            return true;
        }

        public bool IsEquiv(TimeZoneUtil tz)
        {
            // Maybe diff countries but really the same?
            if (this.Offset != tz.Offset)
                return false;
            if (this.UsesDst != tz.UsesDst)
                return false;
            return true;
        }

        public static int GetOffsetMinutes(string s)
        {
            // Get "+10:00" and convert to 10*60 minutes.

            string s2 = s.Substring(1); // skip +-
            int offset = DateUtil.GetTimeMinutes(s2);
            if (s[0] == '-')
            {
                return -offset;
            }
            return offset;
        }

        public static string GetOffsetStr(int offset)
        {
            // offset = minutes.
            if (offset < 0)
                return string.Concat("(-", DateUtil.GetTimeStr(-offset), ")");
            return string.Concat("(+", DateUtil.GetTimeStr(offset), ")");
        }

        public string GetOffsetStr()
        {
            // offset = minutes.
            return TimeZoneUtil.GetOffsetStr(Offset);
        }

        public static int GetOffset(TimeZoneId id)
        {
            // -599 -> -600, 601 -> 600
            // 'Order by' should have lowest as best match.
            int i = (int)id;
            int mod = (i % 15);
            if (mod < 0)
                return i - (15 + mod);
            return i - mod; // Chop 15 minute chunk.
        }

        public static TimeZoneInfo FindTimeZoneInfoBest(ReadOnlyCollection<TimeZoneInfo> lstTzi, int offsetMin, bool usesDst, string nameIANA, string name2)
        {
            // find the BEST match(s) for TimeZoneInfo.
            // nameIANA = JavaScript name. NOT Windows name.

            // MUST be IsEquiv().
            var lstTziEquiv = lstTzi.Where(tzi => offsetMin == tzi.BaseUtcOffset.TotalMinutes && usesDst == tzi.SupportsDaylightSavingTime);
            if (!lstTziEquiv.Any())
            {
                // no possible matches. odd.
                return null;
            }

            TimeZoneInfo tziBest = null;

            if (lstTziEquiv.Count() > 1 && nameIANA != null)
            {
                var words1 = nameIANA.Split(' ').ToList();    // IANA name.
                if (!string.IsNullOrWhiteSpace(name2))  // more descriptions.
                {
                    words1.AddRange(name2.Split(' '));
                }

                // remove junk words.
                for (int i = 0; i < words1.Count; i++)
                {
                    string w = words1[i];
                    int j = w.IndexOf('/');
                    if (j >= 0)
                    {
                        // Lose the continent prefix.
                        w = w.Substring(j + 1);
                    }

                    if (w == "area" || w == "(most" || w == "areas)" || w == "-" || w == "(north)")
                    {
                        w = "";
                    }
                    if (w.Contains('+') || w.Contains('-')) // e.g. "GMT-3". toss it.
                        w = "";

                    w = w.Replace("_", " ");

                    words1[i] = w;
                }

                words1 = words1.Where(x => x.Length > 0).OrderByDescending(x => x.Length).ToList();  // longer are more important.

                foreach (TimeZoneInfo tzi in lstTziEquiv)
                {
                    var words2 = tzi.DisplayName.Split(' ').OrderByDescending(x => x.Length);
                    foreach (string w in words1)
                    {
                        if (string.IsNullOrWhiteSpace(w)) // skip
                            continue;
                        if (words2.Contains(w))
                        {
                            tziBest = tzi;
                            break;
                        }
                    }
                    if (tziBest != null)
                        break;
                }
            }

            if (tziBest == null)
            {
                // just take the first match.
                // null countries get picked first in no other matches. 
                tziBest = lstTziEquiv.First(); // just take the first.
            }

            return tziBest;
        }

        protected void UpdateTimeZoneInfo(ReadOnlyCollection<TimeZoneInfo> lstTzi)
        {
            // MUST resolve to a TZ!
            // lstTzi = TimeZoneInfo.GetSystemTimeZones();
            _tzi = FindTimeZoneInfoBest(lstTzi, Offset, UsesDst, null, null);
            if (_tzi != null)
                return;

            // Bad! fall back to UTC! 
            if (_tziUTC != null)
            {
                _tzi = _tziUTC;
                return;
            }

            _tzi = _tziUTC = FindTimeZoneInfoBest(lstTzi, 0, false, null, null);
            if (_tzi != null)
                return;

            // MUST create it !!! Very bad.
            _tzi = _tziUTC = lstTzi.First();
            LoggerUtil.DebugException("No UTC!", null);
        }

        public virtual TimeZoneInfo GetTimeZoneInfo()
        {
            if (_tzi == null)
            {
                UpdateTimeZoneInfo(TimeZoneInfo.GetSystemTimeZones());
            }
            return _tzi;
        }

        public DateTime ToUtc(DateTime dt)
        {
            // convert this local time zone to UTC
            if (dt.Kind == DateTimeKind.Utc)
                return dt;
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.UtcNow, GetTimeZoneInfo());
        }

        public DateTime ToLocal(DateTime dt)
        {
            // convert UTC to this local time zone
            if (dt.Kind != DateTimeKind.Utc)    // already converted.
                return dt;
            return TimeZoneInfo.ConvertTimeFromUtc(dt, GetTimeZoneInfo());
        }

        public static string ToLocalStr(TimeZoneUtil tz, DateTime dt, IFormatProvider provider, string format = null)
        {
            // Localize the time string for user display.
            // if tz == null then just label as (UTC) clearly.

            var tzi = tz?.GetTimeZoneInfo();
            if (tzi == null)
            {
                // Just label as UTC or (LOCAL)
                return dt.ToString(format, provider) + "(UTC)";
            }

            return TimeZoneInfo.ConvertTimeFromUtc(dt, tzi).ToString(format, provider);
        }
    }
}
