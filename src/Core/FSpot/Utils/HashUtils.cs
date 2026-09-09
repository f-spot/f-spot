//
// HashUtils.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2019 Stephen Shaw
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Security.Cryptography;
using System.Text;

using Hyena;

namespace FSpot.Utils
{
	public static class HashUtils
	{
		public static string GenerateMD5 (SafeUri uri)
		{
			using (var stream = new FileStream (uri.AbsolutePath, FileMode.Open, FileAccess.Read))
				return Md5EncodeStream (stream);
		}

		public static string Md5Encode (string text)
		{
			if (string.IsNullOrEmpty (text))
				return string.Empty;

			using (var md5 = MD5.Create ())
				return ToHex (md5.ComputeHash (Encoding.ASCII.GetBytes (text)));
		}

		public static string Md5EncodeStream (Stream stream)
		{
			using (var md5 = MD5.Create ())
				return ToHex (md5.ComputeHash (stream));
		}

		static string ToHex (byte[] hash)
		{
			var hex = new StringBuilder (hash.Length * 2);
			for (int i = 0; i < hash.Length; i++)
				hex.Append (hash[i].ToString ("x2"));
			return hex.ToString ();
		}
	}
}
