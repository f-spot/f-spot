//
// Mono.Google.Picasa.CreateAlbumException.cs:
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
	public class CreateAlbumException : Exception {
		public CreateAlbumException (string msg) : base (msg)
		{
		}

		protected CreateAlbumException (SerializationInfo info, StreamingContext context)
			: base (info, context)
		{
		}
	}
}
