//
// ChangePhotoPath.IChangePhotoPathGui.cs: Interfaces to ChangePhotoPathGui
//
// Author:
//   Bengt Thuree (bengt@thuree.com)
//
// Copyright (C) 2007
//

namespace ChangePhotoPath
{
	public interface IChangePhotoPathGui
	{
		void remove_progress_dialog();
		bool UpdateProgressBar (string hdr_txt, string text, int total_photos);
		void DisplayDefaultPaths (string oldpath, string newpath);
	}
}
