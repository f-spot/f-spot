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
		
		/* FIXME: we use a list of photo ids. Maybe this can be encoded more efficiently */
		public static void SetPhotosData (Photo [] photos, SelectionData selection_data)
		{
			StringBuilder builder = new StringBuilder ();
			foreach (Photo photo in photos) {

				if (builder.Length == 0)
					builder.Append (photo.Id);
				else
					builder.AppendFormat (" {0}", photo.Id);
			}
			
			Byte [] data = Encoding.UTF8.GetBytes (builder.ToString ());
			Gdk.Atom [] targets = args.Context.Targets;
			
			selection_data.Set (targets[0], 8, data, data.Length);
		}
		
		public static Photo [] GetPhotosData (SelectionData data)
		{
			string [] values = GetStringData (data).Split (' ');
			
			Photo [] photos = new Photo [values.Length];
			
			for (int i = 0; i < values.Length; i++) {
				uint id = Convert.ToUInt32 (values[i]);
				
				photos[i] = MainWindow.Toplevel.Database.Photos.Get (id);
			}

			return photos;
		}
		
		public static void SetTagsData (Tag [] tags, SelectionData selection_data)
		{
			StringBuilder builder = new StringBuilder ();
			foreach (Tag tag in tags) {

				if (builder.Length == 0)
					builder.Append (tag.Id);
				else
					builder.AppendFormat (" {0}", tag.Id);
			}
			
			Byte [] data = Encoding.UTF8.GetBytes (builder.ToString ());
			Gdk.Atom [] targets = args.Context.Targets;
			
			selection_data.Set (targets[0], 8, data, data.Length);
		}
		
		public static Tag [] GetTagsData (SelectionData data)
		{
			string [] values = GetStringData (data).Split (' ');
			
			Tag [] tags = new Tag [values.Length];
			
			for (int i = 0; i < values.Length; i++) {
				uint id = Convert.ToUInt32 (values[i]);
				
				tags[i] = MainWindow.Toplevel.Database.Tags.Get (id);
			}

			return tags;
		}
		
		public static string GetStringData (SelectionData data)
		{
			if (data.Length <= 0)
				return String.Empty;
			
			try {
				return Encoding.UTF8.GetString (data.Data);
			} catch (Exception) {
				return String.Empty;
			}
		}
		
		public static void SetUriListData (UriList uri_list, DragDataGetArgs args)
		{
			Byte [] data = Encoding.UTF8.GetBytes (uri_list.ToString ());
			Gdk.Atom [] targets = args.Context.Targets;
			
			args.SelectionData.Set (targets[0], 8, data, data.Length);
		}
		
		public static UriList GetUriListData (SelectionData data)
		{
			string [] uris = GetStringData (data).Split ('\n');
			
			return new UriList (uris);
		}
	}
}
