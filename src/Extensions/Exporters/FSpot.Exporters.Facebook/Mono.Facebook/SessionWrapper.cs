//
// Mono.Facebook.SessionWrapper.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Mono.Facebook
{
	internal interface SessionWrapper
	{
		FacebookSession Session { get; set; }
	}
}
