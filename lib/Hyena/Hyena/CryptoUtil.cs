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

namespace Hyena
{
	public static class CryptoUtil
	{
		static MD5 md5 = MD5.Create ();

		public static string Md5Encode (string text)
		{
			if (string.IsNullOrEmpty (text)) {
				return string.Empty;
			}

			byte[] hash;
			lock (md5) {
				hash = md5.ComputeHash (Encoding.ASCII.GetBytes (text));
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
