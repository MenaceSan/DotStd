using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// data units. What type of data? SI units + more.
    /// https://en.wikipedia.org/wiki/SI_base_unit
    /// </summary>
    enum DataUnit
    {
        unk = 0,        // can be fractional. percent?
        boolean = 1,       // true or false state.
        quantity = 2,   // quantity of some arbitrary X, typically whole numbers.

        m,      // meters distance.
        g,      // grams mass. (kg)
        s,      // seconds of time. (minutes, hours)

        cd,     // Candella = quantity of luminous intensity (photons)
        A,      // Amperes = quantity of electric current (electrons)
        mol,    // Moles = quantity of substance. (molecules)

        K,      // Kelvin temperature. general collective energy level.

        // Combo unit types. 2 or more SI base types .
        // https://en.wikipedia.org/wiki/Non-SI_units_mentioned_in_the_SI

        v,          // Velocity = m/s
        hectare,    // square area - 100 m2
        litre,      // volume - 0.001 m3
        radian,     // Pi angle.

        // Medical. complex units.

        bpm,        // something per minute. Beats, breaths, etc.
        bmi,        // kg weight per m2 = density.
        bp,         // blood pressure = mmHg (Systolic/Diastolic)
        SpO2,       // %. Pulse oximetry. https://en.wikipedia.org/wiki/Pulse_oximetry
        Glucose,    // mg/dL
    }

    /// <summary>
    /// A single point of data from the real world.
    /// a sample, measurement, reading, point, value,
    /// </summary>
    public class DataValue
    {
        DataUnit Unit { get; set; }
        object? Value { get; set; }      // double ?
    }
}
