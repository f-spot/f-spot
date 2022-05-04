//
// CryptoUtil.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Hyena
{
	public static class CryptoUtil
	{
		// A-Z is ignored on purpose
		static Regex md5_regex = new Regex ("^[a-f0-9]{32}$", RegexOptions.Compiled);
		static MD5 md5 = MD5.Create ();

		public static bool IsMd5Encoded (string text)
		{
			return text == null || text.Length != 32 ? false : md5_regex.IsMatch (text);
		}

		public static string Md5Encode (string text)
		{
			return Md5Encode (text, Encoding.ASCII);
		}

		public static string Md5Encode (string text, Encoding encoding)
		{
			if (string.IsNullOrEmpty (text)) {
				return string.Empty;
			}

			byte[] hash;
			lock (md5) {
				hash = md5.ComputeHash (encoding.GetBytes (text));
			}

			return ToHex (hash);
		}

		public static string Md5EncodeStream (Stream stream)
		{
			byte[] hash;
			lock (md5) {
				hash = md5.ComputeHash (stream);
			}

			return ToHex (hash);
		}

		static string ToHex (byte[] hash)
		{
			var shash = new StringBuilder ();
			for (int i = 0; i < hash.Length; i++) {
				shash.Append (hash[i].ToString ("x2"));
			}

			return shash.ToString ();
		}
	}
}
