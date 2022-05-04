//
// Mono.Facebook.SessionInfo.cs:
//
// Authors:
//	Ruben Vermeersch (ruben@savanne.be)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
