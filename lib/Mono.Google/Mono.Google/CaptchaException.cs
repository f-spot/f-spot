//
// Mono.Google.CaptchaException
//
// Authors:
// 	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2006 Novell, Inc.  (http://www.novell.com)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Mono.Google {
	[Serializable]
	public class CaptchaException : UnauthorizedAccessException, ISerializable
	{
		public static string UnlockCaptchaURL = "https://www.google.com/accounts/DisplayUnlockCaptcha";
		string url;
		string token;
		string captcha_url;

		public CaptchaException ()
		{
		}

		public CaptchaException (string url, string token, string captcha_url)
		{
			this.url = url;
			this.token = token;
			this.captcha_url = captcha_url;
		}

		protected CaptchaException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
			if (info == null)
				throw new ArgumentNullException ("info");

			url = info.GetString ("url");
			token = info.GetString ("token");
			captcha_url = info.GetString ("captcha_url");
		}

		void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException ("info");

			base.GetObjectData (info, context);
			info.AddValue ("url", url);
			info.AddValue ("token", token);
			info.AddValue ("captcha_url", captcha_url);
		}

		public string Url {
			get { return url; }
		}

		public string Token {
			get { return token; }
		}

		public string CaptchaUrl {
			get { return captcha_url; }
		}
	}
}
