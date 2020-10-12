//
// CollectionCellGridView.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Settings;
using FSpot.Utils;

using Gdk;

using Gtk;


namespace FSpot.Widgets
{
	/// <summary>
	///    This class extends CellGridView to provide a grid view for a photo collection.
	/// </summary>
	public abstract class CollectionGridView : CellGridView
	{
		readonly ThumbnailDecorationRenderer rating_renderer = new ThumbnailRatingDecorationRenderer ();

		readonly ThumbnailCaptionRenderer tag_renderer = new ThumbnailTagsCaptionRenderer ();
		readonly ThumbnailCaptionRenderer date_renderer = new ThumbnailDateCaptionRenderer ();
		readonly ThumbnailCaptionRenderer filename_renderer = new ThumbnailFilenameCaptionRenderer ();

		public IBrowsableCollection Collection { get; }

		public PixbufCache Cache { get; }

		protected CollectionGridView (IntPtr raw) : base (raw) { }

		protected CollectionGridView (IBrowsableCollection collection)
		{
			Collection = collection;

			Collection.Changed += (c) => { QueueResize (); };

			Collection.ItemsChanged += (c, args) => {
				foreach (int item in args.Items) {
					if (args.Changes.DataChanged)
						UpdateThumbnail (item);

					InvalidateCell (item);
				}
			};

			Name = "ImageContainer";

			Cache = new PixbufCache ();
			Cache.OnPixbufLoaded += HandlePixbufLoaded;
		}

		#region Zooming and Thumbnail Size

		// fixed constants
		protected const double ZoomFactor = 1.2;
		protected const int MaxThumbnailWidth = 256;
		protected const int MinThumbnailWidth = 64;

		// size of the border of the whole pane
		protected const int SelectionThickness = 5;

		// frame around the whole cell
		protected const int CellBorderWidth = 10;

		// padding between the thumbnail and the thumbnail caption
		protected const int CaptionPadding = 6;


		// current with of the thumbnails. (height is calculated)
		int thumbnailWidth = 128;

		// current ratio of thumbnail width and height
		double thumbnailRatio = 4.0 / 3.0;

		public int ThumbnailWidth {
			get { return thumbnailWidth; }
			set {
				value = Math.Min (value, MaxThumbnailWidth);
				value = Math.Max (value, MinThumbnailWidth);

				if (thumbnailWidth != value) {
					thumbnailWidth = value;
					QueueResize ();

					ZoomChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}

		public double Zoom {
			get {
				return ((double)(ThumbnailWidth - MinThumbnailWidth) / (double)(MaxThumbnailWidth - MinThumbnailWidth));
			}
			set {
				ThumbnailWidth = (int)((value) * (MaxThumbnailWidth - MinThumbnailWidth)) + MinThumbnailWidth;
			}
		}

		public double ThumbnailRatio {
			get { return thumbnailRatio; }
			set {
				thumbnailRatio = value;
				QueueResize ();
			}
		}

		public int ThumbnailHeight {
			get { return (int)Math.Round ((double)thumbnailWidth / ThumbnailRatio); }
		}

		public void ZoomIn ()
		{
			ThumbnailWidth = (int)(ThumbnailWidth * ZoomFactor);
		}

		public void ZoomOut ()
		{
			ThumbnailWidth = (int)(ThumbnailWidth / ZoomFactor);
		}

		#endregion

		#region Implementation of Base Class Layout Properties

		protected override int MinCellHeight {
			get {
				int minCellHeight = ThumbnailHeight + 2 * CellBorderWidth;

				if (DisplayTags || DisplayDates || DisplayFilenames)
					minCellHeight += CaptionPadding;

				if (DisplayTags)
					minCellHeight += tag_renderer.GetHeight (this, ThumbnailWidth);

				if (DisplayDates && Style != null)
					minCellHeight += date_renderer.GetHeight (this, ThumbnailWidth);

				if (DisplayFilenames && Style != null)
					minCellHeight += filename_renderer.GetHeight (this, ThumbnailWidth);

				return minCellHeight;
			}
		}

		protected override int MinCellWidth {
			get { return ThumbnailWidth + 2 * CellBorderWidth; }
		}

		protected override int CellCount {
			get {
				if (Collection == null)
					return 0;

				return Collection.Count;
			}
		}

		#endregion

		#region Thumbnail Decoration

		bool display_tags = true;
		public bool DisplayTags {
			get {
				return display_tags;
			}

			set {
				display_tags = value;
				QueueResize ();
			}
		}

		bool display_dates = true;
		public bool DisplayDates {
			get {
				if (MinCellWidth > 100)
					return display_dates;

				return false;
			}

			set {
				display_dates = value;
				QueueResize ();
			}
		}

		bool display_filenames;
		public bool DisplayFilenames {
			get { return display_filenames; }
			set {
				if (value != display_filenames) {
					display_filenames = value;
					QueueResize ();
				}
			}
		}

		bool display_ratings = true;
		public bool DisplayRatings {
			get {
				if (MinCellWidth > 100)
					return display_ratings;

				return false;
			}

			set {
				display_ratings = value;
				QueueResize ();
			}
		}

		#endregion

		#region Public Events

		public event EventHandler ZoomChanged;

		#endregion

		#region Event Handlers

		void HandlePixbufLoaded (PixbufCache cache, PixbufCache.CacheEntry entry)
		{
			Pixbuf result = entry.ShallowCopyPixbuf ();
			int order = (int)entry.Data;

			if (result == null)
				return;

			// We have to do the scaling here rather than on load because we need to preserve the
			// Pixbuf option iformation to verify the thumbnail validity later
			PixbufUtils.Fit (result, ThumbnailWidth, ThumbnailHeight, false, out var width, out var height);
			if (result.Width > width && result.Height > height) {
				//  Log.Debug ("scaling");
				Pixbuf temp = result.ScaleSimple (width, height, InterpType.Nearest);
				result.Dispose ();
				result = temp;
			} else if (result.Width < ThumbnailWidth && result.Height < ThumbnailHeight) {
				// FIXME this is a workaround to handle images whose actual size is smaller than
				// the thumbnail size, it needs to be fixed at a different level.
				using var temp = new Pixbuf (Colorspace.Rgb, true, 8, ThumbnailWidth, ThumbnailHeight);
				temp.Fill (0x00000000);
				result.CopyArea (0, 0,
						result.Width, result.Height,
						temp,
						(temp.Width - result.Width) / 2,
						temp.Height - result.Height);

				result.Dispose ();
				result = temp;
			}

			cache.Update (entry, result);
			InvalidateCell (order);
		}

		#endregion

		#region Drawing Methods

		public void UpdateThumbnail (int thumbnailNum)
		{
			IPhoto photo = Collection[thumbnailNum];
			Cache.Remove (photo.DefaultVersion.Uri);
			InvalidateCell (thumbnailNum);
		}

		protected override void DrawCell (int cellNum, Rectangle cellArea, Rectangle exposeArea)
		{
			DrawPhoto (cellNum, cellArea, exposeArea, false, false);
		}

		// FIXME Cache the GCs?
		protected virtual void DrawPhoto (int cellNum, Rectangle cellArea, Rectangle exposeArea, bool selected, bool focussed)
		{
			if (!cellArea.Intersect (exposeArea, out exposeArea))
				return;

			IPhoto photo = Collection[cellNum];

			PixbufCache.CacheEntry entry = Cache.Lookup (photo.DefaultVersion.Uri);
			if (entry == null)
				Cache.Request (photo.DefaultVersion.Uri, cellNum, ThumbnailWidth, ThumbnailHeight);
			else
				entry.Data = cellNum;

			StateType cellState = selected ? (HasFocus ? StateType.Selected : StateType.Active) : State;

			if (cellState != State)
				Style.PaintBox (Style, BinWindow, cellState,
					ShadowType.Out, exposeArea, this, "IconView",
					cellArea.X, cellArea.Y,
					cellArea.Width - 1, cellArea.Height - 1);

			var focus = Rectangle.Inflate (cellArea, -3, -3);

			if (HasFocus && focussed) {
				Style.PaintFocus (Style, BinWindow,
						cellState, exposeArea,
						this, null,
						focus.X, focus.Y,
						focus.Width, focus.Height);
			}

			var region = Rectangle.Zero;
			var imageBounds = Rectangle.Inflate (cellArea, -CellBorderWidth, -CellBorderWidth);
			int expansion = ThrobExpansion (cellNum, selected);

			Pixbuf thumbnail = null;
			if (entry != null)
				thumbnail = entry.ShallowCopyPixbuf ();

			var draw = Rectangle.Zero;
			if (Rectangle.Inflate (imageBounds, expansion + 1, expansion + 1).Intersect (exposeArea, out imageBounds) && thumbnail != null) {

				PixbufUtils.Fit (thumbnail, ThumbnailWidth, ThumbnailHeight,
						true, out region.Width, out region.Height);

				region.X = cellArea.X + (cellArea.Width - region.Width) / 2;
				region.Y = cellArea.Y + ThumbnailHeight - region.Height + CellBorderWidth;

				if (Math.Abs (region.Width - thumbnail.Width) > 1 && Math.Abs (region.Height - thumbnail.Height) > 1)
					Cache.Reload (entry, cellNum, thumbnail.Width, thumbnail.Height);

				region = Rectangle.Inflate (region, expansion, expansion);
				Pixbuf tempThumbnail;
				region.Width = Math.Max (1, region.Width);
				region.Height = Math.Max (1, region.Height);

				if (Math.Abs (region.Width - thumbnail.Width) > 1 && Math.Abs (region.Height - thumbnail.Height) > 1) {
					if (region.Width < thumbnail.Width && region.Height < thumbnail.Height) {
						/*
                        temp_thumbnail = PixbufUtils.ScaleDown (thumbnail,
                                region.Width, region.Height);
                        */
						tempThumbnail = thumbnail.ScaleSimple (region.Width, region.Height, InterpType.Bilinear);


						lock (entry) {
							if (entry.Reload && expansion == 0 && !entry.IsDisposed) {
								entry.SetPixbufExtended (tempThumbnail.ShallowCopy (), false);
								entry.Reload = true;
							}
						}
					} else {
						tempThumbnail = thumbnail.ScaleSimple (region.Width, region.Height,
								InterpType.Bilinear);
					}
				} else
					tempThumbnail = thumbnail;

				// FIXME There seems to be a rounding issue between the
				// scaled thumbnail sizes, we avoid this for now by using
				// the actual thumnail sizes here.
				region.Width = tempThumbnail.Width;
				region.Height = tempThumbnail.Height;

				draw = Rectangle.Inflate (region, 1, 1);

				if (!tempThumbnail.HasAlpha)
					Style.PaintShadow (Style, BinWindow, cellState, ShadowType.Out, exposeArea, this, "IconView",
						draw.X, draw.Y,
						draw.Width, draw.Height);

				if (region.Intersect (exposeArea, out draw)) {
					if (ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.ColorManagementDisplayProfile), out var screenProfile)) {
						Pixbuf t = tempThumbnail.Copy ();
						tempThumbnail.Dispose ();
						tempThumbnail = t;
						ColorManagement.ApplyProfile (tempThumbnail, screenProfile);
					}
					tempThumbnail.RenderToDrawable (BinWindow, Style.WhiteGC,
							draw.X - region.X,
							draw.Y - region.Y,
							draw.X, draw.Y,
							draw.Width, draw.Height,
							RgbDither.None,
							draw.X, draw.Y);
				}

				if (tempThumbnail != thumbnail)
					tempThumbnail.Dispose ();
			}

			thumbnail?.Dispose ();

			// Render Decorations
			if (DisplayRatings && region.X == draw.X && region.X != 0)
				rating_renderer.Render (BinWindow, this, region, exposeArea, cellState, photo);

			// Render Captions
			Rectangle captionArea = Rectangle.Zero;
			captionArea.Y = cellArea.Y + CellBorderWidth + ThumbnailHeight + CaptionPadding;
			captionArea.X = cellArea.X + CellBorderWidth;
			captionArea.Width = cellArea.Width - 2 * CellBorderWidth;

			if (DisplayDates) {
				captionArea.Height = date_renderer.GetHeight (this, ThumbnailWidth);
				date_renderer.Render (BinWindow, this, captionArea, exposeArea, cellState, photo);

				captionArea.Y += captionArea.Height;
			}

			if (DisplayFilenames) {
				captionArea.Height = filename_renderer.GetHeight (this, ThumbnailWidth);
				filename_renderer.Render (BinWindow, this, captionArea, exposeArea, cellState, photo);

				captionArea.Y += captionArea.Height;
			}

			if (DisplayTags) {
				captionArea.Height = tag_renderer.GetHeight (this, ThumbnailWidth);
				tag_renderer.Render (BinWindow, this, captionArea, exposeArea, cellState, photo);

				captionArea.Y += captionArea.Height;
			}
		}

		protected override void PreloadCell (int cellNum)
		{
			var photo = Collection[cellNum];
			var entry = Cache.Lookup (photo.DefaultVersion.Uri);

			if (entry == null)
				Cache.Request (photo.DefaultVersion.Uri, cellNum, ThumbnailWidth, ThumbnailHeight);
		}

		#endregion

		#region Throb Interface

		uint throb_timer_id;
		int throb_cell = -1;
		int throb_state;
		const int ThrobStateMax = 40;

		public void Throb (int cellNum)
		{
			throb_state = 0;
			throb_cell = cellNum;
			if (throb_timer_id == 0)
				throb_timer_id = GLib.Timeout.Add ((39000 / ThrobStateMax) / 100, HandleThrobTimer);

			InvalidateCell (cellNum);
		}

		void CancelThrob ()
		{
			if (throb_timer_id != 0)
				GLib.Source.Remove (throb_timer_id);
		}

		bool HandleThrobTimer ()
		{
			InvalidateCell (throb_cell);
			if (throb_state++ < ThrobStateMax)
				return true;

			throb_cell = -1;
			throb_timer_id = 0;
			return false;
		}

		int ThrobExpansion (int cell, bool selected)
		{
			int expansion = 0;
			if (cell == throb_cell) {
				double t = throb_state / (double)(ThrobStateMax - 1);
				double s;
				if (selected)
					s = Math.Cos (-2 * Math.PI * t);
				else
					s = 1 - Math.Cos (-2 * Math.PI * t);

				expansion = (int)(SelectionThickness * s);
			} else if (selected) {
				expansion = SelectionThickness;
			}

			return expansion;
		}

		#endregion

		#region Theming

		void SetColors ()
		{
			if (IsRealized)
				BinWindow.Background = Style.DarkColors[(int)State];
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			SetColors ();
		}

		protected override void OnStateChanged (StateType previous)
		{
			base.OnStateChanged (previous);
			SetColors ();
		}

		protected override void OnStyleSet (Style previous)
		{
			base.OnStyleSet (previous);
			SetColors ();
		}

		#endregion

		#region Override Other Base Class Behavior

		protected override void OnDestroyed ()
		{
			Cache.OnPixbufLoaded -= HandlePixbufLoaded;
			CancelThrob ();

			base.OnDestroyed ();
		}

		#endregion
	}
}
