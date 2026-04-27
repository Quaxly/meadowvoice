using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace meadowvoice
{
    internal static class Natives
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string path);

        [DllImport("libdl")]
        private static extern IntPtr dlopen(string path, int flags);

        public static void LoadNatives()
        {
            string path = ModManager.ActiveMods.FirstOrDefault(x => x.id == "meadowvoice").basePath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var arch = RuntimeInformation.OSArchitecture;
                var platform = "win-x64";

                if (arch == Architecture.X86)
                {
                    platform = "win-x86";
                }

                var nativePath = path + Path.DirectorySeparatorChar.ToString() +
                    "plugins" + Path.DirectorySeparatorChar.ToString() +
                    "runtimes" + Path.DirectorySeparatorChar.ToString() +
                    platform + Path.DirectorySeparatorChar.ToString() +
                    "native" + Path.DirectorySeparatorChar.ToString() +
                    "opus.dll";
                if (!File.Exists(nativePath) || LoadLibrary(nativePath) == IntPtr.Zero)
                {
                    RainMeadow.RainMeadow.Error($"Failed to load opus.dll from {nativePath}. Failed with Win32 error code {Marshal.GetLastWin32Error()}.");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var arch = RuntimeInformation.OSArchitecture;
                var platform = "linux-x64";

                if (arch == Architecture.X86)
                {
                    platform = "linux-x86";
                }

                var nativePath = path + Path.DirectorySeparatorChar.ToString() +
                    "plugins" + Path.DirectorySeparatorChar.ToString() +
                    "runtimes" + Path.DirectorySeparatorChar.ToString() +
                    platform + Path.DirectorySeparatorChar.ToString() +
                    "native" + Path.DirectorySeparatorChar.ToString() +
                    "opus.so";
                if (!File.Exists(nativePath) || dlopen(nativePath, 2) == IntPtr.Zero)
                {
                    RainMeadow.RainMeadow.Error($"Failed to load opus.so from {nativePath}.");
                }
            }
        }
    }
}
