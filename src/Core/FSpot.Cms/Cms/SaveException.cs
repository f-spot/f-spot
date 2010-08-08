/*
 * Cms.SaveException.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Runtime.Serialization;

namespace Cms {
	[Serializable]
	public class SaveException : CmsException {
		public SaveException (string message) : base (message)
		{
		}

		protected SaveException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}

		public SaveException (string message, Exception innerException) : base (message, innerException)
		{
		}

		public SaveException () : base ()
		{
		}
	}
}
