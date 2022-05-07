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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FSpot.Database;
using FSpot.Models;
using FSpot.Utils;

using Gdk;

using Gtk;

using TagLib.Riff;

namespace FSpot
{
	public static class SelectionDataExtensions
	{
		// FIXME, verify these methods
		static int GuidLength = new Guid ().ToByteArray ().Length;

		public static void SetPhotosData (this SelectionData selectionData, Photo[] photos, Atom target)
		{
			var data = new List<byte> ();

			foreach (var photo in photos)
				data.AddRange (photo.Id.ToByteArray ());

			selectionData.Set (target, 8, data.ToArray (), data.Count);
		}

		public static Photo[] GetPhotosData (this SelectionData selectionData)
		{
			var span = new Span<byte> (selectionData.Data);

			int size = GuidLength;
			int length = selectionData.Length / size;

			var photoStore = new PhotoStore ();

			var photos = new Photo[length];

			for (int i = 0; i < length; i++) {
				var id = new Guid (span.Slice (i * size, size).ToArray ());
				photos[i] = photoStore.Get (id);
			}

			return photos;
		}

		public static void SetTagsData (this SelectionData selectionData, IEnumerable<Tag> tags, Atom target)
		{
			var data = new List<byte> ();// new byte[tags.Length * sizeof (uint)];

			//int i = 0;
			foreach (var tag in tags) {
				data.AddRange (tag.Id.ToByteArray ());
				//byte[] bytes = System.BitConverter.GetBytes (tag.Id);

				//foreach (byte b in bytes) {
				//	data[i] = b;
				//	i++;
				//}
			}

			selectionData.Set (target, 8, data.ToArray (), data.Count);
		}

		public static List<Tag> GetTagsData (this SelectionData selectionData)
		{
			//int size = sizeof (uint);
			//int length = selectionData.Length / size;

			//TagStore tag_store = App.Instance.Database.Tags;

			//Tag[] tags = new Tag[length];

			//for (int i = 0; i < length; i++) {
			//	uint id = System.BitConverter.ToUInt32 (selectionData.Data, i * size);
			//	tags[i] = tag_store.Get (id);
			//}

			var span = new Span<byte> (selectionData.Data);

			int size = GuidLength;
			int length = selectionData.Length / size;

			var tagStore = new TagStore ();

			var tags = new List<Tag> (length);

			for (int i = 0; i < length; i++) {
				var id = new Guid (span.Slice (i * size, size).ToArray ());
				tags[i] = tagStore.Get (id);
			}

			return tags;
		}

		public static string GetStringData (this SelectionData selectionData)
		{
			if (selectionData.Length <= 0)
				return string.Empty;

			try {
				return Encoding.UTF8.GetString (selectionData.Data);
			} catch (Exception) {
				return string.Empty;
			}
		}

		public static void SetUriListData (this SelectionData selectionData, UriList uriList, Atom target)
		{
			var data = Encoding.UTF8.GetBytes (uriList.ToString ());

			selectionData.Set (target, 8, data, data.Length);
		}

		public static void SetUriListData (this SelectionData selectionData, UriList uriList)
		{
			selectionData.SetUriListData (uriList, Atom.Intern ("text/uri-list", true));
		}

		public static UriList GetUriListData (this SelectionData selectionData)
		{
			return new UriList (GetStringData (selectionData));
		}

		public static void SetCopyFiles (this SelectionData selectionData, UriList uriList)
		{
			var uris = (from p in uriList select p.ToString ()).ToArray ();
			var data = Encoding.UTF8.GetBytes ("copy\n" + string.Join ("\n", uris));

			selectionData.Set (Atom.Intern ("x-special/gnome-copied-files", true), 8, data, data.Length);
		}
	}
}
