/*
 * LiveWebGalleryExtension.LiveWebGallery.cs
 *
 * Author(s):
 *	Anton Keks  <anton@azib.net>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

using Gtk;

using FSpot;
using FSpot.Extensions;
using FSpot.Utils;
using FSpot.Query;
using FSpot.UI.Dialog;
using Mono.Unix;

namespace LiveWebGalleryExtension
{
	public class LiveWebGallery : ICommand
	{
		private static SimpleWebServer web_server;
		private static ILiveWebGalleryOptions options;
		private static LiveWebGalleryStats stats;
		private LiveWebGalleryDialog dialog;
		
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
	
	public enum QueryType {ByTag, CurrentView, Selected}

	public interface ILiveWebGalleryOptions
	{
		QueryType QueryType {get; set;}
		Tag QueryTag {get; set;}
		bool LimitMaxPhotos {get; set;}
		int MaxPhotos {get; set;}
		bool TaggingAllowed {get; set;}
		Tag EditableTag {get; set;}
	}
	
	public class LiveWebGalleryStats : IWebStats
	{
		public event EventHandler StatsChanged;
		
		private int gallery_views;
		public int GalleryViews {
			get { return gallery_views; }
			set { gallery_views = value; StatsChanged(this, null); }
		}
		
		private int photo_views;
		public int PhotoViews {
			get { return photo_views; }
			set { photo_views = value; StatsChanged(this, null); }
		}
		
		private IPAddress last_ip;
		public IPAddress LastIP {
			get { return last_ip; }
			set { last_ip = value; StatsChanged(this, null); }
		}
		
		public int BytesSent;

		public void IncomingRequest (IPAddress ip)
		{
			LastIP = ip;
		}
	}
}
