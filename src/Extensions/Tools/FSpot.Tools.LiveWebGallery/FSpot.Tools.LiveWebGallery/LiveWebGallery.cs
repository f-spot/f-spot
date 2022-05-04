//
// LiveWebGallery.cs
//
// Author:
//   Anton Keks <anton@azib.net>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Anton Keks
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Net;

using FSpot.Core;
using FSpot.Extensions;

using Gtk;

namespace FSpot.Tools.LiveWebGallery
{
	public class LiveWebGallery : ICommand
	{
		static SimpleWebServer web_server;
		static ILiveWebGalleryOptions options;
		static LiveWebGalleryStats stats;
		LiveWebGalleryDialog dialog;

		public LiveWebGallery ()
		{
		}

		public void Run (object o, EventArgs e)
		{
			if (web_server == null) {
				stats = new LiveWebGalleryStats ();
				RequestHandler gallery = new GalleryRequestHandler (stats);
				options = gallery as ILiveWebGalleryOptions;

				web_server = new SimpleWebServer ();
				web_server.Stats = stats;
				web_server.RegisterHandler ("", gallery);
				web_server.RegisterHandler ("gallery", gallery);
				web_server.RegisterHandler ("ui", new ResourceRequestHandler ());
				web_server.RegisterHandler ("ping", new PingRequestHandler ());
				web_server.RegisterHandler ("photo", new PhotoRequestHandler (stats));
				web_server.RegisterHandler ("thumb", new ThumbnailRequestHandler (stats));
				web_server.RegisterHandler ("tag", new TagAddRemoveRequestHandler (options));
			}

			dialog = new LiveWebGalleryDialog (web_server, options, stats);
			dialog.Response += HandleResponse;
			dialog.ShowAll ();
		}

		void HandleResponse (object obj, ResponseArgs args)
		{
			dialog.Destroy ();
		}
	}

	public enum QueryType { ByTag, CurrentView, Selected }

	public interface ILiveWebGalleryOptions
	{
		QueryType QueryType { get; set; }
		Tag QueryTag { get; set; }
		bool LimitMaxPhotos { get; set; }
		int MaxPhotos { get; set; }
		bool TaggingAllowed { get; set; }
		Tag EditableTag { get; set; }
	}

	public class LiveWebGalleryStats : IWebStats
	{
		public event EventHandler StatsChanged;

		int gallery_views;
		public int GalleryViews {
			get { return gallery_views; }
			set { gallery_views = value; StatsChanged (this, null); }
		}

		int photo_views;
		public int PhotoViews {
			get { return photo_views; }
			set { photo_views = value; StatsChanged (this, null); }
		}

		IPAddress last_ip;
		public IPAddress LastIP {
			get { return last_ip; }
			set { last_ip = value; StatsChanged (this, null); }
		}

		public int BytesSent;

		public void IncomingRequest (IPAddress ip)
		{
			LastIP = ip;
		}
	}
}
