//
// Mono.Google.Picasa.UploadPictureException.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
namespace Mono.Google.Picasa {
	public class UploadPictureException : Exception {
		public UploadPictureException (string msg) : base (msg)
		{
		}

		protected UploadPictureException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
