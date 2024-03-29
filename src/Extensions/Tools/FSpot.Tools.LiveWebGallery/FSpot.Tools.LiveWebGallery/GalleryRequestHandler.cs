//
// GalleryRequestHandler.cs
//
// Author:
//   Anton Keks <anton@azib.net>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Anton Keks
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using FSpot.Core;
using FSpot.Resources.Lang;

namespace FSpot.Tools.LiveWebGallery
{
	public abstract class PhotoAwareRequestHandler : RequestHandler
	{
		protected string TagsToString (Photo photo)
		{
			string tags = "";
			foreach (Tag tag in photo.Tags) {
				tags += ", " + tag.Name;
			}
			return tags.Length > 1 ? tags.Substring (2) : tags;
		}

	}

	public abstract class TemplateRequestHandler : PhotoAwareRequestHandler
	{
		protected string template;

		public TemplateRequestHandler (string name)
		{
			template = LoadTemplate (name);
		}

		protected string GetSubTemplate (StringBuilder s, string begin, string end)
		{
			int start_pos = template.IndexOf (begin);
			string sub = template.Substring (start_pos, template.IndexOf (end, start_pos) - start_pos - 1);
			s.Replace (sub, "");
			return sub.Substring (begin.Length, sub.Length - begin.Length);
		}

		protected string LoadTemplate (string name)
		{
			using (TextReader s = new StreamReader (Assembly.GetCallingAssembly ().GetManifestResourceStream (name))) {
				return s.ReadToEnd ();
			}
		}

		protected string Escape (string s)
		{
			// javascript-proof
			return s.Replace ("\"", "\\\"");
		}
	}

	public class GalleryRequestHandler : TemplateRequestHandler, ILiveWebGalleryOptions
	{
		QueryType query_type = QueryType.ByTag;
		public QueryType QueryType {
			get { return query_type; }
			set { query_type = value; }
		}

		Tag query_tag;
		public Tag QueryTag {
			get { return query_tag; }
			set { query_tag = value; }
		}

		bool limit_max_photos = true;
		public bool LimitMaxPhotos {
			get { return limit_max_photos; }
			set { limit_max_photos = value; }
		}

		int max_photos = 1000;
		public int MaxPhotos {
			get { return max_photos; }
			set { max_photos = value; }
		}

		bool tagging_allowed = false;
		public bool TaggingAllowed {
			get { return tagging_allowed; }
			set { tagging_allowed = value; }
		}

		Tag editable_tag;
		public Tag EditableTag {
			get { return editable_tag; }
			set { editable_tag = value; }
		}

		LiveWebGalleryStats stats;

		public GalleryRequestHandler (LiveWebGalleryStats stats)
			: base ("gallery.html")
		{
			this.stats = stats;
			template = template.Replace ("TITLE", Strings.FSpotGallery);
			template = template.Replace ("OFFLINE_MESSAGE", Strings.TheWebGallerySeemsToBeOfflineNow);
			template = template.Replace ("SHOW_ALL", Strings.ShowAll);
		}

		public override void Handle (string requested, Stream stream)
		{
			Photo[] photos = GetChosenPhotos ();

			var s = new StringBuilder (4096);
			s.Append (template);
			int num_photos = limit_max_photos ? Math.Min (photos.Length, max_photos) : photos.Length;
			var num_photos_string = num_photos <= 1 ? Strings.XPhoto : Strings.XPhotos;
			s.Replace ("NUM_PHOTOS", string.Format (num_photos_string, num_photos));
			s.Replace ("QUERY_TYPE", QueryTypeToString ());
			s.Replace ("EDITABLE_TAG_NAME", tagging_allowed ? Escape (editable_tag.Name) : "");

			string photo_template = GetSubTemplate (s, "BEGIN_PHOTO", "END_PHOTO");
			var photos_s = new StringBuilder (4096);

			num_photos = 0;
			foreach (Photo photo in photos) {
				photos_s.Append (PreparePhoto (photo_template, photo));

				if (++num_photos >= max_photos && limit_max_photos)
					break;
			}
			s.Replace ("END_PHOTO", photos_s.ToString ());

			SendHeadersAndStartContent (stream, "Content-Type: text/html; charset=UTF-8");
			SendLine (stream, s.ToString ());

			stats.BytesSent += s.Length;
			stats.GalleryViews++;
		}

		Photo[] GetChosenPhotos ()
		{
			switch (query_type) {
			case QueryType.ByTag:
				return ObsoletePhotoQueries.Query (new List<Tag> { query_tag });
			case QueryType.CurrentView:
				return App.Instance.Organizer.Query.Photos;
			case QueryType.Selected:
			default:
				return App.Instance.Organizer.SelectedPhotos ();
			}
		}

		string QueryTypeToString ()
		{
			switch (query_type) {
			case QueryType.ByTag:
				return query_tag.Name;
			case QueryType.CurrentView:
				return Strings.CurrentView;
			case QueryType.Selected:
			default:
				return Strings.Selected;
			}
		}

		string PreparePhoto (string template, Photo photo)
		{
			string photo_s = template.Replace ("PHOTO_ID", photo.Id.ToString ())
									 .Replace ("PHOTO_NAME", Escape (photo.Name))
									 .Replace ("PHOTO_DESCRIPTION", Escape (photo.Description))
									 .Replace ("VERSION_NAME", Escape (photo.DefaultVersion.Name));
			string tags = TagsToString (photo);
			photo_s = photo_s.Replace ("PHOTO_TAGS", Escape (tags));

			return photo_s;
		}
	}

	public class PingRequestHandler : RequestHandler
	{
		public override void Handle (string requested, Stream stream)
		{
			SendHeadersAndStartContent (stream);
		}
	}

	public class TagAddRemoveRequestHandler : PhotoAwareRequestHandler
	{
		ILiveWebGalleryOptions options;

		public TagAddRemoveRequestHandler (ILiveWebGalleryOptions options)
		{
			this.options = options;
		}

		public override void Handle (string requested, Stream stream)
		{
			bool addTag = requested.StartsWith ("add");
			if (!addTag && !requested.StartsWith ("remove")) {
				SendError (stream, "400 Bad request " + requested);
				return;
			}
			int slash_pos = requested.IndexOf ('/');
			requested = requested.Substring (slash_pos + 1);
			slash_pos = requested.IndexOf ('/');
			uint photo_id = uint.Parse (requested.Substring (0, slash_pos));
			string tag_name = requested.Substring (slash_pos + 1);

			if (!options.TaggingAllowed || !options.EditableTag.Name.Equals (tag_name)) {
				SendError (stream, "403 Forbidden to change tag " + tag_name);
				return;
			}

			Photo photo = App.Instance.Database.Photos.Get (photo_id);
			if (addTag)
				photo.AddTag (options.EditableTag);
			else
				photo.RemoveTag (options.EditableTag);
			App.Instance.Database.Photos.Commit (photo);

			SendHeadersAndStartContent (stream, "Content-type: text/plain;charset=UTF-8");
			SendLine (stream, TagsToString (photo));
		}
	}
}
