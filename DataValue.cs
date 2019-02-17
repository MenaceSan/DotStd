using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    enum DataUnit
    {
        // units. What type of data? 
        // https://en.wikipedia.org/wiki/SI_base_unit

        unk = 0,        // can be fractional. percent?
        boolean = 1,       // true or false state.
        quantity = 2,   // quantity of some X, typically whole numbers.

        m,      // meters distance.
        g,      // grams mass. (kg)
        s,      // seconds of time. (minutes, hours)

        A,      // Amperes = quantity of electric current
        mol,    // Moles = quantity of substance.
        cd,     // Candella = quantity of luminous intensity

        K,      // Kelvin temperature.

        // Combo unit types. 2 or more SI base types .
        // https://en.wikipedia.org/wiki/Non-SI_units_mentioned_in_the_SI

        v,          // Velocity = m/s
        hectare,    // square - 100 m2
        litre,      // volume - 0.001 m3
        radian,     // Pi angle.

        // Medical.

        bpm,        // something per minute. Beats, breaths, etc.
        bmi,        // kg weight per m2 = density.
        bp,         // blood pressure = mmHg (Systolic/Diastolic)
        SpO2,       // %. Pulse oximetry. https://en.wikipedia.org/wiki/Pulse_oximetry
        Glucose,    // mg/dL


    }

    public class DataValue
    {
        // A single point of data from the real world.
        // a sample, measurement, reading, point, value,

        DataUnit Unit { get; set; }
        object Value { get; set; }
    }
}
