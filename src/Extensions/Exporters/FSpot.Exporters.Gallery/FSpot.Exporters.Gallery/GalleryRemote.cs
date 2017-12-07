//
// GalleryRemote.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <sdelcroix*novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2004-2008 Novell, Inc.
// Copyright (C) 2004-2007 Larry Ewing
// Copyright (C) 2006-2008 Stephane Delcroix
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

using System;
using System.IO;
using System.Collections.Generic;

using FSpot.Core;

using Hyena;

/* These classes are based off the documentation at
 *
 * http://codex.gallery2.org/index.php/Gallery_Remote:Protocol
 */

namespace FSpot.Exporters.Gallery
{
	public enum AlbumPermission : byte
	{
		None = 0,
		Add = 1,
		Write = 2,
		Delete = 4,
		DeleteAlbum = 8,
		CreateSubAlbum = 16
	}

	public class Album : IComparable
	{
		public int RefNum;
		public string Name = string.Empty;
		public string Title = string.Empty;
		public string Summary = string.Empty;
		public int ParentRefNum;
		public int ResizeSize;
		public int ThumbSize;
		public List<Image> Images;
		public string BaseURL = string.Empty;
		public AlbumPermission Perms = AlbumPermission.None;

		public Album Parent {
			get {
				if (ParentRefNum != 0)
					return Gallery.LookupAlbum (ParentRefNum);
				else
					return null;
			}
		}

		protected List<int> parents = null;
		public List<int> Parents {
			get {
				if (parents != null)
					return parents;

				if (Parent == null) {
				       parents = new List<int> ();
				} else {
					parents = Parent.Parents;
					parents.Add (Parent.RefNum);
				}
				return parents;
			}
		}

		public Gallery Gallery { get; private set; }

		public Album (Gallery gallery, string name, int ref_num)
		{
			Name = name;
			Gallery = gallery;
			this.RefNum = ref_num;
			Images = new List<Image> ();
		}

		public void Rename (string name)
		{
			Gallery.MoveAlbum (this, name);
		}

		public void Add (IPhoto item)
		{
			Add (item, item.DefaultVersion.Uri.LocalPath);
		}

		public int Add (IPhoto item, string path)
		{
			if (item == null)
				Log.Warning ("NO PHOTO");

			return Gallery.AddItem (this,
					 path,
					 Path.GetFileName (item.DefaultVersion.Uri.LocalPath),
					 item.Name,
					 item.Description,
					 true);
		}

		public string GetUrl ()
		{
			return Gallery.GetAlbumUrl(this);
		}

		public int CompareTo (Object obj)
		{
			Album other = obj as Album;

			int numThis = this.Parents.Count;
			int numOther = other.Parents.Count;
			int thisVal = -1, otherVal = -1;

			//find where they first differ
			int maxIters = Math.Min (numThis, numOther);
			int i = 0;
			while (i < maxIters) {
				thisVal = (int)this.Parents[i];
				otherVal = (int)other.Parents[i];
				if (thisVal != otherVal) {
					break;
				}
				i++;
			}

			int retVal;
			if (i < numThis && i < numOther) {
				//Parentage differed
				retVal = thisVal.CompareTo (otherVal);

			} else if (i < numThis) {
				//other shorter
				thisVal = (int)this.Parents[i];
				retVal = thisVal.CompareTo (other.RefNum);

				//if equal, we want to make the shorter one come first
				if (retVal == 0)
					retVal = 1;

			} else if (i < numOther) {
				//this shorter
				otherVal = (int)other.Parents[i];
				retVal = this.RefNum.CompareTo (otherVal);

				//if equal, we want to make the shorter one come first
				if (retVal == 0)
					retVal = -1;

			} else {
				//children of the same parent
				retVal = this.RefNum.CompareTo (other.RefNum);
			}
			return retVal;
		}
	}

	public class Image
	{
		public string Name;
		public int RawWidth;
		public int RawHeight;
		public string ResizedName;
		public int ResizedWidth;
		public int ResizedHeight;
		public string ThumbName;
		public int ThumbWidth;
		public int ThumbHeight;
		public int RawFilesize;
		public string Caption;
		public string Description;
		public int Clicks;
		public Album Owner;
		public string Url;

		public Image (Album album, string name) {
			Name = name;
			Owner = album;
		}
	}

	public enum ResultCode {
		Success = 0,
		MajorVersionInvalid = 101,
		MajorMinorVersionInvalid = 102,
		VersionFormatInvalid = 103,
		VersionMissing = 104,
		PasswordWrong = 201,
		LoginMissing = 202,
		UnknownComand = 301,
		NoAddPermission = 401,
		NoFilename = 402,
		UploadPhotoFailed = 403,
		NoWritePermission = 404,
		NoCreateAlbumPermission = 501,
		CreateAlbumFailed = 502,
		// This result is specific to this implementation
		UnknownResponse = 1000
	}

	public enum GalleryVersion : byte
	{
		VersionUnknown = 0,
		Version1 = 1,
		Version2 = 2
	}
}
