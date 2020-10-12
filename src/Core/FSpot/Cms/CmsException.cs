//
// CmsException.cs
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
	public class CmsException : Exception 
	{
		public CmsException (string message) : base (message)
		{
		}

		protected CmsException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}

		public CmsException (string message, Exception innerException) : base (message, innerException)
		{
		}

		public CmsException ()
		{
		}
	}
}
