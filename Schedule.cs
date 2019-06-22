using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DotStd
{
    public enum TimeUnitId : byte
    {
        // Time unit type. 
        // Schedule can recur every Nth recurring (Interval) time unit.
        // used by schedule.RecurUnitId
        // used by app_job.RecurUnitId

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

    public class Schedule
    {
        // A time in the future that may recur.
        // Ignore end of recur sequence here.

        public DateTime StartTime;          // When does this happen or happen first in recurring pattern?
        public TimeUnitId RecurUnitId;       // Does it recur ? on what time unit ?
        public int RecurInterval = 1;    // TimeUnitId interval skips. e.g. 2 with unit day = every 2 days.
        public DaysOfWeek DowBits = DaysOfWeek.Any; // only on these days of the week.

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
            sb.Append(interval > 1 ? $"Every {interval} {_Units[uniti * 2]}" : _Units[(uniti * 2) + 1]);    // pluralize?

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

        public string GetRecurStr()
        {
            // like ToString();
            return GetRecurStr(this.StartTime, this.RecurUnitId, this.RecurInterval, this.DowBits);
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

        public DateTime GetNextRecur(DateTime now)
        {
            // Describe the schedule recur as a string.
            return GetNextRecur(now, this.StartTime, this.RecurUnitId, this.RecurInterval, this.DowBits);
        }

        public List<DateTime> GetRecursInRange(DateRange dtr, int nMax = 128)
        {
            // Get list of dates that recur in this range. (inclusive)
            // interval = quantity of TimeUnitId

            var ret = new List<DateTime>();
            DateTime now = dtr.Start;
            DateTime dtPrev = DateUtil.kExtremeMin;

            while (true)
            {
                DateTime dtNext = GetNextRecur(now, this.StartTime, this.RecurUnitId, this.RecurInterval, this.DowBits);
                System.Diagnostics.Debug.Assert(dtPrev != dtNext);
                if (dtNext.IsExtremeDate() || dtNext > dtr.End)
                    return ret;
                ret.Add(dtNext);
                if (ret.Count > nMax)    // we exceeded max requested quantity of returns?
                {
                    return ret;
                }
                dtPrev = dtNext;
                now = dtNext.AddMilliseconds(1);    // next time.
            }
        }
    }
}
