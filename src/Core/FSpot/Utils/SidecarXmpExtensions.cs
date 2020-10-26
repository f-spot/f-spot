//
// SidecarXmpExtensions.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

using TagLib.Xmp;

namespace FSpot.Utils
{
	public static class SidecarXmpExtensions
	{
		/// <summary>
		///    Parses the XMP file identified by resource and replaces the XMP
		///    tag of file by the parsed data.
		/// </summary>
		public static bool ParseXmpSidecar (this TagLib.Image.File file, TagLib.File.IFileAbstraction resource)
		{
			string xmp;

			try {
				using var stream = resource.ReadStream;
				using var reader = new StreamReader (stream);
				xmp = reader.ReadToEnd ();
			} catch (Exception e) {
				Log.Debug ($"Sidecar cannot be read for file {file.Name}");
				Log.DebugException (e);
				return false;
			}

			XmpTag tag;
			try {
				tag = new XmpTag (xmp, file);
			} catch (Exception e) {
				Log.Debug ($"Metadata of Sidecar cannot be parsed for file {file.Name}");
				Log.DebugException (e);
				return false;
			}

			var xmp_tag = file.GetTag (TagLib.TagTypes.XMP, true) as XmpTag;
			xmp_tag.ReplaceFrom (tag);
			return true;
		}

		public static bool SaveXmpSidecar (this TagLib.Image.File file, TagLib.File.IFileAbstraction resource)
		{
			var xmp_tag = file.GetTag (TagLib.TagTypes.XMP, false) as XmpTag;
			if (xmp_tag == null) {
				// TODO: Delete File
				return true;
			}

			var xmp = xmp_tag.Render ();

			try {
				using var stream = resource.WriteStream;
				stream.SetLength (0);
				using (var writer = new StreamWriter (stream)) {
					writer.Write (xmp);
				}
				resource.CloseStream (stream);
			} catch (Exception e) {
				Log.Debug ($"Sidecar cannot be saved: {resource.Name}");
				Log.DebugException (e);
				return false;
			}

			return true;
		}
	}
}
