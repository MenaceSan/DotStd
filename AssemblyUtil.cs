using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// helper for dealing with assemblies.
    /// </summary>
    public static class AssemblyUtil
    {
        /// <summary>
        /// Find assembly if its already loaded. Don't load it again.
        /// </summary>
        /// <param name="name"> GetName e.g. 'System.Web' ignores version .</param>
        public static Assembly? FindLoadedAssembly(string name)
        {
            // Assembly oAsm = Assembly.Load("System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly asm in asms)
            {
                var n = asm.GetName();
                if (n.Name == name)
                    return asm;
            }
            return null;
        }

        /// <summary>
        /// Get current executing assembly if one is not provided.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static Assembly GetAssemblySafe(Assembly? assembly = null)
        {
            if (assembly == null)   // default to current assembly
                assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly;
        }

        /// <summary>
        /// For display of the build date of some Assembly
        /// Get LOCAL DateTime from PE header. 
        /// NOTE: .NET core assembly doesn't use this anymore, so just get file date.
        ///  https://upload.wikimedia.org/wikipedia/commons/1/1b/Portable_Executable_32_bit_Structure_in_SVG_fixed.svg
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static async Task<DateTime> GetAssemblyLinkDate(Assembly assembly, TimeZoneInfo? target = null)
        {
            assembly = GetAssemblySafe(assembly);

            const int kPeHeaderOffset = 60;    // 0x3c
            const int kLinkerTimestampOffset = 8;

            // MZ header = DOS header before PE prefix

            var fi = new FileInfo(assembly.Location);
            var buffer = new byte[2048];

            using FileStream stream = fi.OpenRead();
            await stream.ReadAsync(buffer, 0, 2048);   // Read first block.

            int offset = BitConverter.ToInt32(buffer, kPeHeaderOffset);    // start of PE
            int peSig = BitConverter.ToInt32(buffer, offset);   // Signature 0x50450000

            int secondsSince1970 = BitConverter.ToInt32(buffer, offset + kLinkerTimestampOffset);
            if (secondsSince1970 <= 0) // .NET CORE has junk value. secondsSince1970 == -1551948072
            {
                // Just get the date on the file.
                return fi.CreationTime;
            }

            DateTime linkTimeUtc = DateUtil.kUnixEpoch.AddSeconds(secondsSince1970);
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, target ?? TimeZoneInfo.Local);
            return localTime;
        }
    }
}
