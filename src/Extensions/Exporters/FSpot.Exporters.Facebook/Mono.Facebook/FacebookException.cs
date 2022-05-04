//
// Mono.Facebook.FacebookException.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

public class FacebookException : Exception
{
	private int error_code;
	private string error_message;

	public int ErrorCode {
		get { return error_code; }
	}

	public string ErrorMessage {
		get { return error_message; }
	}

	public FacebookException (int error_code, string error_message)
		: base (CreateMessage (error_code, error_message))
	{
		this.error_code = error_code;
		this.error_message = error_message;
	}

	private static string CreateMessage (int error_code, string error_message)
	{
		return string.Format ("Code: {0}, Message: {1}", error_code, error_message);
	}
}
