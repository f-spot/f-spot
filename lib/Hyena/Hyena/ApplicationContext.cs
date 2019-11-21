//
// ApplicationContext.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;

using Hyena.CommandLine;

namespace Hyena
{
    public delegate void InvokeHandler ();

    public static class ApplicationContext
    {
        public static readonly DateTime StartedAt = DateTime.Now;

        static ApplicationContext ()
        {
            Log.Debugging = Debugging;
        }

        private static CommandLineParser command_line = new CommandLineParser ();
        public static CommandLineParser CommandLine {
            set { command_line = value; }
            get { return command_line; }
        }

        public static string ApplicationName { get; set; }

        private static Layout command_line_layout;
        public static Layout CommandLineLayout {
            get { return command_line_layout; }
            set { command_line_layout = value; }
        }

        private static bool? debugging = null;
        public static bool Debugging {
            get {
                if (debugging == null) {
                    debugging = CommandLine.Contains ("debug");
                    debugging |= CommandLine.Contains ("debug-sql");
                    debugging |= EnvironmentIsSet ("BANSHEE_DEBUG");
                }

                return debugging.Value;
            }
            set {
                debugging = value;
                Log.Debugging = Debugging;
            }
        }

        public static bool EnvironmentIsSet (string env)
        {
            return !String.IsNullOrEmpty (Environment.GetEnvironmentVariable (env));
        }

        private static CultureInfo current_culture = CultureInfo.CurrentCulture;
        public static CultureInfo CurrentCulture {
            get { return current_culture; }
        }

        public static CultureInfo InternalCultureInfo {
            get { return CultureInfo.InvariantCulture; }
        }

        [DllImport ("libc")] // Linux
        private static extern int prctl (int option, byte [] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

        [DllImport ("libc")] // BSD
        private static extern void setproctitle (byte [] fmt, byte [] str_arg);

        private static void SetProcessName (string name)
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix) {
                return;
            }

            try {
                if (prctl (15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes (name + "\0"),
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0) {
                    throw new ApplicationException ("Error setting process name: " +
                        Mono.Unix.Native.Stdlib.GetLastError ());
                }
            } catch (EntryPointNotFoundException) {
                setproctitle (Encoding.ASCII.GetBytes ("%s\0"),
                    Encoding.ASCII.GetBytes (name + "\0"));
            }
        }

        public static void TrySetProcessName (string name)
        {
            try {
                SetProcessName (name);
            } catch {
            }
        }
    }
}
