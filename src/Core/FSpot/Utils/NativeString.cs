//
// NativeString.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FSpot.Utils
{
	static class NativeString
	{
		// Marshal.PtrToStringUTF8 is not available on net472, and PtrToStringAnsi decodes with
		// the platform default, so native UTF-8 strings are decoded explicitly here instead.
		public static string FromUtf8 (IntPtr native)
		{
			if (native == IntPtr.Zero)
				return null;

			int length = 0;
			while (Marshal.ReadByte (native, length) != 0)
				length++;

			if (length == 0)
				return string.Empty;

			var bytes = new byte[length];
			Marshal.Copy (native, bytes, 0, length);
			return Encoding.UTF8.GetString (bytes);
		}
	}
}
