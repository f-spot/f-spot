//
// OriginalGallery.cs
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

using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;

using FSpot.Core;

namespace FSpot.Exporters.Folder
{
	class OriginalGallery : FolderGallery
	{
		public OriginalGallery (IBrowsableCollection selection, string path, string name) : base (selection, path, name)
		{
			requests = new ScaleRequest [] { new ScaleRequest ("hq", 0, 0, false),
							 new ScaleRequest ("mq", 800, 600, true),
							 new ScaleRequest ("lq", 640, 480, false, true),
							 new ScaleRequest ("thumbs", 120, 120, false) };
		}

		public override void GenerateLayout ()
		{
			base.GenerateLayout ();
			MakeDir (SubdirPath ("comments"));
			CreateHtaccess();
			CreateInfo();
			SetTime ();
		}

		protected override string ImageName (int photo_index)
		{
			return string.Format ("img-{0}.jpg", photo_index + 1);
		}

		private void SetTime ()
		{
			try {
				for (int i = 0; i < Collection.Count; i++)
					CreateComments (Collection [i].DefaultVersion.Uri.LocalPath, i);

				Directory.SetLastWriteTimeUtc(GalleryPath, Collection [0].Time);
			} catch (System.Exception e) {
				Log.Error (e.ToString ());
			}
		}

		internal void CreateZip ()
		{
			MakeDir (SubdirPath ("zip"));
			try {
				if (System.IO.Directory.Exists (SubdirPath ("mq")))
				    CreateZipFile("mq");

				if (System.IO.Directory.Exists (SubdirPath ("hq")))
				    CreateZipFile("hq");

			} catch (System.Exception e) {
				Log.Error (e.ToString ());
			}
		}

		private void CreateComments(string photo_path, int photo_index)
		{
			StreamWriter comment = File.CreateText(SubdirPath  ("comments", photo_index + 1 + ".txt"));
			comment.Write("<span>photo " + (photo_index + 1) + "</span> ");
			comment.Write (Collection [photo_index].Description + Environment.NewLine);
			comment.Close();
		}

		private void CreateZipFile(string img_quality)
		{
			string[] filenames = Directory.GetFiles(SubdirPath (img_quality));
			Crc32 crc = new Crc32();
			ZipOutputStream s = new ZipOutputStream(File.Create(SubdirPath ("zip", img_quality + ".zip")));

			s.SetLevel(0);
			foreach (string file in filenames) {
				FileStream fs = File.OpenRead(file);

				byte[] buffer = new byte[fs.Length];
				fs.Read(buffer, 0, buffer.Length);
				ZipEntry entry = new ZipEntry(Path.GetFileName(file));

				entry.DateTime = DateTime.Now;

				// set Size and the crc, because the information
				// about the size and crc should be stored in the header
				// if it is not set it is automatically written in the footer.
				// (in this case size == crc == -1 in the header)
				// Some ZIP programs have problems with zip files that don't store
				// the size and crc in the header.
				entry.Size = fs.Length;
				fs.Close();

				crc.Reset();
				crc.Update(buffer);

				entry.Crc  = crc.Value;

				s.PutNextEntry(entry);

				s.Write(buffer, 0, buffer.Length);

			}

			s.Finish();
			s.Close();
		}

		private void CreateHtaccess()
		{
			StreamWriter htaccess = File.CreateText(Path.Combine (GalleryPath,".htaccess"));
			htaccess.Write("<Files info.txt>" + Environment.NewLine + "\tdeny from all" + Environment.NewLine+ "</Files>" + Environment.NewLine);
			htaccess.Close();
		}

		private void CreateInfo()
		{
			StreamWriter info = File.CreateText(Path.Combine (GalleryPath, "info.txt"));
			info.WriteLine("name|" + GalleryName);
			info.WriteLine("date|" + Collection [0].Time.Date.ToString ("dd.MM.yyyy"));
			info.WriteLine("description|" + Description);
			info.Close();
		}
	}
}
