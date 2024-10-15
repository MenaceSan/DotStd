using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// Encode a version as a string, a System.Version or as a single int.
    /// e.g. Version=4.0.0.0
    /// </summary>
    public static class VersionUtil
    {
        /// <summary>
        /// take a version integer (for compare) and make a 3 unit string for display. Like System.Version.
        /// Format X.2.3 decimal digits. (Major.Minor.Build)
        /// similar to System.Version
        /// </summary>
        /// <param name="versionInt"></param>
        /// <returns></returns>
        public static string ToVersionStr(int versionInt)
        {
            return $"{versionInt / 100000}.{(versionInt / 1000) % 100}.{versionInt % 1000}";
        }

        public static string ToVersionStr(int versionInt, string revisionId)
        {
            // similar to System.Version. Add the 4th place.
            // revisionId = last (4th) place of the version. Can have a string. source control revision tag. Might not be a number. $WCREV$ from SubWCRev
            string ver = ToVersionStr(versionInt);
            if (revisionId != null)
            {
                ver += "." + revisionId;
            }
            return ver;
        }

        public static int ToVersion32(int major, int minor, int build)
        {
            // Make a single version int from the standard 3 part version string.
            return major * 100000 + minor * 1000 + build;
        }
        public static int ToVersion32(System.Version v)
        {
            // Make a single version int from the standard 3 part version string. leave off "Revision"
            return ToVersion32(v.Major, v.Minor, v.Build);
        }
        public static int ToVersion32(string v)
        {
            // Make a single version int from the standard 3 part version string.

            string[] avs = v.Split('.');
            int[] avi = new int[3];
            for (int i = 0; i < avs.Length; i++)
            {
                if (!int.TryParse(avs[i], out avi[i]))
                    break;
            }
            return ToVersion32(avi[0], avi[1], avi[2]);
        }
    }
}
