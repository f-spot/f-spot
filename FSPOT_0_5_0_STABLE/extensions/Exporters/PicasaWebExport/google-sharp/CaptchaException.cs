//
// Mono.Google.CaptchaException
//
// Authors:
// 	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2006 Novell, Inc.  (http://www.novell.com)
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
