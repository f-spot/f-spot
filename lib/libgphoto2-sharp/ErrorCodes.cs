/*
 * ErrorCodes.cs
 *
 * Author(s):
 *	Ewen Cheslack-Postava <echeslack@gmail.com>
 *	Larry Ewing <lewing@novell.com>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2005-2009 Novel, Inc.
 *
 * This is open source software. See COPYING for details.
 */
using System;
using System.Runtime.InteropServices;

namespace GPhoto2
{
	public enum ErrorCode
	{
		/* IO Errors */
		GeneralError		= -1,
		BadParameters		= -2,
		NoMemory		= -3,
		Library			= -4,
		UnknownPort		= -5,
		NotSupported		= -6,
		IO			= -7,
		FixedLimitExceeded	= -8,
		Timout			= -10,
		SupportedSerial		= -20,
		SupportedUSB		= -21,
		Init			= -31,
		Read			= -34,
		Write			= -35,
		Update			= -37,
		SerialSpeed		= -41,
		USBClearHalt		= -51,
		USBFind			= -52,
		USBClaim		= -53,
		Lock			= -60,
		Hal			= -70,

		/* Other Errors*/
		CorruptedData		= -102,
		FileExists		= -103,
		ModelNotFound		= -105,
		DirectoryNotFound	= -107,
		FileNotFound		= -108,
		DirectoryExists		= -109,
		CameraBusy		= -110,
		PathNotAbsolute		= -111,
		Cancel			= -112,
		CameraError		= -113,
		OsFailure		= -114,
	}

	public static class Error
	{
		public static bool IsError (ErrorCode error_code)
		{
			return (error_code < 0);
		}
		
		public static int CheckError (ErrorCode error)
		{
			if (IsError (error)) {
				string message = "Unknown Error";
				
				if ((int)error <= -100)
					message = GetErrorAsString (error);
				else if ((int)error <= -1 && (int)error >= -99)
					message = GetIOErrorAsString (error);
	
				throw new GPhotoException (error, message);
			}
			
			return (int)error;
		}
		
		[DllImport ("libgphoto2.so")]
		internal static extern IntPtr gp_result_as_string (ErrorCode result);
		
		static string GetErrorAsString (ErrorCode e)
		{
			IntPtr raw_message = gp_result_as_string(e);
			return Marshal.PtrToStringAnsi(raw_message);
		}

		[DllImport ("libgphoto2_port.so")]
		internal static extern IntPtr gp_port_result_as_string (ErrorCode result);

		static string GetIOErrorAsString(ErrorCode e)
		{
			IntPtr raw_message = gp_port_result_as_string(e);
			return Marshal.PtrToStringAnsi(raw_message);
		}
		
	}
	
	public class GPhotoException : Exception
	{
		private ErrorCode error;
		
		public GPhotoException(ErrorCode error_code) : base ("Unknown Error.")
		{
			error = error_code;
		}
		
		public GPhotoException (ErrorCode error_code, string message) : base (message)
		{
			error = error_code;
		}
		
		public override string ToString()
		{
			return ("Error " + error.ToString() + ": " + base.ToString());
		}

		public ErrorCode Error {
			get { return error; }
		}
	}
}
