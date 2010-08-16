/*
 * FSpot.SelectionDataExtensions.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Text;
using System.Linq;

using Gtk;
using Gdk;

using FSpot;
using FSpot.Core;
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
					data[i] = b;
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

			for (int i = 0; i < length; i ++) {
				uint id = System.BitConverter.ToUInt32 (selection_data.Data, i * size);
				photos[i] = photo_store.Get (id);
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
					data[i] = b;
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

			for (int i = 0; i < length; i ++) {
				uint id = System.BitConverter.ToUInt32 (selection_data.Data, i * size);
				tags[i] = tag_store.Get (id);
			}

			return tags;
		}

		public static string GetStringData (this SelectionData selection_data)
		{
			if (selection_data.Length <= 0)
				return String.Empty;

			try {
				return Encoding.UTF8.GetString (selection_data.Data);
			} catch (Exception) {
				return String.Empty;
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
            var data = Encoding.UTF8.GetBytes ("copy\n" + String.Join ("\n", uris));

            selection_data.Set (Atom.Intern ("x-special/gnome-copied-files", true), 8, data, data.Length);
        }
	}
}
