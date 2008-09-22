/*
 * FSpot.PrintOperation.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

#if GTK_2_10
using Cairo;
using System;
using System.Runtime.InteropServices;
using Mono.Unix;

using FSpot.Widgets;
using FSpot.Utils;

namespace FSpot
{
	public class PrintOperation : Gtk.PrintOperation
	{
		IBrowsableItem [] selected_photos;
		int photos_per_page = 1;
		CustomPrintWidget.FitMode fit = CustomPrintWidget.FitMode.Scaled;
		bool repeat, white_borders, crop_marks;
		string comment;

		public PrintOperation (IBrowsableItem [] selected_photos) : base ()
		{
			this.selected_photos = selected_photos;
			CustomTabLabel = Catalog.GetString ("Image Settings");
			NPages = selected_photos.Length;
			DefaultPageSetup = FSpot.Global.PageSetup;
		}

		protected override void OnBeginPrint (Gtk.PrintContext context)
		{
			base.OnBeginPrint (context);
		}

		protected override Gtk.Widget OnCreateCustomWidget ()
		{
			Gtk.Widget widget = new CustomPrintWidget (this);
			widget.ShowAll ();
			(widget as CustomPrintWidget).Changed += OnCustomWidgetChanged;
			OnCustomWidgetChanged (widget);
			return widget;
		}

		protected override void OnCustomWidgetApply (Gtk.Widget widget)
		{
			CustomPrintWidget cpw = widget as CustomPrintWidget;
			UseFullPage = cpw.UseFullPage;
			photos_per_page = cpw.PhotosPerPage;
			repeat = cpw.Repeat;
			NPages = repeat ? selected_photos.Length :(int) Math.Ceiling (1.0 * selected_photos.Length / photos_per_page);
			fit = cpw.Fitmode;
			white_borders = cpw.WhiteBorders;
			crop_marks = cpw.CropMarks;
			comment = cpw.CustomText;
		}

		protected void OnCustomWidgetChanged (Gtk.Widget widget)
		{
			OnCustomWidgetApply (widget);
			using (ImageSurface surface = new ImageSurface (Format.ARGB32, 360, 254)) {
				using (Context gr = new Context (surface)) {
					gr.Color = new Color (1, 1, 1);
					gr.Rectangle (0, 0, 360, 254);
					gr.Fill ();
					using (Gdk.Pixbuf pixbuf = Gdk.Pixbuf.LoadFromResource ("flower.png")) {
						DrawImage (gr, pixbuf,0, 0, 360, 254);
					}
				}
				(widget as CustomPrintWidget).PreviewImage.Pixbuf = CreatePixbuf (surface);
			}
		}

		protected override void OnDrawPage (Gtk.PrintContext context, int page_nr)
		{
			base.OnDrawPage (context, page_nr);
			Context cr = context.CairoContext;	

			int ppx, ppy;
			switch (photos_per_page) {
			default:
			case 1: ppx = ppy =1; break;
			case 2: ppx = 1; ppy = 2; break;
			case 4: ppx = ppy = 2; break;
			case 9: ppx = ppy = 3; break;
			}

			//FIXME: if paper is landscape, swap ppx with ppy

			double w = context.Width / ppx;
			double h = context.Height / ppy;

			for (int x = 0; x <= ppx; x++) {
				for (int y = 0; y <= ppy; y++) {
					int p_index = repeat ? page_nr : page_nr * photos_per_page + y * ppx + x;
					if (crop_marks)
						DrawCropMarks (cr, x*w, y*h, w*.1);
					if (x == ppx || y == ppy || p_index >= selected_photos.Length)
						continue;
					using (ImageFile img = new ImageFile (selected_photos[p_index].DefaultVersionUri))
					{
						Gdk.Pixbuf pixbuf;
						try {
							pixbuf = img.Load ();
							FSpot.ColorManagement.ApplyPrinterProfile (pixbuf, img.GetProfile ());
						} catch (Exception e) {
							Log.Exception ("Unable to load image " + selected_photos[p_index].DefaultVersionUri + "\n", e);
							// If the image is not found load error pixbuf
							pixbuf = new Gdk.Pixbuf (PixbufUtils.ErrorPixbuf, 0, 0, 
										      PixbufUtils.ErrorPixbuf.Width, 
										      PixbufUtils.ErrorPixbuf.Height);
						}
						//Gdk.Pixbuf pixbuf = img.Load (100, 100);
						bool rotated = false;
						if (Math.Sign ((double)pixbuf.Width/pixbuf.Height - 1.0) != Math.Sign (w/h - 1.0)) {
							Gdk.Pixbuf d_pixbuf = pixbuf.RotateSimple (Gdk.PixbufRotation.Counterclockwise);
							pixbuf.Dispose ();
							pixbuf = d_pixbuf;
							rotated = true;
						}

						DrawImage (cr, pixbuf, x * w, y * h, w, h);
						DrawComment (context, (x + 1) * w, (rotated ? y : y + 1) * h, (rotated ? w : h) * .025, comment, rotated);
						pixbuf.Dispose ();
					}
				}
			}

		}

		protected override void OnEndPrint (Gtk.PrintContext context)
		{
			base.OnEndPrint (context);
			context.Dispose ();
		}

		protected override void OnRequestPageSetup (Gtk.PrintContext context, int page_nr, Gtk.PageSetup setup)
		{
			base.OnRequestPageSetup (context, page_nr, setup);
		}

		private void DrawCropMarks (Context cr, double x, double y, double length)
		{
			cr.Save ();
			cr.Color = new Color (0, 0, 0);
			cr.MoveTo (x - length/2, y);
			cr.LineTo (x + length/2, y);
			cr.MoveTo (x, y - length/2);
			cr.LineTo (x, y + length/2);
			cr.LineWidth = .2;
			cr.SetDash (new double[] {length*.4, length*.2}, 0);
			cr.Stroke ();
			cr.Restore ();
		}

		private static void DrawComment (Gtk.PrintContext context, double x, double y, double h, string comment, bool rotated)
		{
			if (comment == null || comment == String.Empty)
				return;

			Context cr = context.CairoContext;
			cr.Save ();
			Pango.Layout layout = context.CreatePangoLayout ();
			Pango.FontDescription desc = Pango.FontDescription.FromString ("sans 14");
			layout.FontDescription = desc;
			layout.SetText (comment);
			int lay_w, lay_h;
			layout.GetPixelSize (out lay_w, out lay_h);
			double scale = h/lay_h;
			if (rotated) {
				cr.Translate (x - h, y + lay_w * scale);
				cr.Rotate (- Math.PI / 2);
			}
			else
				cr.Translate (x - lay_w * scale, y - h);
			cr.Scale (scale, scale);
			Pango.CairoHelper.ShowLayout (context.CairoContext, layout);
			cr.Restore ();
		}
	

		private void DrawImage (Context cr, Gdk.Pixbuf pixbuf, double x, double y, double w, double h)
		{
			double scalex, scaley;
			switch (fit) {
			case CustomPrintWidget.FitMode.Zoom:
				scalex = scaley = Math.Max (w/pixbuf.Width, h/pixbuf.Height);
				break;
			case CustomPrintWidget.FitMode.Fill:
				scalex = w/pixbuf.Width;
				scaley = h/pixbuf.Height;
				break;
			default:
			case CustomPrintWidget.FitMode.Scaled:
				scalex = scaley = Math.Min (w/pixbuf.Width, h/pixbuf.Height);
				break;
			}	

			double rectw = w / scalex;
			double recth = h / scaley;

			cr.Save ();
			if (white_borders)
				cr.Translate (w * .025, h * .025);

			cr.Translate (x, y);
			if (white_borders)
				cr.Scale (.95, .95);
			cr.Scale (scalex, scaley);
			cr.Rectangle (0, 0, rectw, recth);
			Gdk.CairoHelper.SetSourcePixbuf (cr, pixbuf, (rectw - pixbuf.Width) / 2.0, (recth - pixbuf.Height) / 2.0);
			cr.Fill ();

			if (white_borders) {
				cr.Rectangle (0, 0 ,rectw, recth);
				cr.Color = new Color (0, 0, 0);
				cr.LineWidth = 1 / scalex;
				cr.Stroke ();
			}
			cr.Restore ();
		}

		[DllImport("libfspot")]
		static extern IntPtr f_pixbuf_from_cairo_surface (IntPtr handle);
		
		private static Gdk.Pixbuf CreatePixbuf (Surface s)
		{
			IntPtr result = f_pixbuf_from_cairo_surface (s.Handle);
			return (Gdk.Pixbuf) GLib.Object.GetObject (result, true);
		}
	}
}
#endif
