//
// FolderGallery.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
// Copyright (C) 2008-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

/*
 * Copyright (C) 2005 Alessandro Gervaso <gervystar@gervystar.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301
 */

//This should be used to export the selected pics to an original gallery
//located on a GIO location.

using System;
using System.IO;

using Hyena;

using FSpot;
using FSpot.Core;
using FSpot.Database;
using FSpot.Filters;
using FSpot.Settings;
using FSpot.Utils;

namespace FSpot.Exporters.Folder
{
	internal class FolderGallery
	{
		protected struct ScaleRequest
		{
			public string Name;
			public int Width;
			public int Height;
			public bool Skip;
			public bool CopyExif;
			public static ScaleRequest Default = new ScaleRequest (string.Empty, 0, 0, false);

			public ScaleRequest (string name, int width, int height, bool skip, bool exif = false)
			{
				this.Name = name != null ? name : string.Empty;
				this.Width = width;
				this.Height = height;
				this.Skip = skip;
				this.CopyExif = exif;
			}

			public bool AvoidScale (int size)
			{
				return (size < this.Width && size < this.Height && this.Skip);
			}
		}

		#region Variables
		protected bool scale;
		protected ScaleRequest[] requests;
		#endregion

		#region Constructor
		internal FolderGallery (IBrowsableCollection selection, string path, string gallery_name)
		{
			if (null == selection)
				throw new ArgumentNullException ("selection");

			if (0 == selection.Count)
				throw new ArgumentException ("selection can't be empty");

			if (null == path)
				throw new ArgumentNullException ("path");

			if (null == gallery_name)
				throw new ArgumentNullException ("gallery_name");

			Collection = selection;
			GalleryName = gallery_name;
			GalleryPath = Path.Combine (path, GalleryName);
			this.requests = new ScaleRequest [] { ScaleRequest.Default };
		}
		#endregion

		#region Properties
		public string GalleryName { get; protected set; }
		public string GalleryPath { get; protected set; }
		protected IBrowsableCollection Collection { get; set; }
		public string Description { get; set; }
		public Uri Destination { get; set; }
		protected int Size { get; set; }
		public bool ExportTags { get; set; }
		public bool ExportTagIcons { get; set; }

		string language = string.Empty;
		public string Language {
			get {
				if (language == null) {
					if ((language = Environment.GetEnvironmentVariable ("LC_ALL")) == null)
						if ((language = Environment.GetEnvironmentVariable ("LC_MESSAGES")) == null)
							if ((language = Environment.GetEnvironmentVariable ("LANG")) == null)
								language = "en";

					if (language.IndexOf ('.') >= 0)
						language = language.Substring (0, language.IndexOf ('.'));
					if (language.IndexOf ('@') >= 0)
						language = language.Substring (0, language.IndexOf ('@'));
					language = language.Replace ('_', '-');

				}
				return language;
			}
		}
		#endregion

		#region method
		public virtual void GenerateLayout ()
		{
			MakeDir (GalleryPath);

		}

		protected virtual string ImageName (int image_num)
		{
			var uri = Collection [image_num].DefaultVersion.Uri;
			var dest_uri = new SafeUri (GalleryPath);
	
			// Find an unused name
			int i = 1;
			var dest = dest_uri.Append (uri.GetFilename ());
			var file = GLib.FileFactory.NewForUri (dest);
			while (file.Exists) {
				var filename = uri.GetFilenameWithoutExtension ();
				var extension = uri.GetExtension ();
				dest = dest_uri.Append (string.Format ("{0}-{1}{2}", filename, i++, extension));
				file = GLib.FileFactory.NewForUri (dest);
			}
	
			return dest.GetFilename ();
		}

		public void ProcessImage (int image_num, FilterSet filter_set)
		{
			IPhoto photo = Collection [image_num];
			string path;
			ScaleRequest req;

			req = requests [0];

			MakeDir (SubdirPath (req.Name));
			path = SubdirPath (req.Name, ImageName (image_num));

			using (FilterRequest request = new FilterRequest (photo.DefaultVersion.Uri)) {
				filter_set.Convert (request);
				if (request.Current.LocalPath == path)
					request.Preserve (request.Current);
				else
					System.IO.File.Copy (request.Current.LocalPath, path, true);

				if (photo != null && photo is Photo && App.Instance.Database != null)
					App.Instance.Database.Exports.Create ((photo as Photo).Id, (photo as Photo).DefaultVersionId,
								      ExportStore.FolderExportType,
								      // FIXME this is wrong, the final path is the one
								      // after the Xfer.
								      new SafeUri (path).ToString ());

				for (int i = 1; i < requests.Length; i++) {

					req = requests [i];
					if (scale && req.AvoidScale (Size))
						continue;

					FilterSet req_set = new FilterSet ();
					req_set.Add (new ResizeFilter ((uint)Math.Max (req.Width, req.Height)));

					bool sharpen;
					try {
						sharpen = Preferences.Get<bool> (FolderExport.SHARPEN_KEY);
					} catch (NullReferenceException) {
						sharpen = true;
						Preferences.Set (FolderExport.SHARPEN_KEY, true);
					}

					if (sharpen) {
						if (req.Name == "lq")
							req_set.Add (new SharpFilter (0.1, 2, 4));
						if (req.Name == "thumbs")
							req_set.Add (new SharpFilter (0.1, 2, 5));
					}
					using (FilterRequest tmp_req = new FilterRequest (photo.DefaultVersion.Uri)) {
						req_set.Convert (tmp_req);
						MakeDir (SubdirPath (req.Name));
						path = SubdirPath (req.Name, ImageName (image_num));
						System.IO.File.Copy (tmp_req.Current.LocalPath, path, true);
					}
				}
			}
		}

		protected string MakeDir (string path)
		{
			try {
				Directory.CreateDirectory (path);
			} catch {
				Log.ErrorFormat ("Error in creating directory \"{0}\"", path);
			}
			return path;
		}

		protected string SubdirPath (string subdir, string file = null)
		{
			string path = Path.Combine (GalleryPath, subdir);
			if (file != null)
				path = Path.Combine (path, file);

			return path;
		}

		public void SetScale (int size)
		{
			this.scale = true;
			Size = size;
			requests [0].Width = size;
			requests [0].Height = size;
		}
		#endregion
	}
}
