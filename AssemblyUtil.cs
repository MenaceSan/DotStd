using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DotStd
{

    public static class AssemblyUtil
    {
        // helper for dealing with assemblies.

        public static Assembly GetAssemblySafe(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly;
        }

        public static string GetAssemblyPath(Assembly assembly)
        {
            // Get path for the assembly.
            // TODO Q: Is this the same as assembly.Location ??
            assembly = GetAssemblySafe(assembly);
            var uriBuilder = new UriBuilder(assembly.CodeBase);
            return Uri.UnescapeDataString(uriBuilder.Path);
        }

        public static string GetAssemblyDirectory(Assembly assembly)
        {
            // What directory is a particular assembly in ? used to find its particular config file.
            return Path.GetDirectoryName(GetAssemblyPath(assembly));
        }

        public static DateTime GetAssemblyLinkDate(Assembly assembly, TimeZoneInfo target = null)
        {
            // For display of the build date of some Assembly
            // Get from PE header. 
            // NOTE: .NET core doesnt use this, so just get file date.
            // https://upload.wikimedia.org/wikipedia/commons/1/1b/Portable_Executable_32_bit_Structure_in_SVG_fixed.svg

            assembly = GetAssemblySafe(assembly);

            const int kPeHeaderOffset = 60;    // 0x3c
            const int kLinkerTimestampOffset = 8;

            // MZ header = DOS header before PE prefix

            var fi = new FileInfo(assembly.Location);
            var buffer = new byte[2048];

            using (FileStream stream = fi.OpenRead()) 
            {
                stream.Read(buffer, 0, 2048);

                int offset = BitConverter.ToInt32(buffer, kPeHeaderOffset);    // start of PE
                int peSig = BitConverter.ToInt32(buffer, offset);   // Signature 0x50450000

                int secondsSince1970 = BitConverter.ToInt32(buffer, offset + kLinkerTimestampOffset);
                if (secondsSince1970 <= 0) // .NET CORE has junk value. secondsSince1970 == -1551948072
                {
                    // Just get the date on the file.

                    return fi.CreationTime;
                }
                else
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime linkTimeUtc = epoch.AddSeconds(secondsSince1970);
                    TimeZoneInfo tz = target ?? TimeZoneInfo.Local;
                    DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);
                    return localTime;
                }
            }
        }
    }
}
