/*
 * Editors/SoftFocus.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */
using Gtk;
using System;
using Cairo;
using Mono.Unix;

namespace FSpot.Editors {
	public class SoftFocus : EffectEditor {
		Widgets.SoftFocus soft; 
		Scale scale;
		bool double_buffer;
		
		protected override string GetTitle ()
		{
			return Catalog.GetString ("Soft Focus");
		}

		public SoftFocus (PhotoImageView view) : base (view)
		{
		}

		protected override void SetView (PhotoImageView value)
		{
			base.SetView (value);
			
			if (value == null)
				return;

			soft = new Widgets.SoftFocus (info);
			effect = (IEffect) soft;
		}

		protected override Widget CreateControls ()
		{
			VBox box = new VBox ();
			box.Spacing = 12;
			box.BorderWidth = 12;
			scale = new HScale (0, 1, .01);
			scale.Value = 0.5;
			scale.ValueChanged += HandleValueChanged;
			scale.WidthRequest = 250;
			box.PackStart (scale);
			HBox actions = new HBox ();
			actions.Spacing = 12;
			Button cancel = new Button (Catalog.GetString ("Cancel"));
			cancel.Clicked += HandleCancel;
			actions.PackStart (cancel);
			Button apply = new Button ( Catalog.GetString ("Apply"));
			apply.Clicked += HandleApply;
			actions.PackStart (apply);
			box.PackStart (actions);

			return box;	
		}

		private void HandleCancel (object sender, EventArgs args)
		{
			Close ();
		}

		private void HandleApply (object sender, EventArgs args)
		{
			BrowsablePointer item = view.Item;
			EditTarget target = new EditTarget (item);
			try { 
				using (ImageFile img = ImageFile.Create (item.Current.DefaultVersionUri)) {

					Cairo.Format format = view.CompletePixbuf ().HasAlpha ? Cairo.Format.Argb32 : Cairo.Format.Rgb24;

					MemorySurface dest = new MemorySurface (format,
										info.Bounds.Width,
										info.Bounds.Height);

					Context ctx = new Context (dest);
					effect.OnExpose (ctx, info.Bounds);
					((IDisposable)ctx).Dispose ();

					string tmp = ImageFile.TempPath (item.Current.DefaultVersionUri.LocalPath);
					using (Gdk.Pixbuf output = Widgets.CairoUtils.CreatePixbuf (dest)) {
						using (System.IO.Stream stream = System.IO.File.OpenWrite (tmp)) {
							img.Save (output, stream);
						
						}
					}

					dest.Destroy ();

					// FIXME Not this again. I need to imlplement a real version of the transfer
					// function that shows progress in the main window and allows for all the
					// goodies we'll need.
					Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;
					result = Gnome.Vfs.Xfer.XferUri (new Gnome.Vfs.Uri (UriList.PathToFileUri (tmp).ToString ()),
									 new Gnome.Vfs.Uri (target.Uri.ToString ()),
									 Gnome.Vfs.XferOptions.Default,
									 Gnome.Vfs.XferErrorMode.Abort, 
									 Gnome.Vfs.XferOverwriteMode.Replace, 
									 delegate {
										 System.Console.Write (".");
										 return 1;
									 });

					target.Commit ();
				}
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				target.Delete ();
				Dialog d = new EditExceptionDialog ((Gtk.Window) view.Toplevel, e, view.Item.Current);
				d.Show ();
				d.Run ();
				d.Destroy ();
			}
			Destroy ();
		}

		private void HandleValueChanged (object sender, EventArgs args)
		{
			soft.Radius = ((Scale)sender).Value;
			if (view != null)
				view.QueueDraw ();
		}
	}
}
