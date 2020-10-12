//
// IChangePhotoPathGui.cs
//
// Author:
//   Stephane Delcroix <sdelcroix*novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

//
// ChangePhotoPath.IChangePhotoPathGui.cs: Interfaces to ChangePhotoPathGui
//
// Author:
//   Bengt Thuree (bengt@thuree.com)
//
// Copyright (C) 2007
//

namespace FSpot.Tools.ChangePhotoPath
{
	public interface IChangePhotoPathGui
	{
		void remove_progress_dialog();
		bool UpdateProgressBar (string hdr_txt, string text, int total_photos);
		void DisplayDefaultPaths (string oldpath, string newpath);
	}
}
