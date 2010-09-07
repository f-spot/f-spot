/*
 * CollectionGridView.cs
 *
 * Author(s)
 *  Etore Perazzoli
 *  Larry Ewing <lewing@novell.com>
 *  Stephane Delcroix <stephane@delcroix.org>
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;

using Gtk;
using Gdk;

using FSpot.Core;
using FSpot.Utils;


namespace FSpot.Widgets
{

    /// <summary>
    ///    This class extends CellGridView to provide a grid view for a photo collection.
    /// </summary>
    public abstract class CollectionGridView : CellGridView
    {

#region Private Fields

        private ThumbnailDecorationRenderer rating_renderer = new ThumbnailRatingDecorationRenderer ();

        private ThumbnailCaptionRenderer tag_renderer = new ThumbnailTagsCaptionRenderer ();
        private ThumbnailCaptionRenderer date_renderer = new ThumbnailDateCaptionRenderer ();
        private ThumbnailCaptionRenderer filename_renderer = new ThumbnailFilenameCaptionRenderer ();

#endregion

#region Public Properties

        public IBrowsableCollection Collection {
            get; private set;
        }

        public FSpot.PixbufCache Cache {
            get; private set;
        }

#endregion

#region Constructors

        public CollectionGridView (IntPtr raw) : base (raw)
        {
        }

        public CollectionGridView (IBrowsableCollection collection) : base ()
        {
            Collection = collection;

            Collection.Changed += (obj) => {
                QueueResize ();
            };

            Collection.ItemsChanged += (obj, args) => {
                foreach (int item in args.Items) {
                    if (args.Changes.DataChanged)
                        UpdateThumbnail (item);
                    InvalidateCell (item);
                }
            };

            Name = "ImageContainer";

            Cache = new FSpot.PixbufCache ();
            Cache.OnPixbufLoaded += HandlePixbufLoaded;
        }

#endregion

#region Zooming and Thumbnail Size

        // fixed constants
        protected const double ZOOM_FACTOR = 1.2;
        protected const int MAX_THUMBNAIL_WIDTH = 256;
        protected const int MIN_THUMBNAIL_WIDTH = 64;

        // size of the border of the whole pane
        protected const int SELECTION_THICKNESS = 5;

        // frame around the whole cell
        protected const int CELL_BORDER_WIDTH = 10;

        // padding between the thumbnail and the thumbnail caption
        protected const int CAPTION_PADDING = 6;


        // current with of the thumbnails. (height is calculated)
        private int thumbnail_width = 128;

        // current ratio of thumbnail width and height
        private double thumbnail_ratio = 4.0 / 3.0;

        public int ThumbnailWidth {
            get { return thumbnail_width; }
            set {
                value = Math.Min (value, MAX_THUMBNAIL_WIDTH);
                value = Math.Max (value, MIN_THUMBNAIL_WIDTH);

                if (thumbnail_width != value) {
                    thumbnail_width = value;
                    QueueResize ();

                    if (ZoomChanged != null)
                        ZoomChanged (this, System.EventArgs.Empty);
                }
            }
        }

        public double Zoom {
            get {
                return ((double)(ThumbnailWidth - MIN_THUMBNAIL_WIDTH) / (double)(MAX_THUMBNAIL_WIDTH - MIN_THUMBNAIL_WIDTH));
            }
            set {
                ThumbnailWidth = (int) ((value) * (MAX_THUMBNAIL_WIDTH - MIN_THUMBNAIL_WIDTH)) + MIN_THUMBNAIL_WIDTH;
            }
        }

        public double ThumbnailRatio {
            get { return thumbnail_ratio; }
            set {
                thumbnail_ratio = value;
                QueueResize ();
            }
        }

        public int ThumbnailHeight {
            get { return (int) Math.Round ((double) thumbnail_width / ThumbnailRatio); }
        }

        public void ZoomIn ()
        {
            ThumbnailWidth = (int) (ThumbnailWidth * ZOOM_FACTOR);
        }

        public void ZoomOut ()
        {
            ThumbnailWidth = (int) (ThumbnailWidth / ZOOM_FACTOR);
        }

#endregion

#region Implementation of Base Class Layout Properties

        protected override int MinCellHeight {
            get {
                int cell_height = ThumbnailHeight + 2 * CELL_BORDER_WIDTH;

                if (DisplayTags || DisplayDates || DisplayFilenames)
                    cell_height += CAPTION_PADDING;

                if (DisplayTags)
                    cell_height += tag_renderer.GetHeight (this, ThumbnailWidth);

                if (DisplayDates && Style != null)
                    cell_height += date_renderer.GetHeight (this, ThumbnailWidth);

                if (DisplayFilenames && Style != null)
                    cell_height += filename_renderer.GetHeight (this, ThumbnailWidth);

                return cell_height;
            }
        }

        protected override int MinCellWidth {
            get { return ThumbnailWidth + 2 * CELL_BORDER_WIDTH; }
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

        private bool display_tags = true;
        public bool DisplayTags {
            get {
                return display_tags;
            }

            set {
                display_tags = value;
                QueueResize ();
            }
        }

        private bool display_dates = true;
        public bool DisplayDates {
            get {
                if (MinCellWidth > 100)
                    return display_dates;
                else
                    return false;
            }

            set {
                display_dates = value;
                QueueResize ();
            }
        }

        private bool display_filenames = false;
        public bool DisplayFilenames {
            get { return display_filenames; }
            set {
                if (value != display_filenames) {
                    display_filenames = value;
                    QueueResize ();
                }
            }
        }

        private bool display_ratings = true;
        public bool DisplayRatings {
            get {
                if (MinCellWidth > 100)
                    return display_ratings;
                else
                    return false;
            }

            set {
                display_ratings  = value;
                QueueResize ();
            }
        }

#endregion

#region Public Events

        public event EventHandler ZoomChanged;

#endregion

#region Event Handlers

        private void HandlePixbufLoaded (FSpot.PixbufCache Cache, FSpot.PixbufCache.CacheEntry entry)
        {
            Gdk.Pixbuf result = entry.ShallowCopyPixbuf ();
            int order = (int) entry.Data;

            if (result == null)
                return;

            // We have to do the scaling here rather than on load because we need to preserve the
            // Pixbuf option iformation to verify the thumbnail validity later
            int width, height;
            PixbufUtils.Fit (result, ThumbnailWidth, ThumbnailHeight, false, out width, out height);
            if (result.Width > width && result.Height > height) {
                //  Log.Debug ("scaling");
                Gdk.Pixbuf temp = PixbufUtils.ScaleDown (result, width, height);
                result.Dispose ();
                result = temp;
            } else if (result.Width < ThumbnailWidth && result.Height < ThumbnailHeight) {
                // FIXME this is a workaround to handle images whose actual size is smaller than
                // the thumbnail size, it needs to be fixed at a different level.
                Gdk.Pixbuf temp = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, ThumbnailWidth, ThumbnailHeight);
                temp.Fill (0x00000000);
                result.CopyArea (0, 0,
                        result.Width, result.Height,
                        temp,
                        (temp.Width - result.Width)/ 2,
                        temp.Height - result.Height);

                result.Dispose ();
                result = temp;
            }

            Cache.Update (entry, result);
            InvalidateCell (order);
        }

#endregion

#region Drawing Methods

        public void UpdateThumbnail (int thumbnail_num)
        {
            IPhoto photo = Collection [thumbnail_num];
            Cache.Remove (photo.DefaultVersion.Uri);
            InvalidateCell (thumbnail_num);
        }

        protected override void DrawCell (int cell_num, Rectangle cell_area, Rectangle expose_area)
        {
            DrawPhoto (cell_num, cell_area, expose_area, false, false);
        }

        // FIXME Cache the GCs?
        protected virtual void DrawPhoto (int cell_num, Rectangle cell_area, Rectangle expose_area, bool selected, bool focussed)
        {
            if (!cell_area.Intersect (expose_area, out expose_area))
                return;

            IPhoto photo = Collection [cell_num];

            FSpot.PixbufCache.CacheEntry entry = Cache.Lookup (photo.DefaultVersion.Uri);
            if (entry == null)
                Cache.Request (photo.DefaultVersion.Uri, cell_num, ThumbnailWidth, ThumbnailHeight);
            else
                entry.Data = cell_num;

            StateType cell_state = selected ? (HasFocus ? StateType.Selected : StateType.Active) : State;

            if (cell_state != State)
                Style.PaintBox (Style, BinWindow, cell_state,
                    ShadowType.Out, expose_area, this, "IconView",
                    cell_area.X, cell_area.Y,
                    cell_area.Width - 1, cell_area.Height - 1);

            Gdk.Rectangle focus = Gdk.Rectangle.Inflate (cell_area, -3, -3);

            if (HasFocus && focussed) {
                Style.PaintFocus(Style, BinWindow,
                        cell_state, expose_area,
                        this, null,
                        focus.X, focus.Y,
                        focus.Width, focus.Height);
            }

            Gdk.Rectangle region = Gdk.Rectangle.Zero;
            Gdk.Rectangle image_bounds = Gdk.Rectangle.Inflate (cell_area, -CELL_BORDER_WIDTH, -CELL_BORDER_WIDTH);
            int expansion = ThrobExpansion (cell_num, selected);

            Gdk.Pixbuf thumbnail = null;
            if (entry != null)
                thumbnail = entry.ShallowCopyPixbuf ();

            Gdk.Rectangle draw = Gdk.Rectangle.Zero;
            if (Gdk.Rectangle.Inflate (image_bounds, expansion + 1, expansion + 1).Intersect (expose_area, out image_bounds) && thumbnail != null) {

                PixbufUtils.Fit (thumbnail, ThumbnailWidth, ThumbnailHeight,
                        true, out region.Width, out region.Height);

                region.X = (int) (cell_area.X + (cell_area.Width - region.Width) / 2);
                region.Y = (int) cell_area.Y + ThumbnailHeight - region.Height + CELL_BORDER_WIDTH;

                if (Math.Abs (region.Width - thumbnail.Width) > 1
                    && Math.Abs (region.Height - thumbnail.Height) > 1)
                Cache.Reload (entry, cell_num, thumbnail.Width, thumbnail.Height);

                region = Gdk.Rectangle.Inflate (region, expansion, expansion);
                Pixbuf temp_thumbnail;
                region.Width = System.Math.Max (1, region.Width);
                region.Height = System.Math.Max (1, region.Height);

                if (Math.Abs (region.Width - thumbnail.Width) > 1
                    && Math.Abs (region.Height - thumbnail.Height) > 1) {
                    if (region.Width < thumbnail.Width && region.Height < thumbnail.Height) {
                        /*
                        temp_thumbnail = PixbufUtils.ScaleDown (thumbnail,
                                region.Width, region.Height);
                        */
                        temp_thumbnail = thumbnail.ScaleSimple (region.Width, region.Height,
                                InterpType.Bilinear);


                        lock (entry) {
                            if (entry.Reload && expansion == 0 && !entry.IsDisposed) {
                                entry.SetPixbufExtended (temp_thumbnail.ShallowCopy (), false);
                                entry.Reload = true;
                            }
                        }
                    } else {
                        temp_thumbnail = thumbnail.ScaleSimple (region.Width, region.Height,
                                InterpType.Bilinear);
                    }
                } else
                    temp_thumbnail = thumbnail;

                // FIXME There seems to be a rounding issue between the
                // scaled thumbnail sizes, we avoid this for now by using
                // the actual thumnail sizes here.
                region.Width = temp_thumbnail.Width;
                region.Height = temp_thumbnail.Height;

                draw = Gdk.Rectangle.Inflate (region, 1, 1);

                if (!temp_thumbnail.HasAlpha)
                    Style.PaintShadow (Style, BinWindow, cell_state,
                        ShadowType.Out, expose_area, this,
                        "IconView",
                        draw.X, draw.Y,
                        draw.Width, draw.Height);

                if (region.Intersect (expose_area, out draw)) {
                    Cms.Profile screen_profile;
                    if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
                        Pixbuf t = temp_thumbnail.Copy ();
                        temp_thumbnail.Dispose ();
                        temp_thumbnail = t;
                        FSpot.ColorManagement.ApplyProfile (temp_thumbnail, screen_profile);
                    }
                    temp_thumbnail.RenderToDrawable (BinWindow, Style.WhiteGC,
                            draw.X - region.X,
                            draw.Y - region.Y,
                            draw.X, draw.Y,
                            draw.Width, draw.Height,
                            RgbDither.None,
                            draw.X, draw.Y);
                }

                if (temp_thumbnail != thumbnail) {
                    temp_thumbnail.Dispose ();
                }

            }

            if (thumbnail != null) {
                thumbnail.Dispose ();
            }

            // Render Decorations
            if (DisplayRatings && region.X == draw.X && region.X != 0) {
                rating_renderer.Render (BinWindow, this, region, expose_area, cell_state, photo);
            }

            // Render Captions
            Rectangle caption_area = Rectangle.Zero;
            caption_area.Y = cell_area.Y + CELL_BORDER_WIDTH + ThumbnailHeight + CAPTION_PADDING;
            caption_area.X = cell_area.X + CELL_BORDER_WIDTH;
            caption_area.Width = cell_area.Width - 2 * CELL_BORDER_WIDTH;

            if (DisplayDates) {
                caption_area.Height = date_renderer.GetHeight (this, ThumbnailWidth);
                date_renderer.Render (BinWindow, this, caption_area, expose_area, cell_state, photo);

                caption_area.Y += caption_area.Height;
            }

            if (DisplayFilenames) {
                caption_area.Height = filename_renderer.GetHeight (this, ThumbnailWidth);
                filename_renderer.Render (BinWindow, this, caption_area, expose_area, cell_state, photo);

                caption_area.Y += caption_area.Height;
            }

            if (DisplayTags) {
                caption_area.Height = tag_renderer.GetHeight (this, ThumbnailWidth);
                tag_renderer.Render (BinWindow, this, caption_area, expose_area, cell_state, photo);

                caption_area.Y += caption_area.Height;
            }
        }

        protected override void PreloadCell (int cell_num)
        {
            var photo = Collection [cell_num];
            var entry = Cache.Lookup (photo.DefaultVersion.Uri);

            if (entry == null)
                Cache.Request (photo.DefaultVersion.Uri, cell_num, ThumbnailWidth, ThumbnailHeight);
        }

#endregion


#region Throb Interface

        private uint throb_timer_id;
        private int throb_cell = -1;
        private int throb_state;
        private const int throb_state_max = 40;

        public void Throb (int cell_num)
        {
            throb_state = 0;
            throb_cell = cell_num;
            if (throb_timer_id == 0)
                throb_timer_id = GLib.Timeout.Add ((39000/throb_state_max)/100,
                    new GLib.TimeoutHandler (HandleThrobTimer));

            InvalidateCell (cell_num);
        }

        private void CancelThrob ()
        {
            if (throb_timer_id != 0)
                GLib.Source.Remove (throb_timer_id);
        }

        private bool HandleThrobTimer ()
        {
            InvalidateCell (throb_cell);
            if (throb_state++ < throb_state_max) {
                return true;
            } else {
                throb_cell = -1;
                throb_timer_id = 0;
                return false;
            }
        }

        private int ThrobExpansion (int cell, bool selected)
        {
            int expansion = 0;
            if (cell == throb_cell) {
                double t = throb_state / (double) (throb_state_max - 1);
                double s;
                if (selected)
                    s = Math.Cos (-2 * Math.PI * t);
                else
                    s = 1 - Math.Cos (-2 * Math.PI * t);

                expansion = (int) (SELECTION_THICKNESS * s);
            } else if (selected) {
                expansion = SELECTION_THICKNESS;
            }

            return expansion;
        }

#endregion

#region Theming

        private void SetColors ()
        {
            if (IsRealized) {
                BinWindow.Background = Style.DarkColors [(int)State];
            }
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

