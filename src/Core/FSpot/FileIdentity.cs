// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FSpot
{
	// Path.GetFullPath leaves a directory symlink in place, so /tmp and /private/tmp
	// compare as different files. Catalog and export checks need the same file.
	static class FileIdentity
	{
		public static bool Same (string left, string right)
		{
			if (string.IsNullOrEmpty (left) || string.IsNullOrEmpty (right))
				return false;
			try {
				return string.Equals (Canonical (left), Canonical (right), Comparison);
			} catch (Exception) {
				return false;
			}
		}

		public static bool IsInside (string folder, string candidate)
		{
			if (string.IsNullOrWhiteSpace (folder) || string.IsNullOrWhiteSpace (candidate))
				return false;
			try {
				string root = Canonical (folder);
				string child = Canonical (candidate);
				string prefix = root.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
				return child.StartsWith (prefix, Comparison);
			} catch (Exception) {
				return false;
			}
		}

		static StringComparison Comparison
			=> Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

		static string Canonical (string path)
		{
			string full = Path.GetFullPath (path);
			if (Path.DirectorySeparatorChar == '\\')
				return full;
			// realpath fails for a segment that does not exist yet. Walk up to an
			// existing directory so the new name and its folder share one prefix.
			string resolved = UnixReal (full);
			if (resolved != null)
				return resolved;
			var pending = new Stack<string> ();
			string current = full;
			while (true) {
				string name = Path.GetFileName (current);
				string parent = Path.GetDirectoryName (current);
				if (string.IsNullOrEmpty (parent) || string.IsNullOrEmpty (name) || parent == current)
					return full;
				pending.Push (name);
				resolved = UnixReal (parent);
				if (resolved == null) {
					current = parent;
					continue;
				}
				while (pending.Count > 0)
					resolved = Path.Combine (resolved, pending.Pop ());
				return resolved;
			}
		}

		static string UnixReal (string path)
		{
			IntPtr native = realpath (Encoding.UTF8.GetBytes (path + "\0"), IntPtr.Zero);
			if (native == IntPtr.Zero)
				return null;
			try {
				return ReadUtf8 (native);
			} finally {
				free (native);
			}
		}

		static string ReadUtf8 (IntPtr native)
		{
			int length = 0;
			while (Marshal.ReadByte (native, length) != 0)
				length++;
			var bytes = new byte[length];
			Marshal.Copy (native, bytes, 0, length);
			return Encoding.UTF8.GetString (bytes);
		}

		[DllImport ("libc", EntryPoint = "realpath")]
		static extern IntPtr realpath (byte[] path, IntPtr resolved);

		[DllImport ("libc", EntryPoint = "free")]
		static extern void free (IntPtr pointer);
	}
}
