//
// NoSuchKeyException.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace FSpot.Platform
{
	[Serializable]
	public class NoSuchKeyException : Exception
	{
		public NoSuchKeyException ()
		{
		}

		public NoSuchKeyException (string key) : base (key)
		{
		}

		public NoSuchKeyException (string key, Exception e) : base (key, e)
		{
		}

		protected NoSuchKeyException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}
	}
}
