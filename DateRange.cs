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

        Custom = 0,     // may be empty or arbitrary date range.
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


    /// <summary>
    /// Represents a range of two date/times. .NET has no native concept.
    /// A range of DateTime. Might be just dates or full times. usually UTC.
    /// </summary>
    public struct DateRange : IEquatable<DateRange>
    {
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
            // Is date range valid? Not extreme or backwards.
            // NOTE: Empty range is valid.
            get
            {
                return !this.Start.IsExtremeDate() && !this.End.IsExtremeDate() && this.Start <= this.End;
            }
        }

        /// <summary>
        /// Gets the number of ticks in the range.
        /// </summary>
        public long Ticks
        {
            get
            {
                return this.End.Ticks - this.Start.Ticks;
            }
        }

        /// <summary>
        /// Gets TimeSpan for the range.
        /// NOTE: Does not check IsExtremeDate().
        /// </summary>
        public TimeSpan TimeSpan
        {
            get
            {
                return this.End - this.Start;
            }
        }

        /// <summary>
        /// Gets the range of time only if the range is valid (e.g. Start less than End) else return 0.
        /// </summary>
        /// <returns>Integer minutes of time span. If span is not valid return 0.</returns>
        public int TotalMinutesValid
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

        /// <summary>
        /// Expand / union the range of time to include this date time.
        /// </summary>
        public void Add(DateTime dt)
        {
            // Add
            if (dt < Start)
                Start = dt;
            if (dt > End)
                End = dt;
        }

        /// <summary>
        /// Expand / union the range of time to include this date time.
        /// </summary>
        public void Add(DateRange dtr)
        {
            // Add / Union a date range to this.
            if (dtr.Start < Start)
                Start = dtr.Start;
            if (dtr.End > End)
                End = dtr.End;
        }


        /// <summary>
        /// Returns true if the specified DateTime falls between the DateTimes of the range.
        /// </summary>
        /// <param name="dateTime">date to check</param>
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
        public override string ToString()
        {
            bool x1 = Start.IsExtremeDate();
            bool x2 = End.IsExtremeDate();
            if (x1 && x2)
                return "";
            return string.Concat(x1 ? "" : Start.ToDateString(), " - ", x2 ? "" : End.ToDateString());
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
            return "{0} - {1}".FormatInvariant(Start.ToDtString(format), End.ToDtString(format));
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
            if (obj is DateRange)
            {
                return Equals((DateRange)obj);
            }
            return false;
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
            /// <param name="dt">start or end of week</param>

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

        public void FixExtreme(bool inclusiveDefault)
        {
            // Try to fix bad data.
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

        //****************

        public DateRange(DateTime dt)
        {
            // create empty range
            Start = dt;
            End = dt;
        }

        public DateRange(DateTime startDate, DateTime endDate, bool inclusiveDefault)
        {
            Start = startDate;
            End = endDate;
            FixExtreme(inclusiveDefault);
        }

        /// <summary>
        /// Constructs a new <see cref="DateRange"/> using the specified start and end DateTimes.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        public DateRange(DateTime start, DateTime? end)
        {
            Start = start;
            End = end ?? DateTime.MaxValue; // infinite.
        }

        /// <summary>
        /// Constructs a new <see cref="DateRange"/> using the specified start DateTime and a TimeSpan.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="timeSpan">The end of the range.</param>
        public DateRange(DateTime start, TimeSpan timeSpan)
        {
            Start = start;
            End = start + timeSpan;
        }

        public DateRange(DateTime[] range)
        {
            Start = (range.Length > 0) ? range[0] : DateTime.MinValue;
            End = (range.Length > 1) ? range[1] : DateTime.MaxValue;
        }
    }
}
