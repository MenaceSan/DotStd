using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// Helpers for drivers license.
    /// </summary>
    public class DriversLicense
    {
        public const int k_DLicense_MaxLen = 21;   // there are no states with DLNum len > 21 (NY)

        public static string? GetValidDriversLicense(string sDLNum, GeoStateId stateId, out string sFailure)
        {
            // Is this a generally validate Drivers license format ? Is it valid for a particular issuing state ?
            // Mostly for the US and Canada.
            // RETURN: null = cant make this valid.
            // Assume All DLNums are greater than 2 chars, and less than k_DLNum_MaxLen chars. (1 digit is technically allowed in some states. ignore this)
            // All contain digits and letters. WA can contain *.
            // some contain '-'. we strip these. always strip all spaces for compare purposes.
            // https://insurancelink.custhelp.com/app/answers/detail/a_id/1631/~/license-formats-for-individual-states
            // http://www.diogenesllc.com/stdlformats.html
            // TODO : Some of these have check digits etc that can be validated.

            if (stateId == GeoStateId.WA)   // WA Must allow *
            {
                // Some special chars are valid.
                sDLNum = System.Text.RegularExpressions.Regex.Replace(sDLNum, @"[^\*A-Za-z0-9]", "");  // strip all but letters and numbers and *.
            }
            else
            {
                sDLNum = StringUtil.GetAlphaNumericOnly(sDLNum);  // strip all but letters and numbers. They may be present but ignored.
            }
            if (sDLNum.Length < 1)
            {
                sFailure = "DLNum doesn't contain enough characters";
                return null;
            }

            bool bMustStartWithAlpha = false;
            int nOnlyDigitsAfterX = 128;    // Has No effect by default.
            int nLengthMin = 2;
            int nLengthMax = k_DLicense_MaxLen;    // there are no states with DLnum > 21 (NY)

            // Australia ?

            switch (stateId)
            {
                case GeoStateId.AK:
                    // The license number format for Alaska must be 1 to 7 numeric characters. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = 1;
                    nLengthMax = 7;
                    break;
                case GeoStateId.AL:
                    // The license number format for Alabama is 7 numeric characters.
                    // NOTE: If the driver's license number contains zeroes, they must be present for a HIT. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = nLengthMax = 7;
                    break;
                case GeoStateId.AR:
                    // Arkansas = 8 or 9 numeric characters. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = 8;
                    nLengthMax = 9;
                    break;
                case GeoStateId.AZ:
                    // Arizona = 1 alpha (either A, B, D, or Y) followed by 8 numeric characters.  OR 9 numeric characters. 
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 9;
                    break;
                case GeoStateId.CA:
                    // California license format is 1 alpha followed by 7 numeric characters. 
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 8;
                    break;
                case GeoStateId.CO:
                    // There are 2 license formats in Colorado.
                    // 1. 1 to 2 alpha characters followed by 1 to 6 numeric characters.
                    // 2. 9 numeric characters (Not the social security number). 
                    if (sDLNum.Length <= 8)
                    {
                        bMustStartWithAlpha = true;
                        nOnlyDigitsAfterX = 2;
                        nLengthMin = 2;
                        nLengthMax = 8;
                    }
                    else
                    {
                        nOnlyDigitsAfterX = 0;
                        nLengthMin = nLengthMax = 9;
                    }
                    break;
                case GeoStateId.CT:
                    // Connecticut license format is 9 numeric characters.
                    // TODO = The first 2 digits can not be less than '01' or greater than '24'. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = nLengthMax = 9;
                    break;
                case GeoStateId.DE:
                    // The Delaware license format is 1 to 7 numeric characters. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = 2;
                    nLengthMax = 7;
                    break;
                case GeoStateId.FL:
                    // FL = "A123456789012", 1 Letter + 12 digits. normally. 
                    // The Florida license format is 1 alpha followed by 11-12 digits. 
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = 12;
                    nLengthMax = 13;
                    break;
                case GeoStateId.GA:
                    // Georgia license format is 9 numeric characters. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = nLengthMax = 9;
                    break;
                case GeoStateId.IA:
                    // Iowa. There are 2 license formats in Iowa. 1. 9 alpha or numeric characters. 2. 9 numeric characters. 
                    nLengthMin = nLengthMax = 9;
                    break;
                case GeoStateId.ID:
                    // Idaho license formats:
                    // 1. 9 numeric characters.
                    // 2. 2 alpha characters followed by 6 numeric characters followed by an alpha character.
                    // 3. 1-2 alpha followed by 3-8 numeric characters followed by an alpha character. 
                    nLengthMin = 4;
                    nLengthMax = 9;
                    break;
                case GeoStateId.IL:
                    // First Letter Of Last Name And 11 Digits
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 12;
                    break;
                case GeoStateId.IN:
                    // There are 2 license formats in Indiana.
                    // 1. 10 numeric characters.
                    // 2. 1 alpha followed by 9 numeric characters. 
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 10;
                    break;
                case GeoStateId.KS: // Kansas
                    // There are 3 license formats in Kansas.
                    // 1. 9 numeric characters.
                    // 2. 1 alpha followed by 8 numeric characters.
                    // 3. 6 alternating alpha and numeric characters. 
                    nLengthMin = 6;
                    nLengthMax = 9;
                    if (sDLNum.Length == 7 || sDLNum.Length == 8)
                    {
                        sFailure = "KS DLNum must be 6 or 9 characters";
                        return null;
                    }
                    break;
                case GeoStateId.KY:
                    // There are 2 license formats in Kentucky.
                    // 1. 9 numeric characters.
                    // 2. 1 alpha followed by 8 numeric characters. 
                    nLengthMin = nLengthMax = 9;
                    break;
                case GeoStateId.LA:
                    break;
                case GeoStateId.MA:
                    //  9 numeric characters. OR 1 alpha followed by 8 numeric characters (Note: the first position may not be 'X'). 
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = 9;
                    nLengthMax = 9;
                    break;
                case GeoStateId.MD:
                    // The Maryland license consists of 1 alpha followed by 12 digits. 
                    // The number is derived from the users name + some digits for uniqueness. Full validation should include this?
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 13;
                    break;
                case GeoStateId.MI:
                    // Michigan license number format is 1 alpha character followed by 12
                    // numeric characters. The first three numeric characters cannot be
                    // 7, 8, or 9. (except for range 726 - 750) 
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 13;
                    break;
                case GeoStateId.MN:
                    // Minnesota license number format is 1 alpha character followed by 12 numeric characters. 
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 13;
                    break;
                case GeoStateId.MO:
                    // Missouri license formats (as of 01/27/95):
                    // 1)  7-10 positions
                    //          1st position alpha
                    //          2-10 positions alphanumeric or spaces
                    // 2)  9 positions numeric
                    // 3)  10 positions
                    //          1-9 positions numeric
                    //          10th position alpha
                    // 
                    // Other license formats supported for now:
                    // 1)  16 positions
                    //          1st position alpha
                    //          2-16 positions numeric
                    // 2)  17 positions
                    //          1st position alpha
                    //          2-17 positions numeric
                    // 3)  17 positions
                    //          1st position alpha
                    //          2-16 positions numeric
                    //          17th position alpha 
                    nLengthMin = 7;
                    nLengthMax = 17;
                    break;
                case GeoStateId.MS:
                    // Mississippi license format is 9 numeric characters. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = nLengthMax = 9;
                    break;
                case GeoStateId.MT:
                    // Montana 
                    break;
                case GeoStateId.NE:
                    // Nebraska license format is 1 alpha followed by 3 to 8 numeric characters. (typically 8?)
                    bMustStartWithAlpha = true;
                    if (!("ABCEGHPRVZ".Contains(sDLNum[0].ToString())))
                    {
                        sFailure = "NE Drivers license must begin with A,B,C,E,G,H,P,R,V or Z";
                        return null;
                    }

                    nOnlyDigitsAfterX = 1;
                    nLengthMin = 9; // some docs say this is 4. db sometimes has 8. But NE regs say all numbers are 9 ?
                    nLengthMax = 9;
                    break;
                case GeoStateId.NM:
                    // New Mexico license format is 8 or 9 digits. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = 8;
                    nLengthMax = 9;
                    break;
                case GeoStateId.NJ:
                    break;
                case GeoStateId.NY:
                    // TODO
                    // There are 2 license formats in New York.
                    // 1. 1 position - alpha (prefix not required)
                    //      16 positions - numeric (required)
                    //      1-5 positions - numeric (suffix not required)
                    //      (Position 16 is a check digit)
                    //      NOTE: Although the physical license number is 18
                    //      to 21 numeric positions, the DMV allows only
                    //      the first 16 numeric positions on input.
                    //      1st position alpha
                    // 2. 9 numeric characters (all new licenses after X date)
                    // 
                    // Another source says : 1Alpha+7Numeric or
                    // 1Alpha+18Numeric or
                    // 8Numeric or
                    // 9Numeric or
                    // 16 Numeric
                    // or 8Alpha 
                    nLengthMin = 8;
                    nLengthMax = 21;
                    break;
                case GeoStateId.OK:  // Oklahoma
                    break;
                case GeoStateId.OH:  // Ohio
                    break;
                case GeoStateId.ON:    // Ontario
                    // 15 positions.    1st position alpha.    2-15 positions numeric 
                    // May contain 2*'-' chars which we strip.
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 15;
                    break;
                case GeoStateId.OR:  // Oregon
                    break;
                case GeoStateId.PA:
                    break;
                case GeoStateId.RI:
                    break;
                case GeoStateId.SC:
                    // South Carolina license format is 1 to 10 digits. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = 6;     // We typically see >= 6 
                    nLengthMax = 10;
                    break;
                case GeoStateId.SD:
                    break;
                case GeoStateId.TN:
                    // Tennessee license formats: 1. 8 numeric characters. 2. 9 numeric characters. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = 8;
                    nLengthMax = 9;
                    break;
                case GeoStateId.TX:
                    // Texas license format is 8 numeric characters. 
                    nOnlyDigitsAfterX = 0;
                    nLengthMin = nLengthMax = 8;
                    break;
                case GeoStateId.UT:
                    break;
                case GeoStateId.VA:
                    break;
                case GeoStateId.VT:
                    break;
                case GeoStateId.WA:
                    // TODO
                    // Washington license format is 12 alpha or numeric characters
                    // 1-7 positions alpha or asterisks.
                    // 8th position alphanumeric character.
                    // 9th positions numeric
                    // 10th position numeric or asterisks.
                    // 11-12 positions alphanumeric characters
                    // NOTE: Position 10 is a check digit. 
                    nLengthMin = nLengthMax = 12;
                    break;

                case GeoStateId.WI:
                    // Wisconsin license format is 1 alpha character followed by 13 numeric characters.
                    // Position 13 is a duplicate tie breaker and position 14 is a check digit.
                    // The check digit is validated. 
                    bMustStartWithAlpha = true;
                    nOnlyDigitsAfterX = 1;
                    nLengthMin = nLengthMax = 14;
                    break;

                case GeoStateId.WV:
                    break;
                case GeoStateId.WY:  // Wyoming
                    break;
                default:
                    // NOT a valid state ?
                    break;
            }

            if (nLengthMin == nLengthMax && sDLNum.Length != nLengthMax)
            {
                sFailure = stateId + " DLNum must be " + nLengthMin.ToString() + " characters";
                return null;
            }
            if (sDLNum.Length < nLengthMin) // do this last since we may have trimmed spaces etc.
            {
                sFailure = stateId + " DLNum must be at least " + nLengthMin.ToString() + " characters";
                return null;
            }
            if (sDLNum.Length > nLengthMax) // do this last since we may have trimmed spaces etc.
            {
                sFailure = stateId + " DLNum must be less than " + nLengthMax.ToString() + " characters";
                return null;
            }
            if (bMustStartWithAlpha && !char.IsLetter(sDLNum[0]))   // Assume ToUpper called.
            {
                sFailure = stateId + " DLNum must start with a character";
                return null;
            }

            // Ends in digits?
            for (int i = nOnlyDigitsAfterX; i < sDLNum.Length; i++)
            {
                if (!char.IsDigit(sDLNum[i]))
                {
                    if (nOnlyDigitsAfterX == 0)
                        sFailure = stateId + " DLNum must contain only digits";
                    else
                        sFailure = stateId + " DLNum must contain only digits after " + nOnlyDigitsAfterX.ToString() + " char";
                    return null;
                }
            }

            sFailure = "";
            return sDLNum;  // All good.
        }
    }
}
