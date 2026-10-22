//
// DCRawImageFile.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2005-2006 Larry Ewing
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Platform;

using Hyena;

namespace FSpot.Imaging
{
	class DCRawImageFile : BaseImageFile
	{
		const string dcraw_command = "dcraw";

		public DCRawImageFile (SafeUri uri) : base (uri)
		{
		}

		public override System.IO.Stream OpenImageStream ()
		{
			return OpenRawImageStream (Uri);
		}

		internal static System.IO.Stream OpenRawImageStream (SafeUri location)
		{
			string path = location.LocalPath;
			string[] args = { "-h", "-w", "-c", "-t", "0", path };

			return ProcessStream.Start (System.IO.Path.GetDirectoryName (path), ExternalTool.Resolve (dcraw_command), args);
		}
	}
}
