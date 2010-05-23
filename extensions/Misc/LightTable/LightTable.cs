/*
 * LightTable.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Gtk;

using FSpot;
using FSpot.Extensions;

using Gtk.Moonlight;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Input;

namespace LightTableExtension
{
	public class LightTable: ICommand
	{
		public void Run (object o, EventArgs e)
		{
			Console.WriteLine ("EXECUTING LIGHTTABLE EXTENSION");

			LightTableWidget light_table;

			GtkSilver silver = new GtkSilver (1280, 855);
			silver.Attach (light_table = new LightTableWidget ());

			Gtk.Button reorganize = new Gtk.Button ("Place on grid");
			reorganize.Clicked += delegate (object ob, EventArgs ev) {light_table.ToGrid (); };

			HBox toolbar = new HBox ();
			toolbar.Add (reorganize);

			VBox vbox = new VBox ();
			vbox.Add (silver);
			vbox.Add (toolbar);

			Window w = new Window ("Light Table");
			w.Add (vbox);
			w.ShowAll ();
		}

		enum Action {Select, Move, Zoom};

		class LightTableWidget : Canvas
		{
			List <MLPhoto> photos;
			List <MLPhoto> selected_photos;
			bool multi_select;
			bool mouse_down;
			bool ctrl_down;
			Point ref_point;

			public LightTableWidget () : base ()
			{
				photos = new List<MLPhoto> ();
				selected_photos = new List<MLPhoto> ();

				foreach (Photo p in App.Instance.Organizer.SelectedPhotos ()) {
					MLPhoto p1 = new MLPhoto (p.DefaultVersion.Uri, this);
					photos.Add (p1);
				}

				//hook some events
				KeyDown += HandleKeyDown;
				KeyUp += HandleKeyUp;
				MouseLeftButtonDown += HandleLeftButtonDown;
				MouseLeftButtonUp += HandleLeftButtonUp;
				MouseMove += HandleMouseMove;

				ToGrid ();
			}

			public void ToGrid ()
			{
				int grid = (int)Math.Ceiling (Math.Sqrt ((double)photos.Count));
				for (int p = 0; p < photos.Count; p++) {
					MLPhoto ph = photos [p];
					int i = p%grid; int j = p/grid;
					if (!Children.Contains (ph))
						Children.Add (ph);
					ph.Scale (1280/grid);
					ph.SetPosition (10 + i * (10 + 1280/grid), 10 + j * (10 + 855/grid));
					ph.HideControls ();
				}
			}

			public void Select (MLPhoto photo)
			{
				if (IsSelected (photo) && multi_select) {
					photo.HideControls ();
					selected_photos.Remove (photo);
					return;
				}

				//Push on top
				Children.Remove (photo); Children.Add (photo);

				if (!multi_select && selected_photos != null) {
					foreach (MLPhoto p in selected_photos)
						p.HideControls ();
					selected_photos = new List<MLPhoto> ();
				}
				selected_photos.Add (photo);
				photo.ShowControls ();
			}

			bool IsSelected (MLPhoto photo)
			{
				return (photos != null && selected_photos.Contains (photo));
			}

			void RemoveSelected ()
			{
				if (selected_photos == null || selected_photos.Count == 0)
					return;

				foreach (MLPhoto selected in selected_photos) {
					Children.Remove (selected);
					photos.Remove (selected);
					Console.WriteLine ("Children:"+Children.Count);
				}
				selected_photos = new List<MLPhoto> ();
				ToGrid ();
			}

			void HandleKeyDown (object o, KeyboardEventArgs k)
			{
				if (k.Key == 4) {
					multi_select = true;
					return;
				}

				if (k.Key == 5) {
					ctrl_down = true;
					return;
				}

				if (k.Key == 18) {
					RemoveSelected ();
					return;
				}
			}

			void HandleKeyUp (object o, KeyboardEventArgs k)
			{
				if (k.Key == 4) {
					multi_select = false;
					return;
				}

				if (k.Key == 5) {
					ctrl_down = false;
					return;
				}

			}

			void HandleLeftButtonDown (object o, MouseEventArgs e)
			{
				mouse_down = true;
				ref_point = e.GetPosition (null);
			}

			void HandleLeftButtonUp (object o, MouseEventArgs e)
			{
				mouse_down = false;
				ref_point = e.GetPosition (null);
			}

			void HandleMouseMove (object o, MouseEventArgs e)
			{
				if (mouse_down && ctrl_down) { //Zooming
					Point current = e.GetPosition (null);
					double ratio;
					if (current.Y - ref_point.Y > 0)
						ratio = 1.02;
					else
						ratio = 0.98;
					foreach (MLPhoto selected in selected_photos)
						selected.Zoom (ratio);
					ref_point = current;
					return;
				}

				if (mouse_down) { //translating
					Point current = e.GetPosition (null);
					foreach (MLPhoto selected in selected_photos)
						selected.Translate ((int)(current.X - ref_point.X), (int)(current.Y - ref_point.Y));
					ref_point = current;
					return;
				}
			}

		}

		class MLPhoto : Control
		{
			LightTableWidget parent;

			FrameworkElement xaml;
			System.Windows.Controls.Image image;
			ScaleTransform scale_transform;
			TranslateTransform translate_transform;
			Canvas controls;

			public MLPhoto (Uri uri, LightTableWidget parent)
			{
				this.parent = parent;

				//Load XAML
				using (Stream stream = Assembly.GetCallingAssembly ().GetManifestResourceStream ("Photo.xaml")) {
					using (StreamReader reader = new StreamReader (stream)) {
						xaml = InitializeFromXaml (reader.ReadToEnd ());
					}
				}

				//Extract Elements
				image = xaml.FindName ("image") as System.Windows.Controls.Image;
				scale_transform = xaml.FindName ("scaleTransform") as ScaleTransform;
				translate_transform = xaml.FindName ("translateTransform") as TranslateTransform;
				controls = xaml.FindName ("allControls") as Canvas;
				Canvas translate_controls = xaml.FindName ("translateControls") as Canvas;

				//Hook events
				xaml.MouseLeftButtonDown += HandleLeftButtonDown;

				//Get the image
				Downloader downl = new Downloader ();
				downl.Completed += DownloadComplete;
				downl.Open ("GET", uri);
				downl.Send ();
			}

			public void Scale (int width)
			{
				double ratio = (double)width / 610;
				scale_transform.ScaleX = ratio;
				scale_transform.ScaleY = ratio;
			}

			public void Zoom (double ratio)
			{
				scale_transform.ScaleX *= ratio;
				scale_transform.ScaleY *= ratio;
			}

			public void SetPosition (int x, int y)
			{
				translate_transform.X = x;
				translate_transform.Y = y;
			}

			public void Translate (int x, int y)
			{
				translate_transform.X += x;
				translate_transform.Y += y;
			}

			public void ShowControls ()
			{
				controls.Opacity = 0.3;
			}

			public void HideControls ()
			{
				controls.Opacity = 0;
			}

			public void HandleLeftButtonDown (object o, MouseEventArgs e)
			{
				parent.Select (this);
			}

			private void DownloadComplete (object o, EventArgs e)
			{
				image.SetSource (o as Downloader, null);
			}
		}
	}
}
