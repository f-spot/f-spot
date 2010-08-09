//
// Mono.Facebook.SessionInfo.cs:
//
// Authors:
//	Ruben Vermeersch (ruben@savanne.be)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Xml.Serialization;
using Mono.Facebook.Schemas;

namespace Mono.Facebook
{
    [System.Xml.Serialization.XmlRootAttribute("auth_getSession_response", Namespace="http://api.facebook.com/1.0/", IsNullable=false)]
	public class SessionInfo : session_info
	{
		[XmlIgnore]
			public bool IsInfinite
			{
				get { return expires == 0; }
			}

		public SessionInfo ()
		{}

		// use this if you want to create a session based on infinite session
		// credentials
		public SessionInfo (string session_key, long uid, string secret)
		{
			this.session_key = session_key;
			this.uid = uid;
			this.secret = secret;
			this.expires = 0;
		}
	}
}
