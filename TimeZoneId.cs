using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DotStd
{
    /// <summary>
    /// Enum of known useful time zones as number id.
    /// # = usually difference in minutes from GMT if between (-12*60 and 12*60). Standard Time.
    /// DO NOT ASSUME All values between (-12*60 and 12*60) observe US daylight savings time rules.
    /// </summary>
    public enum TimeZoneId
    {
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

        UTC = 1,        // No DST. default?
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

    /// <summary>
    /// IANA timezone (~374) is a super-set of Windows .NET time zones (~138). 
    /// https://en.wikipedia.org/wiki/List_of_tz_database_time_zones table.
    /// Wrapper for useful info from .NET System.TimeZoneInfo (adds an integer id)
    /// https://en.wikipedia.org/wiki/List_of_tz_database_time_zones table.
    /// ASSUME all zones Offset modulus to 15 minute chunks.
    /// This zone may/should have an equiv JavaScript IANA Name
    /// Similar to JavaScript getTimezoneOffset()
    /// https://stackoverflow.com/questions/17348807/how-to-translate-between-windows-and-iana-time-zones
    /// </summary>
    public class TimeZoneEntry
    {
        /// <summary>
        /// PK for TZ. See TimeZoneEntry.GetOffset(), maybe the same as Offset for most common time zones.
        /// </summary>
        public readonly TimeZoneId Id;
        /// <summary>
        /// offset in minutes from UTC. Not including DST offset. May be same as Id and/or BaseUtcOffset
        /// </summary>
        public readonly int Offset;
        /// <summary>
        /// AKA SupportsDaylightSavingTime. Does this zone use Daylight Savings Time (DST) rules for part of the year? e.g. EST are EDT are the same time zone.
        /// </summary>
        public byte UsesDst { get; set; }
        /// <summary>
        /// equiv JavaScript / IANA Name. System.TimeZoneInfo.HasIanaId
        /// </summary>
        public string? IanaId { get; set; }

        /// <summary>
        /// best match for .NET TimeZoneInfo and TimeZoneId . fallback to TimeZoneInfo.Utc. WindowsId
        /// </summary>
        System.TimeZoneInfo? _tzi;

        /// <summary>
        /// Find best Match to a .NET TimeZoneInfo
        /// MUST resolve to a TZ! TimeZoneInfo.GetSystemTimeZones(). 
        /// </summary>
        [MemberNotNull(member: nameof(_tzi))]
        public System.TimeZoneInfo TZI
        {
            get
            {
                if (_tzi != null)
                    return _tzi;
                _tzi = FindTimeZoneInfoBest(Offset, UsesDst, null, null);
                return _tzi;
            }
        }

        /// <summary>
        /// Get raw/estimated offset from UTC in minutes given TimeZoneId
        /// </summary>
        /// <param name="id">TimeZoneId</param>
        /// <returns>minutes</returns>
        public static int GetOffsetEst(TimeZoneId id)
        {
            // -599 -> -600, 601 -> 600
            // 'Order by' should have lowest as best match.
            int i = (int)id;
            int mod = (i % 15);
            if (mod < 0)
                return i - (15 + mod);
            return i - mod; // Chop 15 minute chunk.
        }

        public TimeZoneEntry(TimeZoneId id)
        {
            Id = id;
            Offset = GetOffsetEst(id);
        }

        /// <summary>
        /// Make equiv System.TimeZoneInfo (WindowsId) for this TimeZoneEntry. Find best match for name.
        /// NOTE: this throws if the id is invalid.
        /// When hosted on Linux see TZDIR : /usr/share/zoneinfo/
        /// https://stackoverflow.com/questions/41566395/timezoneinfo-in-net-core-when-hosting-on-unix-nginx
        /// </summary>
        /// <returns></returns>
        public TimeZoneEntry(TimeZoneId id, int offset, string? windowsId)
        {
            Id = id;
            Offset = offset;
            if (!string.IsNullOrWhiteSpace(windowsId))
            {
                try
                {
                    _tzi = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }
                catch // (System.TimeZoneNotFoundException ex)
                {
                    // WindowsId (Id) doesn't match this system anymore . What should we do ?
                    // Call UpdateTimeZoneInfo() again ??
                }
            }
         }

        public string WindowsId => TZI.Id;    // .NET Id for Windows registry

        /// <summary>
        /// like: TimeZoneInfo.HasSameRules()
        /// </summary>
        /// <param name="tzi"></param>
        /// <returns></returns>
        public bool IsEquiv(System.TimeZoneInfo tzi)
        {
            if (this.Offset != tzi.BaseUtcOffset.TotalMinutes)
                return false;
            if (Converter.ToBool(this.UsesDst) != tzi.SupportsDaylightSavingTime)
                return false;
            return true;
        }

        /// <summary>
        /// Are these time zones functionally equivalent? like: TimeZoneInfo.HasSameRules()
        /// Maybe diff countries (or IANA id) but really the same?
        /// </summary>
        /// <param name="tz"></param>
        /// <returns></returns>        
        public bool IsEquiv(TimeZoneEntry tz)
        {
            if (this.Offset != tz.Offset)
                return false;
            if (this.UsesDst != tz.UsesDst)
                return false;
            return true;
        }

        /// <summary>
        /// convert (this) local time zone DateTime to UTC DateTime
        /// </summary>
        /// <param name="dt">DateTime</param>
        /// <returns>DateTime</returns>
        public DateTime ToUtc(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc)    // no change.
                return dt;
            return TimeZoneInfo.ConvertTimeToUtc(TimeNow.Utc, TZI);
        }

        /// <summary>
        /// convert UTC DateTime to (this) local time zone DateTime
        /// </summary>
        /// <param name="dt">DateTime</param>
        /// <returns>DateTime</returns>
        public DateTime ToLocal(DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)    // ASSUME already converted to local. Though we don't know that for sure. we just know its not UTC.
                return dt;
            return TimeZoneInfo.ConvertTimeFromUtc(dt, TZI);
        }

        /// <summary>
        /// offset in minutes for a timezone.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        static string GetOffsetStr(int offset)
        {
            if (offset < 0)
                return string.Concat("(-", DateUtil.GetTimeStr(-offset), ")");
            return string.Concat("(+", DateUtil.GetTimeStr(offset), ")");
        }

        public string OffsetStr => TimeZoneEntry.GetOffsetStr(Offset);

        /// <summary>
        /// find the BEST match(s) for TimeZoneInfo. TryConvertWindowsIdToIanaId ?
        /// </summary>
        /// <param name="lstTzi"></param>
        /// <param name="offsetMin">MUST match this.</param>
        /// <param name="usesDst">MUST match this</param>
        /// <param name="nameIANA">JavaScript name. NOT Windows name.</param>
        /// <param name="name2"></param>
        /// <returns></returns>
        public static TimeZoneInfo FindTimeZoneInfoBest(int offsetMin, byte usesDst, string? nameIANA, string? name2)
        {
            var sysTZs = TimeZoneInfo.GetSystemTimeZones();

            // TZ MUST be IsEquiv().
            bool supportsDST = Converter.ToBool(usesDst);
            var lstTziEquiv = sysTZs.Where(tzi => offsetMin == tzi.BaseUtcOffset.TotalMinutes && supportsDST == tzi.SupportsDaylightSavingTime);
            if (!lstTziEquiv.Any())
            {
                // no possible matches! odd.
                return TimeZoneInfo.Utc;
            }

            TimeZoneInfo? tziBest = null;

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
                // just take the first IsEquiv() match.
                // null countries get picked first if no other matches. 
                tziBest = lstTziEquiv.First(); // just take the first.
            }

            return tziBest;
        }
    }
}
