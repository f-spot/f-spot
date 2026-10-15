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
				if (string.Equals (Canonical (left), Canonical (right), Comparison))
					return true;
				return SameNode (left, right);
			} catch (Exception) {
				return false;
			}
		}

		// realpath keeps two names for one inode distinct. A hard link of the open catalog is still that catalog.
		static bool SameNode (string left, string right)
		{
			if (Path.DirectorySeparatorChar == '\\')
				return false;
			if (!TryNode (left, out ulong device, out ulong inode))
				return false;
			if (!TryNode (right, out ulong otherDevice, out ulong otherInode))
				return false;
			return device == otherDevice && inode == otherInode;
		}

		static bool TryNode (string path, out ulong device, out ulong inode)
		{
			device = 0;
			inode = 0;
			var buffer = new byte[256];
			if (stat (Encoding.UTF8.GetBytes (path + "\0"), buffer) != 0)
				return false;
			// Darwin st_dev is 4 bytes at 0 and st_ino is 8 bytes at 8. The 8 bytes at 0
			// also cover mode and link count, which a hard link shares. Linux stores
			// st_dev and st_ino as the 8-byte fields at those same offsets.
			device = ReadLittle (buffer, 0);
			inode = ReadLittle (buffer, 8);
			return inode != 0;
		}

		static ulong ReadLittle (byte[] buffer, int offset)
		{
			ulong value = 0;
			for (int i = 0; i < 8; i++)
				value |= (ulong)buffer[offset + i] << (8 * i);
			return value;
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
				return WindowsCanonical (full);
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

		// GetFullPath leaves a directory symlink in place, so a name inside the chosen
		// folder can still point at another drive. net472 has no ResolveLinkTarget.
		static string WindowsCanonical (string full)
		{
#if NET5_0_OR_GREATER
			var pending = new Stack<string> ();
			string current = full;
			while (true) {
				if (Directory.Exists (current) || File.Exists (current)) {
					string resolved = WindowsFinal (current);
					while (pending.Count > 0)
						resolved = Path.Combine (resolved, pending.Pop ());
					return resolved;
				}
				string name = Path.GetFileName (current);
				string parent = Path.GetDirectoryName (current);
				if (string.IsNullOrEmpty (parent) || string.IsNullOrEmpty (name) || parent == current)
					return full;
				pending.Push (name);
				current = parent;
			}
#else
			return full;
#endif
		}

#if NET5_0_OR_GREATER
		static string WindowsFinal (string path)
		{
			// ResolveLinkTarget throws on a Windows drive root, and Same would then
			// treat one missing file as a different file from itself.
			if (IsVolumeRoot (path))
				return Path.GetFullPath (path);
			FileSystemInfo target = null;
			try {
				if (Directory.Exists (path))
					target = Directory.ResolveLinkTarget (path, returnFinalTarget: true);
				else if (File.Exists (path))
					target = File.ResolveLinkTarget (path, returnFinalTarget: true);
			} catch (Exception) {
				return Path.GetFullPath (path);
			}
			if (target == null)
				return Path.GetFullPath (path);
			return target.FullName;
		}

		static bool IsVolumeRoot (string path)
		{
			string full = TrimSeparator (Path.GetFullPath (path));
			string root = TrimSeparator (Path.GetPathRoot (full) ?? "");
			return root.Length > 0 && string.Equals (full, root, StringComparison.OrdinalIgnoreCase);
		}

		static string TrimSeparator (string path)
			=> path.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
#endif

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

		[DllImport ("libc", EntryPoint = "stat")]
		static extern int stat (byte[] path, byte[] buffer);

		[DllImport ("libc", EntryPoint = "realpath")]
		static extern IntPtr realpath (byte[] path, IntPtr resolved);

		[DllImport ("libc", EntryPoint = "free")]
		static extern void free (IntPtr pointer);
	}
}
