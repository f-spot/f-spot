/*
 * DragDrop.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */


using System;
using System.Text;

using Gtk;

using Gdk;

using FSpot;
using FSpot.Utils;



namespace FSpot.GuiUtils
{	
	public static class DragDrop
	{
		public enum TargetType {
			UriList,
			TagList,
			TagQueryItem,
			UriQueryItem,
			PhotoList,
			RootWindow
		};

		public static readonly TargetEntry PhotoListEntry =
			new TargetEntry ("application/x-fspot-photos", 0, (uint) TargetType.PhotoList);
		
		public static readonly TargetEntry UriListEntry =
			new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList);
		
		public static readonly TargetEntry TagListEntry =
			new TargetEntry ("application/x-fspot-tags", 0, (uint) TargetType.TagList);
		
		/* FIXME: maybe we need just one fspot-query-item */
		public static readonly TargetEntry UriQueryEntry =
			new TargetEntry ("application/x-fspot-uri-query-item", 0, (uint) TargetType.UriQueryItem);
		
		public static readonly TargetEntry TagQueryEntry =
			new TargetEntry ("application/x-fspot-tag-query-item", 0, (uint) TargetType.TagQueryItem);
		
		public static readonly TargetEntry RootWindowEntry =
			new TargetEntry ("application/x-root-window-drop", 0, (uint) TargetType.RootWindow);
		

		public static void SetPhotosData (Photo [] photos, SelectionData selection_data, Gdk.Atom target)
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
		
		public static Photo [] GetPhotosData (SelectionData selection_data)
		{
			int size = sizeof (uint);
			int length = selection_data.Length / size;
			
			PhotoStore photo_store = MainWindow.Toplevel.Database.Photos;

			Photo [] photos = new Photo [length];
			
			for (int i = 0; i < length; i ++) {
				uint id = System.BitConverter.ToUInt32 (selection_data.Data, i * size);
				photos[i] = photo_store.Get (id);
			}

			return photos;
		}
		
		public static void SetTagsData (Tag [] tags, SelectionData selection_data, Gdk.Atom target)
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
		
		public static Tag [] GetTagsData (SelectionData selection_data)
		{
			int size = sizeof (uint);
			int length = selection_data.Length / size;
			
			TagStore tag_store = MainWindow.Toplevel.Database.Tags;

			Tag [] tags = new Tag [length];
			
			for (int i = 0; i < length; i ++) {
				uint id = System.BitConverter.ToUInt32 (selection_data.Data, i * size);
				tags[i] = tag_store.Get (id);
			}

			return tags;
		}
		
		public static string GetStringData (SelectionData selection_data)
		{
			if (selection_data.Length <= 0)
				return String.Empty;
			
			try {
				return Encoding.UTF8.GetString (selection_data.Data);
			} catch (Exception) {
				return String.Empty;
			}
		}
		
		public static void SetUriListData (UriList uri_list, SelectionData selection_data, Gdk.Atom target)
		{
			Byte [] data = Encoding.UTF8.GetBytes (uri_list.ToString ());
			
			selection_data.Set (target, 8, data, data.Length);
		}
		
		public static UriList GetUriListData (SelectionData selection_data)
		{
			string [] uris = GetStringData (selection_data).Split ('\n');
			
			return new UriList (uris);
		}
	}
}
