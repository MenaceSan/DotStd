using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    public class VersionUtil
    {
        // Encode a version as a string, a System.Version or as a single int.
        // e.g. Version=4.0.0.0

        public static string ToVersionStr(int versionInt)
        {
            // take a version integer (for compare) and make a string for display. Like System.Version.
            // Format X.2.3 decimal digits. (Major.Minor.Build)
            // kVersionInt to kVersionStr
            // similar to System.Version

            return $"{versionInt / 100000}.{(versionInt / 1000) % 100}.{versionInt % 1000}";
        }

        public static int ToVersionInt(int major, int minor, int build)
        {
            // Make a single version int from the standard 3 part version string.
            return major * 100000 + minor * 1000 + build;
        }
        public static int ToVersionInt(System.Version v)
        {
            // Make a single version int from the standard 3 part version string.
            return ToVersionInt(v.Major, v.Minor, v.Build);
        }
        public static int ToVersionInt(string v)
        {
            // Make a single version int from the standard 3 part version string.

            string[] avs = v.Split('.');
            int[] avi = new int[3];
            for (int i = 0; i < avs.Length; i++)
            {
                if (!int.TryParse(avs[i], out avi[i]))
                    break;
            }
            return ToVersionInt(avi[0], avi[1], avi[2]);
        }
    }
}
