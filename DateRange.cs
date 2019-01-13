using System;
using System.ComponentModel;
using System.Linq;

namespace DotStd
{
    /// <summary>
    /// Represents a range of two date/times. .NET has no native concept.
    /// </summary>
    public struct DateRange : IEquatable<DateRange>
    {
        // A range of DateTime. Might be just dates or full times.

        /// <summary>
        /// Gets or Sets the date representing the start of this range.
        /// </summary>
        public DateTime Start { get; set; }

        /// <summary>
        /// Gets or Sets the date representing the end of this range.
        /// </summary>
        public DateTime End { get; set; }

        public TimeSpan TimeSpan
        {
            // Exclusive dates.
            get
            {
                return this.End - this.Start;
            }
        }

        /// <summary>
        /// Gets the range of time in days only if the range is valid (e.g. Start less than End) else return 0.
        /// </summary>
        /// <returns>Integer days of time span. If span is not valid return 0.</returns>
        public int SpanDaysValid
        {
            get
            {
                int iDays = this.TimeSpan.Days;
                if (iDays <= 0)  // NOT valid range.
                    return 0;
                return iDays;
            }
        }

        public bool IsValidDateRange
        {
            // Empty is valid.
            get
            {
                return !this.Start.IsExtremeDate() && this.Start <= this.End;
            }
        }

        public bool IsEmptyI
        {
            // Inclusive dates. (End is included)
            get
            {
                if (!IsValidDateRange)
                    return true;
                return this.Start > this.End;
            }
        }
        public bool IsEmptyX
        {
            // Exclusive dates.
            get
            {
                if (!IsValidDateRange)
                    return true;
                return this.Start >= this.End;
            }
        }

        public DateRange(DateTime dt)
        {
            // empty
            Start = dt;
            End = dt;
        }

        /// <summary>
        /// Constructs a new <see cref="DateRange"/> using the specified start and end DateTimes.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        public DateRange(DateTime start, DateTime end)
            : this()
        {
            Start = start;
            End = end;
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

        public DateRange(DateTime[] startEndDates)
        {
            Start = startEndDates[0];
            End = startEndDates[1];
        }

        public void FixExtreme(bool inclusiveDefault)
        {
            // Null/empty dates produce IsExtremeDate. What did we intend ?
            bool isStartEx = Start.IsExtremeDate();
            bool isEndEx = End.IsExtremeDate();
            if (inclusiveDefault)
            {
                // Default = include more if we get null values
                if (isStartEx) Start = DateUtil.kDateExtremeMin;
                if (isEndEx) End = DateUtil.kDateExtremeMax;
            }
            else if (isStartEx && isEndEx)
            {
                // Bad IsValidDateRange
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
        /// Returns the hash code for this object.
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

        public void SetDatesForMonth(DateTime dt)
        {
            // Create Date range aligned to month that includes dt. Inclusive.
            int y = dt.Year;
            int m = dt.Month;
            int daysInMonth = DateTime.DaysInMonth(y, m);
            Start = new DateTime(y, m, 1);
            End = new DateTime(y, m, daysInMonth);
        }

        public static DateRange GetMonth(DateTime dt)
        {
            // Get a month range.
            // same as SetDatesForMonth
            int y = dt.Year;
            int m = dt.Month;
            int daysInMonth = DateTime.DaysInMonth(y, m);
            return new DateRange(new DateTime(y, m, 1),
                new DateTime(y, m, daysInMonth));
        }

        public void SetDatesForQuarter(DateTime dt)
        {
            // Set date range aligned to quarter.
            int y = dt.Year;
            int m = dt.Month;    // 1 based
            int q = (m - 1) / 3;  // quarter 0 based. 0 to 3
            Start = new DateTime(y, (q * 3) + 1, 1);
            End = Start .AddMonths(3).AddDays(-1);
        }

        public void SetDatesForYear(DateTime dt)
        {
            // Create range aligned to year.
            Start = new DateTime(dt.Year, 1, 1);
            End = new DateTime(dt.Year, 12, 31);
        }
    }
}
