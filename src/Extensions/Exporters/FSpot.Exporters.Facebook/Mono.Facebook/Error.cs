//
// Mono.Facebook.FacebookException.cs:
//
// Authors:
//	George Talusan (george@convolve.ca)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Xml.Serialization;

namespace Mono.Facebook
{
	[XmlRoot ("error_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class Error
	{
		[XmlElement ("error_code")]
		public int ErrorCode;

		[XmlElement ("error_msg")]
		public string ErrorMsg;
	}
}
