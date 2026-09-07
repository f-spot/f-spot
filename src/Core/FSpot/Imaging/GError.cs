//
// GError.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using FSpot.Utils;

namespace FSpot.Imaging
{
	static class GError
	{
		[StructLayout (LayoutKind.Sequential)]
		struct Native
		{
			public uint Domain;
			public int Code;
			public IntPtr Message;
		}

		[DllImport ("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
		static extern void g_error_free (IntPtr error);

		[DllImport ("libglib-2.0-0.dll", EntryPoint = "g_source_remove", CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool RemoveSource (uint tag);

		public static void ThrowIfSet (IntPtr error)
		{
			if (error == IntPtr.Zero)
				return;

			var native = Marshal.PtrToStructure<Native> (error);
			string message = NativeString.FromUtf8 (native.Message) ?? "GLib error";
			g_error_free (error);
			throw new IOException (message);
		}
	}
}
