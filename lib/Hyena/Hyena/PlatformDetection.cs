//
// PlatformUtil.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.InteropServices;

namespace Hyena
{
    public static class PlatformDetection
    {
        public static readonly bool IsMac;
        public static readonly bool IsWindows;
        public static readonly bool IsLinux;
        public static readonly bool IsUnix;
        public static readonly bool IsMeeGo;

        public static readonly string PosixSystemName;
        public static readonly string SystemName;

        [DllImport ("libc")]
        private static extern int uname (IntPtr utsname);

        static PlatformDetection ()
        {
            // From http://www.mono-project.com/FAQ:_Technical
            int p = (int)Environment.OSVersion.Platform;
            IsUnix = p == 4 || p == 6 || p == 128;
            IsWindows = p < 4;

            if (IsWindows) {
                SystemName = "Windows";
                return;
            }

            // uname expects a pointer to a utsname structure, but we are
            // tricky here - this structure's first field is the field we
            // care about (char sysname []); the size of the structure is
            // unknown, as it varies on all platforms. Darwin uses only
            // the five POSIX fields, each 256 bytes, so the total size is
            // total size is 5 * 256 = 1280 bytes. Arbitrarily using 8192.
            var utsname = IntPtr.Zero;
            try {
                utsname = Marshal.AllocHGlobal (8192);
                if (uname (utsname) == 0) {
                    PosixSystemName = Marshal.PtrToStringAnsi (utsname);
                }
            } catch {
            } finally {
                if (utsname != IntPtr.Zero) {
                    Marshal.FreeHGlobal (utsname);
                }
            }

            if (PosixSystemName == null) {
                if (IsUnix) {
                    SystemName = "Unix";
                }
                return;
            }

            switch (PosixSystemName) {
                case "Darwin": IsMac = true; break;
                case "Linux": IsLinux = true; break;
            }

            SystemName = PosixSystemName;

            IsMeeGo = System.IO.File.Exists ("/etc/meego-release");
        }
    }
}
