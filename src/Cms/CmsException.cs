/*
 * Cms.CmsException.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Runtime.Serialization;

namespace Cms {
	[Serializable]
	public class CmsException : System.Exception {
		public CmsException (string message) : base (message)
		{
		}

		protected CmsException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}

		public CmsException (string message, Exception innerException) : base (message, innerException)
		{
		}

		public CmsException () : base ()
		{
		}
	}
}
