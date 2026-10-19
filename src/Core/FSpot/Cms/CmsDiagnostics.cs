// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using lcmsNET;

namespace FSpot.Cms
{
	// Little CMS reports a specific reason for every failure. Nothing was listening,
	// so a profile it declined became "Invalid Profile Data" with the reason dropped.
	public static class CmsDiagnostics
	{
		// The native side holds this pointer for the life of the process. A
		// handler that only lives on the stack is collected, and the next lcms
		// error then calls freed memory.
		static readonly ErrorHandler handler = OnError;

		[ThreadStatic]
		static string last;

		// Tests replace this. Production logs when it is left unset.
		public static Action<string> Report { get; set; }

		public static void Install ()
		{
			lcmsNET.Cms.SetErrorHandler (handler);
		}

		internal static string Take ()
		{
			string value = last;
			last = null;
			return value;
		}

		static void OnError (IntPtr contextId, int errorCode, string errorText)
		{
			last = errorText ?? string.Empty;
			Action<string> report = Report;
			if (report != null)
				report (last);
			else if (last.Length > 0)
				Logger.Log.Warning ($"Little CMS: {last}");
		}
	}
}
