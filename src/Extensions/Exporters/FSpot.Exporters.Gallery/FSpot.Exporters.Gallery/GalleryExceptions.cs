// GalleryExceptions.cs
// 
// Author:
//      Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (c) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Exporters.Gallery
{
	public class GalleryException : System.Exception
	{
		public string ResponseText { get; private set; }

		public GalleryException (string text) : base (text) { }

		public GalleryException (string text, string full_response) : base (text)
		{
			ResponseText = full_response;
		}
	}

	public class GalleryCommandException : GalleryException
	{
		public ResultCode Status { get; private set; }

		public GalleryCommandException (string status_text, ResultCode result) : base (status_text)
		{
			Status = result;
		}
	}
}
