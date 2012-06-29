//
// LiveWebGallery.cs
//
// Author:
//   Anton Keks <anton@azib.net>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Anton Keks
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
using System.Net;

using Gtk;

using FSpot.Core;
using FSpot.Extensions;

namespace FSpot.Tools.LiveWebGallery
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
