//
// SelectionDataExtensions.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//   Mike Gemuende <mike@gemuende.de>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
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
using System.Text;
using System.Linq;

using Gtk;
using Gdk;

using FSpot.Core;
using FSpot.Database;
using FSpot.Utils;

namespace FSpot
{
	public static class SelectionDataExtensions
	{
		public static void SetPhotosData (this SelectionData selection_data, Photo [] photos, Atom target)
		{
			byte [] data = new byte [photos.Length * sizeof (uint)];

			int i = 0;
			foreach (Photo photo in photos) {
				byte [] bytes = System.BitConverter.GetBytes (photo.Id);

				foreach (byte b in bytes) {
					data [i] = b;
					i++;
				}
			}

			selection_data.Set (target, 8, data, data.Length);
		}

		public static Photo [] GetPhotosData (this SelectionData selection_data)
		{
			int size = sizeof (uint);
			int length = selection_data.Length / size;

			PhotoStore photo_store = App.Instance.Database.Photos;

			Photo [] photos = new Photo [length];

			for (int i = 0; i < length; i++) {
				uint id = System.BitConverter.ToUInt32 (selection_data.Data, i * size);
				photos [i] = photo_store.Get (id);
			}

			return photos;
		}

		public static void SetTagsData (this SelectionData selection_data, Tag [] tags, Atom target)
		{
			byte [] data = new byte [tags.Length * sizeof (uint)];

			int i = 0;
			foreach (Tag tag in tags) {
				byte [] bytes = System.BitConverter.GetBytes (tag.Id);

				foreach (byte b in bytes) {
					data [i] = b;
					i++;
				}
			}

			selection_data.Set (target, 8, data, data.Length);
		}

		public static Tag [] GetTagsData (this SelectionData selection_data)
		{
			int size = sizeof (uint);
			int length = selection_data.Length / size;

			TagStore tag_store = App.Instance.Database.Tags;

			Tag [] tags = new Tag [length];

			for (int i = 0; i < length; i++) {
				uint id = System.BitConverter.ToUInt32 (selection_data.Data, i * size);
				tags [i] = tag_store.Get (id);
			}

			return tags;
		}

		public static string GetStringData (this SelectionData selection_data)
		{
			if (selection_data.Length <= 0)
				return string.Empty;

			try {
				return Encoding.UTF8.GetString (selection_data.Data);
			} catch (Exception) {
				return string.Empty;
			}
		}

		public static void SetUriListData (this SelectionData selection_data, UriList uri_list, Atom target)
		{
			Byte [] data = Encoding.UTF8.GetBytes (uri_list.ToString ());

			selection_data.Set (target, 8, data, data.Length);
		}

		public static void SetUriListData (this SelectionData selection_data, UriList uri_list)
		{
			selection_data.SetUriListData (uri_list, Atom.Intern ("text/uri-list", true));
		}

		public static UriList GetUriListData (this SelectionData selection_data)
		{
			return new UriList (GetStringData (selection_data));
		}

		public static void SetCopyFiles (this SelectionData selection_data, UriList uri_list)
		{
			var uris = (from p in uri_list select p.ToString ()).ToArray ();
			var data = Encoding.UTF8.GetBytes ("copy\n" + string.Join ("\n", uris));

			selection_data.Set (Atom.Intern ("x-special/gnome-copied-files", true), 8, data, data.Length);
		}
	}
}
