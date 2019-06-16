using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DotStd
{
    public enum DateRelative
    {
        // Create a DateRange that is relative to the current date. Or some date in current users time zone and selected starting DoW.
        // Used for reporting purposes. This, Last, Next - Week, Month, etc.
        // <member name="F:OfficeOpenXml.ConditionalFormatting.eExcelConditionalFormattingTimePeriodType.LastWeek">
        // <member name="F:Intuit.Ipp.Data.DateMacro.LastWeek">

        Custom = 0,     // maybe nothing.
        [Description("All Dates")]
        All = 1,        // Any data i might have. no filter on time. Max Range.
        None = 2,       // Empty date range.

        [Description("Today")]
        DSF,        // Current day so far. Not future.
        [Description("Week to Date")]
        WTD,
        [Description("Month to Date")]
        MTD,
        [Description("Quarter to Date")]
        QTD,
        [Description("Year to Date")]
        YTD,

        Today,          // Includes to the end of today.
        ThisWeek,       // Starting day of week (DoW) is based on users pref ?
        ThisMonth,
        ThisQuarter,
        ThisYear,       // to the end of this year. not just year to date.

        Yesterday,
        LastWeek,       // Starting day of week (DoW) is based on users pref ? // e.g. JS moment 'lastWeek'. 
        LastMonth,
        LastQuarter,
        LastYear,
    }

    public enum TimeUnitId : byte
    {
        // Time unit type. 
        // Schedule can recur every Nth recurring (Interval) time unit.
        // used by schedule.RecurTypeId
        // used by app_job.RecurTypeId
        // used by agency_sub_type.RecurTypeId

        None = 0,       // just once. never again

        MilliSec = 1,   // fractional seconds.
        Seconds = 2,    // 
        Minutes = 3,
        Hours = 4,

        Days = 5,      // Every day or every other day for time period.
        Weeks = 6,     // can use bitmask of days of the week.

        // Approximate unit times.
        Months,        // On same day of month.
        Quarters,
        Years,         // do this once per year. on same day of year.
    }

    [Flags]
    public enum DaysOfWeek : byte
    {
        // Bitmask of days of week.
        // 0 = none. 
        Sunday = (1 << (int)System.DayOfWeek.Sunday),   // =1, Sunday = 0
        Monday = (1 << (int)System.DayOfWeek.Monday),
        Tuesday = (1 << (int)System.DayOfWeek.Tuesday),
        Wednesday = (1 << (int)System.DayOfWeek.Wednesday),
        Thursday = (1 << (int)System.DayOfWeek.Thursday),
        Friday = (1 << (int)System.DayOfWeek.Friday),
        Saturday = (1 << (int)System.DayOfWeek.Saturday),
        Any = 127,  // all days.
    }

    /// <summary>
    /// Represents a range of two date/times. .NET has no native concept.
    /// </summary>
    public struct DateRange : IEquatable<DateRange>
    {
        // A range of DateTime. Might be just dates or full times. usually UTC.

        /// <summary>
        /// Gets or Sets the date representing the start of this range.
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// Gets or Sets the date representing the end of this range. May be used as inclusive date (or not)
        /// </summary>
        public DateTime End { get; set; }

        public bool IsValidRange
        {
            // Is date range valid? 
            // NOTE: Empty range is valid.
            get
            {
                return !this.Start.IsExtremeDate() && !this.End.IsExtremeDate() && this.Start <= this.End;
            }
        }

        public TimeSpan TimeSpan
        {
            // Exclusive dates.
            // NOTE: if IsExtremeDate() then this really isn't valid.
            get
            {
                return this.End - this.Start;
            }
        }

        /// <summary>
        /// Gets the range of time only if the range is valid (e.g. Start less than End) else return 0.
        /// </summary>
        /// <returns>Integer minutes of time span. If span is not valid return 0.</returns>
        public int SpanMinutesValid
        {
            get
            {
                if (!IsValidRange)
                    return 0;   // NOT valid range.
                return (int)this.TimeSpan.TotalMinutes;
            }
        }

        public bool IsEmptyTime
        {
            // Is this time range empty?
            get
            {
                if (!IsValidRange)
                    return true;
                return this.Start >= this.End;
            }
        }
        public bool IsEmptyDates
        {
            // Is empty day range? e.g. end on same day = 1 day.
            get
            {
                if (!IsValidRange)
                    return true;
                return this.Start.Date > this.End.Date;
            }
        }

        public DateRange(DateTime dt)
        {
            // create empty range
            Start = dt;
            End = dt;
        }

        /// <summary>
        /// Constructs a new <see cref="DateRange"/> using the specified start and end DateTimes.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        public DateRange(DateTime start, DateTime? end)
            : this()
        {
            Start = start;
            End = end ?? DateTime.MaxValue;
        }

        /// <summary>
        /// Constructs a new <see cref="DateRange"/> using the specified start DateTime and a TimeSpan.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="timeSpan">The end of the range.</param>
        public DateRange(DateTime start, TimeSpan timeSpan)
            : this()
        {
            Start = start;
            End = start + timeSpan;
        }

        public DateRange(DateTime[] range)
        {
            Start = (range.Length > 0) ? range[0] : DateTime.MinValue;
            End = (range.Length > 1) ? range[1] : DateTime.MaxValue;
        }

        public void FixExtreme(bool inclusiveDefault)
        {
            // Null/empty dates produce IsExtremeDate. do i want this to mean infinite range ?
            bool isStartEx = Start.IsExtremeDate();
            bool isEndEx = End.IsExtremeDate();
            if (inclusiveDefault)
            {
                // Default = include more if we get null values
                if (isStartEx) Start = DateUtil.kExtremeMin;
                if (isEndEx) End = DateUtil.kExtremeMax;
            }
            else if (isStartEx && isEndEx)
            {
                // Bad IsValidRange
                Start = DateTime.MinValue;
                End = DateTime.MinValue;
            }
            else
            {
                // Just empty range.
                if (isStartEx) Start = End;
                if (isEndEx) End = Start;
            }
        }

        public DateRange(DateTime startDate, DateTime endDate, bool inclusiveDefault)
        {
            Start = startDate;
            End = endDate;
            FixExtreme(inclusiveDefault);
        }

        /// <summary>
        /// Expand the range of time to include this date time.
        /// </summary>
        public void Add(DateTime dt)
        {
            // Add
            if (dt < Start)
                Start = dt;
            if (dt > End)
                End = dt;
        }

        public void Add(DateRange dtr)
        {
            // Add / Union a date range to this.
            if (dtr.Start < Start)
                Start = dtr.Start;
            if (dtr.End > End)
                End = dtr.End;
        }


        /// <summary>
        /// Returns true if the specified DateTimes fall between the DateTimes of the range.
        /// </summary>
        /// <param name="dates">An array of dates to check</param>
        /// <returns>
        /// true, if all dates supplied fall between the start DateTime and end DateTime of 
        /// the range and false otherwise.
        /// </returns>
        public bool IsInRangeI(DateTime dateTime)
        {
            return (dateTime >= this.Start) && (dateTime <= this.End);
        }

        /// <summary>
        /// Returns true if ALL the specified DateTimes fall between the DateTimes of the range.
        /// </summary>
        /// <param name="dateTimes">An array of dates to check</param>
        /// <returns>
        /// true, if all dates supplied fall between the start DateTime and end DateTime of 
        /// the range and false otherwise.
        /// </returns>
        public bool IsInRangeI(params DateTime[] dateTimes)
        {
            var dtr = this;
            return dateTimes.All(dt => dtr.IsInRangeI(dt));
        }

        /// <summary>
        /// Checks if the supplied DateRange is between the dates of this range.
        /// </summary>
        /// <param name="dates">The DateRange to check.
        /// <returns>true if the DateRange starts and ends within this DateRange.</returns>
        public bool IsInRangeI(DateRange dtr)
        {
            return IsInRangeI(dtr.Start, dtr.End);
        }

        /// <summary>
        /// Converts the value of the current DateTimeRange object to a string of the format "Start - End"
        /// </summary>
        /// <returns>A string representation of the DateTimeRange.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The date and time is outside the range of dates supported by the calendar
        /// used by the current culture.
        /// </exception>
        public override string ToString()
        {
            return "{0} - {1}".FormatInvariant(Start, End);
        }

        /// <summary>
        /// Converts the value of the current DateTimeRange object to a string of the format "Start - End"
        /// where the Start and End dates are converted using the specified culture-specific format information.
        /// </summary>
        /// <param name="format">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of the DateTimeRange.</returns>
        /// <exception cref="System.FormatException">
        /// The length of format is 1, and it is not one of the format specifier characters
        /// defined for System.Globalization.DateTimeFormatInfo.-or- format does not
        /// contain a valid custom format pattern.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The date and time is outside the range of dates supported by the calendar
        //  used by provider.
        /// </exception>
        public string ToString(IFormatProvider provider)
        {
            return "{0} - {1}".FormatInvariant(Start.ToString(provider), End.ToString(provider));
        }

        /// <summary>
        /// Converts the value of the current DateTimeRange object to a string of the format "Start - End"
        /// where the Start and End dates are converted using the specified format.
        /// </summary>
        /// <param name="format">A standard or custom date and time format string.</param>
        /// <returns>A string representation of the DateTimeRange.</returns>
        /// <exception cref="System.FormatException">
        /// The length of format is 1, and it is not one of the format specifier characters
        /// defined for System.Globalization.DateTimeFormatInfo.-or- format does not
        /// contain a valid custom format pattern.
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// The date and time is outside the range of dates supported by the calendar
        /// used by the current culture.
        /// </exception>
        public string ToString(string format)
        {
            return "{0} - {1}".FormatInvariant(Start.ToString(format), End.ToString(format));
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(DateRange other)
        {
            return (Start == other.Start) && (End == other.End);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">Another object to compare to.</param>
        /// <returns>
        /// true if obj and this instance are the same type and represent the same value;
        /// otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is DateRange))
            {
                return false;
            }

            return Equals((DateRange)obj);
        }

        /// <summary>
        /// Indicates whether two specified instances of DateTimeRange are equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>
        /// true if a and b instances represent the same value;
        /// otherwise, false.
        /// </returns>
        public static bool operator ==(DateRange a, DateRange b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Indicates whether two specified instances of StringEnum are not equal.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>
        /// true if a and b instances represent different value;
        /// otherwise, false.
        /// </returns>
        public static bool operator !=(DateRange a, DateRange b)
        {
            return !(a.Equals(b));
        }

        /// <summary>
        /// Returns the hash code for this range.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Start.GetHashCode() ^ End.GetHashCode();
        }

        public void SetDatesForWeekEnd(DateTime dt, DayOfWeek weekend)
        {
            // Create range aligned to week weekend. Inclusive.
            DateTime d2 = DateUtil.GetDateOfWeekDayNext(dt, weekend);
            Start = d2.AddDays(-6).Date;
            End = d2.Date;
        }

        public void SetDatesForWeek(DateTime dt, bool isStartDate_)
        {
            // Create a week of inclusive dates.
            // date_ = start or end of week,
            dt = dt.Date;
            if (isStartDate_)
            {
                Start = dt;
                End = dt.AddDays(6);
            }
            else
            {
                Start = dt.AddDays(-6);
                End = dt;
            }
        }

        public void SetDatesForMonth(int year, int month)
        {
            // Create Date range aligned to month. Inclusive.
            // month = 1 based. MonthId
            int daysInMonth = DateTime.DaysInMonth(year, month);
            Start = new DateTime(year, month, 1);
            End = new DateTime(year, month, daysInMonth);
        }

        public void SetDatesForMonth(DateTime dt)
        {
            // Create Date range aligned to month that includes dt. Inclusive.
            SetDatesForMonth(dt.Year, dt.Month);   // 1 based. MonthId
        }

        public void SetDatesForQuarter(int year, int month)
        {
            // Set date range aligned to quarter. inclusive.
            // month = 1 based. MonthId
            int q = (month - 1) / 3;  // quarter 0 based. 0 to 3
            Start = new DateTime(year, (q * 3) + 1, 1);
            End = Start.AddMonths(3);
        }

        public void SetDatesForQuarter(DateTime dt)
        {
            // Set date range aligned to quarter.  inclusive.
            SetDatesForQuarter(dt.Year, dt.Month);    // 1 based. MonthId
        }

        public void SetDatesForYear(int year)
        {
            // Create date range aligned to year. inclusive.
            Start = new DateTime(year, 1, 1);
            End = new DateTime(year, 12, 31);
        }
        public void SetDatesForYear(DateTime dt)
        {
            SetDatesForYear(dt.Year);
        }

        public static readonly string[] _Units = { // for TimeUnitId
            null, null,     // once
            "milliseconds", "every millisecond",
            "seconds", "every second",
            "minutes", "every minute",
            "hours", "hourly",
            "days", "daily",
            "weeks", "weekly",
            "months", "monthly",
            "quarters", "quarterly",
            "years", "yearly",
        };

        public static string GetRecurStr(DateTime start, TimeUnitId unitId, int interval = 1, DaysOfWeek dowBits = DaysOfWeek.Any)
        {
            // Describe the recurrence pattern. scheduled recurrence rules.
            // Ignore that start may be in the future.

            if (DateUtil.IsExtremeDate(start))
                return "Never";

            if (unitId <= TimeUnitId.None || unitId > TimeUnitId.Years)    // does not repeat.
            {
                // "Once at " + date time
                return start.ToString();
            }

            if (interval <= 0)
                interval = 1;

            var sb = new StringBuilder();
            int uniti = (int)unitId;
            sb.Append(interval > 1 ? $"Every {interval} {_Units[uniti * 2]}" : _Units[(uniti * 2) + 1]);

            switch (unitId)
            {
                case TimeUnitId.MilliSec:   // fractional seconds.
                case TimeUnitId.Seconds:
                case TimeUnitId.Minutes:
                    return sb.ToString();   // done. // Say no more.

                case TimeUnitId.Hours:
                    if (start.Minute > 0)
                    {
                        sb.Append($" at {start.Minute} minutes past");    // minutes into the hour.
                    }
                    return sb.ToString();   // done.

                case TimeUnitId.Days:     // daily. can use bitmask of days of the week.
                    if (dowBits != 0 && dowBits != DaysOfWeek.Any)
                    {
                        // Only on specific days of the week.
                        if (interval == 1)
                        {
                            sb.Clear();
                            sb.Append("Every ");
                        }
                        else
                        {
                            sb.Append(" if ");
                        }
                        int dowBit = 1;
                        bool hasBit = false;
                        for (int i = 0; i < 7; i++, dowBit <<= 1)
                        {
                            if ((((int)dowBits) & dowBit) != 0)
                            {
                                if (hasBit)
                                    sb.Append(",");
                                sb.Append(((System.DayOfWeek)i).ToString());
                                hasBit = true;
                            }
                        }
                    }
                    break;

                case TimeUnitId.Weeks:     // a single day of the week.
                    if (interval == 1)
                    {
                        sb.Clear();
                        sb.Append($"Every {start.DayOfWeek.ToString()}");
                        break;
                    }
                    sb.Append($"on {start.DayOfWeek.ToString()}");
                    break;

                // Approximate unit times.
                case TimeUnitId.Months:        // On same day of month.
                    if (interval == 1)
                    {
                        sb.Clear();
                        sb.Append($"Every {Formatter.ToOrdinal(start.Day)} of the month");
                        break;
                    }
                    sb.Append($" on {Formatter.ToOrdinal(start.Day)} of the month");
                    break;

                case TimeUnitId.Quarters:
                    // Name the months ?? Jan, Apr, Jul, Oct
                    sb.Append($" on {Formatter.ToOrdinal(start.Day)} of the month");
                    break;

                case TimeUnitId.Years:
                    sb.Append($" on {start.ToString("MMM", CultureInfo.InvariantCulture)} {start.Day:D2}");
                    break;

                default:
                    return "?"; // not valid for TimeUnitId
            }

            sb.Append($" at {start.Hour:D2}:{start.Minute:D2}");    // at hr:minutes into the day.
            return sb.ToString();
        }

        public static readonly int[] _TimeUnits = { // for TimeUnitId
            0,
            1,  // mSec
            1000,       // Sec
            60*1000,        // Minute
            60*60*1000,         // Hour
            24*60*60*1000,      // Day
            7*24*60*60*1000,    // TimeUnitId.Weeks
            1,  // TimeUnitId.Months
            3,  // Quarter
            12, // Years
        };
        
        public static bool IsDowSet(DaysOfWeek dowBits, DayOfWeek dayOfWeek)
        {
            // Is a DayOfWeek in the DaysOfWeek mask?
            return (((int)dowBits) & (1 << ((int)dayOfWeek))) != 0;
        }

        public static DateTime GetNextRecur(DateTime now, DateTime start, TimeUnitId unitId, int interval = 1, DaysOfWeek dowBits = DaysOfWeek.Any)
        {
            // Get the next date / time in the sequence after now.
            // start = anchor date. Was the last official time. for calculation of relative times, time of day, etc. kExtremeMax = never before
            // interval = quantity of TimeUnitId
            // dowBits = only on these days of the week.
            // RETURN: kExtremeMax = never again.
            // ASSUME times are GMT and have no DST weirdness.

            if (DateUtil.IsExtremeDate(start))
                return DateUtil.kExtremeMax;        // never
            if (now < start)    // not until start date.
                return start;

            if (unitId <= TimeUnitId.None || unitId > TimeUnitId.Years)    // does not repeat.
            {
                return (now <= start) ? start : DateUtil.kExtremeMax;
            }

            if (interval <= 0)
                interval = 1;

            DateTime dtNext;

            int timeUnits = _TimeUnits[(int)unitId];

            if (unitId <= TimeUnitId.Weeks)
            {
                // Discreet/Exact time units.
                long tickDiff = now.Ticks - start.Ticks;

                long intervalUnits = (timeUnits * TimeSpan.TicksPerMillisecond) * interval;
                long intervalsDiff = tickDiff / intervalUnits;

                int intervalInc = ((tickDiff % intervalUnits) == 0) ? 0 : 1;    // increment?

                dtNext = new DateTime(start.Ticks + ((intervalsDiff + intervalInc) * intervalUnits));

                if (unitId == TimeUnitId.Days)
                {
                    // skips days if not on dowMask. if interval is multiple of 7 then this may never satisfy !!
                    for (int i = 0; true; i++)
                    {
                        if (IsDowSet(dowBits, dtNext.DayOfWeek))    // good.
                            break;
                        if (i > 7)
                            return DateUtil.kExtremeMax;        // never
                        dtNext = dtNext.AddTicks(intervalUnits);
                    }
                }
            }
            else
            {
                // month based time. not exact/Discreet time units.
                int monthsStart = start.Year * 12 + start.Month;
                int monthsNow = now.Year * 12 + now.Month;
                int monthsDiff = monthsNow - monthsStart;

                int intervalUnits = timeUnits * interval;
                int intervalsDiff = monthsDiff / intervalUnits;

                int intervalInc = ((monthsDiff % intervalUnits) == 0 && start.Day == now.Day && start.TimeOfDay == now.TimeOfDay) ? 0 : 1;  // increment?

                dtNext = start.AddMonths((intervalsDiff + intervalInc) * intervalUnits);
            }

            return dtNext;
        }

        public List<DateTime> GetRecursInRange(DateTime start, TimeUnitId unitId, int interval = 1, DaysOfWeek dowBits = DaysOfWeek.Any, int nMax = 128)
        {
            // Get list of dates that recur in this range. (inclusive)
            // interval = quantity of TimeUnitId

            var ret = new List<DateTime>();
            DateTime now = this.Start;
            DateTime dtPrev = DateUtil.kExtremeMin;

            while (true)
            {
                DateTime dtNext = GetNextRecur(now, start, unitId, interval, dowBits);
                System.Diagnostics.Debug.Assert(dtPrev != dtNext);
                if (dtNext.IsExtremeDate() || dtNext > this.End)
                    return ret;
                ret.Add(dtNext);
                if (ret.Count > nMax)    // we exceeded max requested returns?
                {
                    return ret;
                }
                dtPrev = dtNext;
                now = dtNext.AddMilliseconds(1);    // next time.
            }
        }
    }
}
