using System;
using System.Runtime.InteropServices;

namespace GLib
{
	public class Shell
	{
		[DllImport ("libglib-2.0-0.dll")]
		static extern IntPtr g_shell_quote (IntPtr unquoted_string);

		public static string Quote (string unquoted)
		{
			IntPtr native_string = GLib.Marshaller.StringToPtrGStrdup (unquoted);
			string quoted = GLib.Marshaller.PtrToStringGFree (g_shell_quote (native_string));
			GLib.Marshaller.Free (native_string);
			return quoted;
		}
	}
}
