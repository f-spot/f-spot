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

using Hyena;

namespace FSpot.Utils
{
	public static class HashUtils
	{
		public static string GenerateMD5 (SafeUri uri)
		{
			string hash = string.Empty;

			using (var stream = new FileStream (uri.AbsolutePath, FileMode.Open, FileAccess.Read)) {
				hash = CryptoUtil.Md5EncodeStream (stream);
			}

			return hash;
		}
	}
}
