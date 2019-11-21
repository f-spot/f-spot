//
// Paths.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2005-2008 Novell, Inc.
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

using System.IO;
using System;
using System.Text;

namespace Hyena
{
    public static class Paths
    {
        public const char UnixSeparator = ':';
        public const char DosSeparator = ';';

        public static class Folder
        {
            public const char UnixSeparator = '/';
            public const char DosSeparator = '\\';
        }

        public static string GetTempFileName (string dir)
        {
            return GetTempFileName (dir, null);
        }

        public static string GetTempFileName (string dir, string extension)
        {
            return GetTempFileName (new DirectoryInfo (dir), extension);
        }

        public static string GetTempFileName (DirectoryInfo dir, string extension)
        {
            string path = null;

            if (dir == null || !dir.Exists) {
                throw new DirectoryNotFoundException ();
            }

            do {
                string guid = Guid.NewGuid ().ToString ();
                string file = extension == null ? guid : String.Format ("{0}.{1}", guid, extension);
                path = Path.Combine (dir.FullName, file);
            } while (File.Exists (path));

            return path;
        }

        public static string Combine (string first, params string [] components)
        {
            if (String.IsNullOrEmpty (first)) {
                throw new ArgumentException ("First component must not be null or empty", "first");
            } else if (components == null || components.Length < 1) {
                throw new ArgumentException ("One or more path components must be provided", "components");
            }

            string result = first;

            foreach (string component in components) {
                result = Path.Combine (result, component);
            }

            return result;
        }

        public static string FindProgramInPath (string command)
        {
            foreach (string path in GetExecPaths ()) {
                string full_path = Path.Combine (path, command);
                try {
                    FileInfo info = new FileInfo (full_path);
                    // FIXME: System.IO is super lame, should check for 0755
                    if (info.Exists) {
                        return full_path;
                    }
                } catch {
                }
            }

            return null;
        }

        private static string [] GetExecPaths ()
        {
            string path = Environment.GetEnvironmentVariable ("PATH");
            if (String.IsNullOrEmpty (path)) {
                return new string [] { "/bin", "/usr/bin", "/usr/local/bin" };
            }

            // this is super lame, should handle quoting/escaping
            return path.Split (UnixSeparator);
        }

        public static string SwitchRoot (string path, string mountPoint, string rootPath)
        {
            if (!path.StartsWith (mountPoint)) {
                throw new ArgumentException ("mountPoint must be contained in first part of the path");
            }
            return path.Replace (mountPoint, rootPath);
        }

        public static string MakePathRelative (string path, string to)
        {
            if (String.IsNullOrEmpty (path) || String.IsNullOrEmpty (to)) {
                return null;
            }

            if (Path.IsPathRooted (path) ^ Path.IsPathRooted (to))
            {
                // one path is absolute, one path is relative, impossible to compare
                return null;
            }

            if (path == to) {
                return String.Empty;
            }

            if (to[to.Length - 1] != Path.DirectorySeparatorChar) {
                to = to + Path.DirectorySeparatorChar;
            }

            if (path.StartsWith (to))
            {
                return path.Substring (to.Length);
            }

            return BuildRelativePath (path, to);
        }

        private static string BuildRelativePath (string path, string to)
        {
            var toParts = to.Split (Path.DirectorySeparatorChar);
            var pathParts = path.Split (Path.DirectorySeparatorChar);

            var i = 0;
            while (i < toParts.Length && i < pathParts.Length && toParts [i] == pathParts [i]) {
                i++;
            }

            var relativePath = new StringBuilder ();
            for (int j = 0; j < toParts.Length - i - 1; j++) {
                relativePath.Append ("..");
                relativePath.Append (Path.DirectorySeparatorChar);
            }

            var required = new string [pathParts.Length - i];
            for (int j = i; j < pathParts.Length; j++) {
                required [j - i] = pathParts [j];
            }
            relativePath.Append (String.Join (Path.DirectorySeparatorChar.ToString (), required));

            return relativePath.ToString ();
        }

        public static string NormalizeToDos (string path)
        {
            return path.Replace (Folder.UnixSeparator, Folder.DosSeparator);
        }

        public static string NormalizeToUnix (string path)
        {
            if (!path.Contains (Folder.UnixSeparator.ToString ()) && path.Contains (Folder.DosSeparator.ToString ())) {
                return path.Replace (Folder.DosSeparator, Folder.UnixSeparator);
            }
            return path;
        }

        public static string ApplicationData {
            get; private set;
        }

        public static string ApplicationCache {
            get; private set;
        }

        private static string application_name = null;

        public static string ApplicationName {
            get {
                if (application_name == null) {
                    throw new ApplicationException ("Paths.ApplicationName must be set first");
                }
                return application_name;
            }
            set { application_name = value; InitializePaths (); }
        }

        private static string user_application_name = null;
        public static string UserApplicationName {
            get {
                var application_name = user_application_name ?? ApplicationName;
                if (application_name == null) {
                    throw new ApplicationException ("Paths.ApplicationName must be set first");
                }
                return application_name;
            }
            set { user_application_name = value; }
        }

        // This can only happen after ApplicationName is set.
        private static void InitializePaths ()
        {
            ApplicationCache = Path.Combine (XdgBaseDirectorySpec.GetUserDirectory (
                "XDG_CACHE_HOME", ".cache"), UserApplicationName);

            ApplicationData = Path.Combine (Environment.GetFolderPath (
                Environment.SpecialFolder.ApplicationData), UserApplicationName);
            if (!Directory.Exists (ApplicationData)) {
                Directory.CreateDirectory (ApplicationData);
            }
        }


        public static string ExtensionCacheRoot {
            get { return Path.Combine (ApplicationCache, "extensions"); }
        }

        public static string SystemTempDir {
            get { return "/tmp/"; }
        }

        public static string TempDir {
            get {
                string dir = Path.Combine (ApplicationCache, "temp");

                // If this location exists, but as a file not a directory, delete it
                if (File.Exists (dir)) {
                    File.Delete (dir);
                }

                Directory.CreateDirectory (dir);
                return dir;
            }
        }

        private static string installed_application_prefix = null;
        public static string InstalledApplicationPrefix {
            get {
                if (installed_application_prefix == null) {
                    installed_application_prefix = Path.GetDirectoryName (
                        System.Reflection.Assembly.GetExecutingAssembly ().Location);

                    // For Banshee on Linux running uninstalled, share/ is located within the assembly's dir
                    if (Directory.Exists (Paths.Combine (installed_application_prefix, "share", ApplicationName))) {
                        return installed_application_prefix;
                    }

                    // For Banshee on Windows, share/ is one up from bin/ where the assembly is located
                    if (Directory.Exists (Paths.Combine (installed_application_prefix, "..", "share", ApplicationName))) {
                        return installed_application_prefix = new DirectoryInfo (installed_application_prefix).Parent.FullName;
                    }

                    DirectoryInfo entry_directory = new DirectoryInfo (installed_application_prefix);

                    if (entry_directory != null && entry_directory.Parent != null && entry_directory.Parent.Parent != null) {
                        installed_application_prefix = entry_directory.Parent.Parent.FullName;
                    }
                }

                return installed_application_prefix;
            }
        }

        public static string InstalledApplicationDataRoot {
            get { return Path.Combine (InstalledApplicationPrefix, "share"); }
        }

        public static string InstalledApplicationData {
            get { return Path.Combine (InstalledApplicationDataRoot, ApplicationName); }
        }

        public static string GetInstalledDataDirectory (string path)
        {
            return Path.Combine (InstalledApplicationData, path);
        }
    }
}
