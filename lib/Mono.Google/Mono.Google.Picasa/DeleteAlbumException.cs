//
// Mono.Google.Picasa.DeleteAlbumException.cs:
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
	public class DeleteAlbumException : Exception {
		public DeleteAlbumException (string msg) : base (msg)
		{
		}

		protected DeleteAlbumException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
