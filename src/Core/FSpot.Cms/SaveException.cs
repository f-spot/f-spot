//
// SaveException.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace FSpot.Cms
{
	[Serializable]
	public class SaveException : CmsException
	{
		public SaveException (string message) : base (message)
		{
		}

		protected SaveException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}

		public SaveException (string message, Exception innerException) : base (message, innerException)
		{
		}

		public SaveException ()
		{
		}
	}
}
